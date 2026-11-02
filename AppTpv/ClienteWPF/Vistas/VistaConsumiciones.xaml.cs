// ============================================================
//  AppTpv — Interfaz 2 de 5: CONSUMICIONES (logica)
//  Flujo: abrir pedido -> anadir lineas (valida stock el servidor)
//         -> confirmar compra (el servidor DECREMENTA stock)
// ============================================================
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using ClienteWPF.Servicios;
using ClienteWPF.Ventanas;

namespace ClienteWPF.Vistas;

public partial class VistaConsumiciones : UserControl
{
    private int _idPedido;               // 0 = sin pedido todavia
    private int _idUltimoPedido = 0;     // ultimo pedido cobrado (para ver el ticket)
    private readonly List<LineaVista> _lineas = new();

    public VistaConsumiciones()
    {
        InitializeComponent();
        Loaded += (_, _) => RefrescarCarta();
    }

    private List<ProductoVista> _carta = new();

    private void RefrescarCarta()
    {
        var r = Conexion.Enviar("PRODUCTOS_LISTA");
        if (!r.Ok) { TxtCartaInfo.Text = r.Error; return; }

        _carta = JsonSerializer.Deserialize<List<ProductoVista>>(r.Datos.GetProperty("productos").GetRawText()) ?? new();
        var visibles = _carta.Where(p => p.Activo == 1).ToList();
        ListaCarta.ItemsSource = visibles;
    }

    private void BtnAnadir_Click(object sender, RoutedEventArgs e)
    {
        if (_idPedido == 0)
        {
            MessageBox.Show("Primero abre un pedido.", "AppTpv",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (ListaCarta.SelectedItem is not ProductoVista prod)
        {
            TxtCartaInfo.Text = "Elige un producto de la carta.";
            return;
        }

        var cuadrito = new VentanaCantidad(prod.Nombre, prod.Stock);
        cuadrito.Owner = Window.GetWindow(this);
        if (cuadrito.ShowDialog() != true) return;
        var cantidad = cuadrito.Cantidad;

        var r = Conexion.Enviar("PEDIDO_AGREGAR_LINEA",
            new { idPedido = _idPedido, idProducto = prod.Id, cantidad });
        if (!r.Ok) { MessageBox.Show(r.Error, "No se pudo anadir"); RefrescarCarta(); return; }

        _lineas.Add(new LineaVista
        {
            Nombre = prod.Nombre,
            Cantidad = cantidad,
            PrecioUnitario = r.Datos.GetProperty("precioUnitario").GetDouble()
        });
        RefrescarPedido();
        RefrescarCarta();                  // el stock baja al confirmar; aqui avisamos
        TxtCartaInfo.Text = "Linea anadida (el stock baja al confirmar la compra)";
    }

    private void BtnAbrir_Click(object sender, RoutedEventArgs e)
    {
        var r = Conexion.Enviar("PEDIDO_ABRIR");
        if (!r.Ok)
        {
            // si ya habia uno abierto, el servidor dice su numero: lo adoptamos
            var m = Regex.Match(r.Error, @"Ya tienes el pedido (\d+) abierto");
            if (m.Success) { _idPedido = int.Parse(m.Groups[1].Value); }
            else { MessageBox.Show(r.Error, "AppTpv"); return; }
        }
        else
        {
            _idPedido = r.Datos.GetProperty("idPedido").GetInt32();
        }

        TxtPedidoTitulo.Text = $"Pedido n. {_idPedido}";
        BtnAbrir.IsEnabled = false;
        RefrescarPedido();
    }

    private void RefrescarPedido()
    {
        ListaLineas.ItemsSource = null;
        ListaLineas.ItemsSource = _lineas;
        TxtTotal.Text = $"{_lineas.Sum(l => l.Importe):C2}";
    }

    private void BtnQuitar_Click(object sender, RoutedEventArgs e)
    {
        // quitar de la vista: el pedido real no tiene lineas firmes hasta confirmar
        if (ListaLineas.SelectedItem is LineaVista linea)
        {
            // cancelamos solo si el servidor lo permite? El protocolo no borra
            // lineas: para simplicidad, avisamos de anular la cuenta entera.
            MessageBox.Show("Para volver atras de una linea, cancela el pedido y abre otro.",
                "AppTpv", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void BtnConfirmar_Click(object sender, RoutedEventArgs e)
    {
        if (_idPedido == 0) { MessageBox.Show("No hay pedido abierto."); return; }

        var r = Conexion.Enviar("PEDIDO_CONFIRMAR", new { idPedido = _idPedido });
        if (!r.Ok) { MessageBox.Show(r.Error, "No se pudo confirmar"); RefrescarCarta(); return; }

        var total = r.Datos.GetProperty("total").GetDouble();
        _idUltimoPedido = _idPedido;
        _idPedido = 0;
        _lineas.Clear();
        TextPedidoTitulo_vacio();
        RefrescarPedido();
        RefrescarCarta();
        MessageBox.Show($"Compra confirmada. Total: {total:C2}\nEl stock del almacen ya esta actualizado.",
            "AppTpv", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void TextPedidoTitulo_vacio()
    {
        TxtPedidoTitulo.Text = "Pedido (aun no abierto)";
        BtnAbrir.IsEnabled = true;
    }

    private void BtnTicket_Click(object sender, RoutedEventArgs e)
    {
        var id = _idPedido != 0 ? _idPedido : _idUltimoPedido;
        if (id == 0)
        {
            MessageBox.Show("Aun no hay pedido para ver su ticket.", "AppTpv");
            return;
        }
        var r = Conexion.Enviar("TICKET_OBTENER", new { idPedido = id });
        if (!r.Ok) { MessageBox.Show(r.Error); return; }

        var t = r.Datos.GetProperty("ticket");
        var lineas = JsonSerializer.Deserialize<List<LineaVista>>(
            t.GetProperty("lineas").GetRawText()) ?? new();
        var desglose = string.Join("\n", lineas.Select(l =>
            $" {l.Cantidad} x {l.Nombre,-24} {l.Importe:C2}"));

        var texto = $"Ticket del pedido {t.GetProperty("idPedido").GetInt32()}\n" +
                    $"Cliente: {t.GetProperty("cliente").GetString()}\n" +
                    $"Fecha:   {t.GetProperty("fecha").GetString()}\n" +
                    $"\n{desglose}\n" +
                    $"\nTOTAL: {t.GetProperty("total").GetDouble():C2} (IVA 10% incluido)";
        MessageBox.Show(texto, "Ticket", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
