// ============================================================
//  AppTpv — Interfaz 2 de 5: CONSUMICIONES (logica)
//
//  Misma logica que Comandero/js/menu.js + pedido.js:
//   1. El camarero elige categoria -> productos.
//   2. Al pulsar un producto se acumula en el pedido PENDIENTE
//      (aun no viaja al servidor).
//   3. "Enviar" abre/reutiliza la comanda abierta y manda las
//      lineas pendientes (quedan en verde).
//   4. "SUBTOTAL" muestra el desglose; solo si no hay pendientes.
//   5. "COBRAR" confirma la compra (el servidor decrementa stock).
// ============================================================
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using ClienteWPF.Servicios;

namespace ClienteWPF.Vistas;

public partial class VistaConsumiciones : UserControl
{
    private int _idPedido;               // 0 = sin pedido todavia
    private int _idUltimoPedido;         // ultimo pedido cobrado (para ver el ticket)

    // Lineas del comandero: pendientes (locales) + enviadas (comanda)
    private readonly List<LineaPedidoVista> _lineas = new();

    private List<ProductoVista> _carta = new();
    private string _categoriaActual = "";

    public VistaConsumiciones()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            CargarCarta();
            RefrescarPedido();
        };
    }

    // ---------- Carta / categorias ----------

    private void CargarCarta()
    {
        var r = Conexion.Enviar("PRODUCTOS_LISTA");
        if (!r.Ok) { TxtCartaInfo.Text = r.Error; return; }

        _carta = JsonSerializer.Deserialize<List<ProductoVista>>(
            r.Datos.GetProperty("productos").GetRawText()) ?? new();
        _carta = _carta.Where(p => p.Activo == 1).ToList();

        var categorias = _carta
            .Select(p => p.Categoria)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct()
            .OrderBy(c => c)
            .ToList();

        ListaCategorias.ItemsSource = categorias;

        if (categorias.Count == 0)
        {
            _categoriaActual = "";
            TxtCategoriaTitulo.Text = "PRODUCTOS";
            MostrarProductos(new List<ProductoVista>());
            return;
        }

        var indice = categorias.IndexOf(_categoriaActual);
        ListaCategorias.SelectedIndex = indice >= 0 ? indice : 0;
    }

    private void ListaCategorias_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ListaCategorias.SelectedItem is not string categoria) return;

        _categoriaActual = categoria;
        TxtCategoriaTitulo.Text = categoria.ToUpperInvariant();
        MostrarProductos(_carta.Where(p => p.Categoria == categoria).ToList());
    }

    private void MostrarProductos(List<ProductoVista> productos)
    {
        ListaProductos.ItemsSource = productos;
        TxtCartaInfo.Text = productos.Count == 0
            ? "No hay productos en esta categoria."
            : "";
    }

    // ---------- Pedido: anadir / editar lineas ----------

    private void Producto_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.Tag is not ProductoVista prod) return;

        if (prod.Agotado)
        {
            TxtCartaInfo.Text = $"{prod.Nombre}: no quedan unidades.";
            return;
        }

        // igual que pedido.js: si ya hay una linea PENDIENTE del mismo
        // producto se acumula la cantidad; las enviadas son intocables.
        var pendiente = _lineas.FirstOrDefault(l => !l.Enviada && l.IdProducto == prod.Id);
        if (pendiente != null)
        {
            pendiente.Cantidad++;
        }
        else
        {
            _lineas.Add(new LineaPedidoVista
            {
                IdProducto = prod.Id,
                Nombre = prod.Nombre,
                Cantidad = 1,
                PrecioUnitario = prod.Precio,
                Estado = "pendiente"
            });
        }

        RefrescarPedido();
        TxtCartaInfo.Text = $"{prod.Nombre} anadido al pedido.";
    }

    private void ListaPedido_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // el editor solo se abre para lineas pendientes
        if (ListaPedido.SelectedItem is LineaPedidoVista { Enviada: false } linea)
        {
            TxtCantidadEditor.Text = linea.Cantidad.ToString();
            PanelEditor.Visibility = Visibility.Visible;
        }
        else
        {
            PanelEditor.Visibility = Visibility.Collapsed;
        }
    }

    private void BtnGuardarLinea_Click(object sender, RoutedEventArgs e)
    {
        if (ListaPedido.SelectedItem is not LineaPedidoVista { Enviada: false } linea) return;

        if (!int.TryParse(TxtCantidadEditor.Text, out var cantidad) || cantidad < 1)
        {
            MessageBox.Show("Escribe una cantidad valida (1 o mas).", "AppTpv",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        linea.Cantidad = cantidad;
        RefrescarPedido();
    }

    private void BtnEliminarLinea_Click(object sender, RoutedEventArgs e)
    {
        if (ListaPedido.SelectedItem is not LineaPedidoVista { Enviada: false } linea) return;

        _lineas.Remove(linea);
        RefrescarPedido();
    }

    // ---------- Enviar a la comanda ----------

    private void BtnEnviar_Click(object sender, RoutedEventArgs e)
    {
        var pendientes = _lineas.Where(l => !l.Enviada).ToList();
        if (pendientes.Count == 0)
        {
            MessageBox.Show("No hay productos nuevos para enviar.", "AppTpv",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (_idPedido == 0)
        {
            if (!AsegurarPedidoAbierto()) return;
            CargarLineasEnviadas();     // recupera lo que ya tuviera la comanda
        }

        BtnEnviar.IsEnabled = false;
        BtnEnviar.Content = "Enviando...";

        try
        {
            foreach (var linea in pendientes)
            {
                var r = Conexion.Enviar("PEDIDO_AGREGAR_LINEA", new
                {
                    idPedido = _idPedido,
                    idProducto = linea.IdProducto,
                    cantidad = linea.Cantidad
                });

                if (!r.Ok)
                {
                    MessageBox.Show(r.Error, "No se pudo enviar", MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    RefrescarPedido();
                    CargarCarta();
                    return;
                }

                // el servidor es la fuente de verdad del precio
                if (r.Datos.TryGetProperty("precioUnitario", out var precio) &&
                    precio.ValueKind == JsonValueKind.Number)
                {
                    linea.PrecioUnitario = precio.GetDouble();
                }

                linea.Estado = "enviado";
            }

            RefrescarPedido();
            CargarCarta();
            TxtCartaInfo.Text = "Pedido enviado.";
        }
        finally
        {
            BtnEnviar.Content = "Enviar";
            BtnEnviar.IsEnabled = true;
        }
    }

    /// <summary>
    /// Abre una comanda nueva o adopta la que el usuario ya tuviera abierta
    /// (el servidor responde "Ya tienes el pedido N abierto").
    /// </summary>
    private bool AsegurarPedidoAbierto()
    {
        var r = Conexion.Enviar("PEDIDO_ABRIR");

        if (r.Ok)
        {
            _idPedido = r.Datos.GetProperty("idPedido").GetInt32();
        }
        else
        {
            var m = Regex.Match(r.Error, @"Ya tienes el pedido (\d+) abierto");
            if (!m.Success)
            {
                MessageBox.Show(r.Error, "AppTpv", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return false;
            }
            _idPedido = int.Parse(m.Groups[1].Value);
        }

        TxtEstadoPedido.Text = $"pedido n. {_idPedido}";
        return true;
    }

    /// <summary>
    /// Trae del servidor las lineas ya enviadas de la comanda abierta
    /// (equivale a pintarPedidoUlConEstado en menu.js).
    /// </summary>
    private void CargarLineasEnviadas()
    {
        if (_idPedido == 0) return;

        var r = Conexion.Enviar("TICKET_OBTENER", new { idPedido = _idPedido });
        if (!r.Ok) return;

        var ticket = r.Datos.GetProperty("ticket");
        if (!ticket.TryGetProperty("lineas", out var lista) ||
            lista.ValueKind != JsonValueKind.Array) return;

        var enviadas = JsonSerializer.Deserialize<List<LineaVista>>(lista.GetRawText()) ?? new();

        _lineas.RemoveAll(l => l.Enviada);
        foreach (var l in enviadas)
        {
            var producto = _carta.FirstOrDefault(p => p.Nombre == l.Nombre);
            _lineas.Add(new LineaPedidoVista
            {
                IdProducto = producto?.Id ?? 0,
                Nombre = l.Nombre,
                Cantidad = l.Cantidad,
                PrecioUnitario = l.PrecioUnitario,
                Estado = "enviado"
            });
        }

        RefrescarPedido();
    }

    // ---------- Subtotal / cobrar / ticket ----------

    private void BtnSubtotal_Click(object sender, RoutedEventArgs e)
    {
        if (_lineas.Any(l => !l.Enviada))
        {
            MessageBox.Show("Elimina o envia los productos pendientes.", "AppTpv",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (_lineas.Count == 0)
        {
            MessageBox.Show("No hay productos en el pedido.", "AppTpv",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var desglose = string.Join("\n", _lineas.Select(l =>
            $" {l.Cantidad} x {l.Nombre,-24} {l.Importe:0.00} €"));
        var total = _lineas.Sum(l => l.Importe);

        MessageBox.Show($"SUBTOTAL\n\n{desglose}\n\nTOTAL: {total:0.00} €\n(IVA 10% incluido)",
            "Subtotal", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnCobrar_Click(object sender, RoutedEventArgs e)
    {
        if (_lineas.Any(l => !l.Enviada))
        {
            MessageBox.Show("Elimina o envia los productos pendientes.", "AppTpv",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (_idPedido == 0)
        {
            MessageBox.Show("No hay pedido abierto.", "AppTpv",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (_lineas.Count == 0)
        {
            MessageBox.Show("El pedido esta vacio.", "AppTpv",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var r = Conexion.Enviar("PEDIDO_CONFIRMAR", new { idPedido = _idPedido });
        if (!r.Ok)
        {
            MessageBox.Show(r.Error, "No se pudo cobrar", MessageBoxButton.OK,
                MessageBoxImage.Warning);
            CargarCarta();
            return;
        }

        var total = r.Datos.GetProperty("total").GetDouble();
        _idUltimoPedido = _idPedido;
        _idPedido = 0;
        _lineas.Clear();
        RefrescarPedido();
        CargarCarta();

        MessageBox.Show(
            $"Compra confirmada. Total: {total:0.00} €\nEl stock del almacen ya esta actualizado.",
            "AppTpv", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnTicket_Click(object sender, RoutedEventArgs e)
    {
        var id = _idPedido != 0 ? _idPedido : _idUltimoPedido;
        if (id == 0)
        {
            MessageBox.Show("Aun no hay pedido para ver su ticket.", "AppTpv",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var r = Conexion.Enviar("TICKET_OBTENER", new { idPedido = id });
        if (!r.Ok)
        {
            MessageBox.Show(r.Error, "AppTpv", MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var t = r.Datos.GetProperty("ticket");
        var lineas = JsonSerializer.Deserialize<List<LineaVista>>(
            t.GetProperty("lineas").GetRawText()) ?? new();
        var desglose = string.Join("\n", lineas.Select(l =>
            $" {l.Cantidad} x {l.Nombre,-24} {l.Importe:0.00} €"));

        var texto = $"Ticket del pedido {t.GetProperty("idPedido").GetInt32()}\n" +
                    $"Cliente: {t.GetProperty("cliente").GetString()}\n" +
                    $"Fecha:   {t.GetProperty("fecha").GetString()}\n" +
                    $"\n{desglose}\n" +
                    $"\nTOTAL: {t.GetProperty("total").GetDouble():0.00} € (IVA 10% incluido)";
        MessageBox.Show(texto, "Ticket", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    // ---------- Refresco de la vista ----------

    private void RefrescarPedido()
    {
        AgruparLineasEnviadas();

        ListaPedido.ItemsSource = null;
        ListaPedido.ItemsSource = _lineas;

        var total = _lineas.Sum(l => l.Importe);
        TxtTotal.Text = $"{total:0.00} €";

        TxtEstadoPedido.Text = _idPedido == 0
            ? "sin pedido abierto"
            : $"pedido n. {_idPedido}";
    }

    /// <summary>
    /// menu.js agrupa por producto las lineas ya enviadas. Aqui se hace lo
    /// mismo para que una comanda reabierta no muestre la misma linea dos
    /// veces (las pendientes se mantienen aparte, como en el comandero web).
    /// </summary>
    private void AgruparLineasEnviadas()
    {
        var enviadas = _lineas.Where(l => l.Enviada).ToList();
        if (enviadas.Count <= 1) return;

        foreach (var grupo in enviadas.GroupBy(l =>
                     l.IdProducto != 0 ? $"id:{l.IdProducto}" : $"n:{l.Nombre}"))
        {
            var primera = grupo.First();
            if (grupo.Count() == 1) continue;

            primera.Cantidad = grupo.Sum(l => l.Cantidad);
            foreach (var repetida in grupo.Skip(1)) _lineas.Remove(repetida);
        }
    }
}
