// ============================================================
//  AppTpv — Interfaz 5 de 5: ADMINISTRACION DE STOCK/CARTA (logica)
// ============================================================
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using ClienteWPF.Servicios;

namespace ClienteWPF.Vistas;

public partial class VistaStock : UserControl
{
    public VistaStock()
    {
        InitializeComponent();
        Loaded += (_, _) => Refrescar();
    }

    private void Refrescar()
    {
        var r = Conexion.Enviar("PRODUCTOS_LISTA");
        TxtAviso.Text = r.Ok ? "" : r.Error;
        var productos = r.Ok
            ? JsonSerializer.Deserialize<List<ProductoVista>>(r.Datos.GetProperty("productos").GetRawText()) ?? new()
            : new List<ProductoVista>();
        GridProductos.ItemsSource = productos;
    }

    private void BtnRefrescar_Click(object sender, RoutedEventArgs e) => Refrescar();

    private (string nombre, double precio, int stock, string ing, string cat, bool error) LeerForm()
    {
        var nombre = TxtNombre.Text.Trim();
        double.TryParse(TxtPrecio.Text, out var precio);
        int.TryParse(TxtStock.Text, out var stock);
        var ing = TxtIngredientes.Text.Trim();
        var cat = TxtCategoria.Text.Trim();
        if (cat == "") cat = "general";
        bool error = nombre == "" || precio <= 0 || stock < 0;
        return (nombre, precio, stock, ing, cat, error);
    }

    private void BtnCrear_Click(object sender, RoutedEventArgs e)
    {
        var f = LeerForm();
        if (f.error)
        {
            TxtAviso.Text = "Nombre, precio (> 0) y stock (>= 0) son obligatorios.";
            return;
        }
        var r = Conexion.Enviar("PRODUCTO_AGREGAR", new
        {
            nombre = f.nombre, precio = f.precio, stock = f.stock,
            ingredientes = f.ing, categoria = f.cat
        });
        TxtAviso.Text = r.Ok ? "Producto creado." : r.Error;
        if (r.Ok) Refrescar();
    }

    private ProductoVista? Elegido => GridProductos.SelectedItem as ProductoVista;

    private void BtnGuardar_Click(object sender, RoutedEventArgs e)
    {
        if (Elegido is not { } elegido)
        {
            TxtAviso.Text = "Elige un producto de la lista.";
            return;
        }
        var f = LeerForm();
        var datos = new Dictionary<string, object>
        {
            ["id"] = elegido.Id,
            ["ingredientes"] = f.ing,
            ["categoria"] = f.cat
        };
        if (f.nombre != "" && f.nombre != elegido.Nombre) datos["nombre"] = f.nombre;
        if (f.precio > 0) datos["precio"] = f.precio;
        if (f.stock >= 0) datos["stock"] = f.stock;

        var r = Conexion.Enviar("PRODUCTO_MODIFICAR", datos);
        TxtAviso.Text = r.Ok ? $"Producto {elegido.Nombre} actualizado." : r.Error;
        if (r.Ok) Refrescar();
    }

    private void BtnBorrar_Click(object sender, RoutedEventArgs e)
    {
        if (Elegido is not { } elegido)
        {
            TxtAviso.Text = "Elige un producto de la lista.";
            return;
        }
        var confirmo = MessageBox.Show(
            $"¿Quitas '{elegido.Nombre}' de la carta? (borrado logico; la historia " +
            "de tickets no se ve afectada)", "AppTpv",
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirmo != MessageBoxResult.Yes) return;

        var r = Conexion.Enviar("PRODUCTO_BORRAR", new { id = elegido.Id });
        TxtAviso.Text = r.Ok ? "Producto quitado de la carta." : r.Error;
        if (r.Ok) Refrescar();
    }

    private void BtnStock_Click(object sender, RoutedEventArgs e)
    {
        if (Elegido is not { } elegido)
        {
            TxtAviso.Text = "Elige un producto de la lista.";
            return;
        }
        if (!int.TryParse(TxtNuevoStock.Text, out var stock) || stock < 0)
        {
            TxtAviso.Text = "Escribe un stock valido (>= 0).";
            return;
        }
        var r = Conexion.Enviar("STOCK_MODIFICAR", new { id = elegido.Id, stock });
        TxtAviso.Text = r.Ok ? $"Stock de '{elegido.Nombre}' puesto a {stock}." : r.Error;
        if (r.Ok) Refrescar();
    }

    // al pulsar una fila se rellena el formulario
    private void GridProductos_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Elegido is not { } elegido) return;
        TxtNombre.Text = elegido.Nombre;
        TxtPrecio.Text = elegido.Precio.ToString("0.00");
        TxtStock.Text = elegido.Stock.ToString();
        TxtIngredientes.Text = elegido.Ingredientes;
        TxtCategoria.Text = elegido.Categoria;
        TxtNuevoStock.Text = "";
    }
}
