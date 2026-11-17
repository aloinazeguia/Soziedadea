// ============================================================
//  AppTpv — Ventana auxiliar: pedir una cantidad
// ============================================================
using System.Windows;

namespace ClienteWPF.Ventanas;

public partial class VentanaCantidad : Window
{
    public int Cantidad { get; private set; }
    private readonly int _stockMaximo;

    // si stock >= 1 se avisa, pero el servidor valida en firme
    public VentanaCantidad(string producto, int stock)
    {
        InitializeComponent();
        TxtTitulo.Text = producto;
        TxtStock.Text = stock <= 0
            ? "Ez dago unitaterik biltegian"
            : $"Eskuragarri dauden unitateak: {stock}";
        _stockMaximo = stock;
    }

    private void BtnOk_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(TxtCantidad.Text, out var n) || n <= 0)
        {
            MessageBox.Show("Sartu unitate-kopuru baliozko bat.", "AppTpv");
            return;
        }
        // nota: es un aviso suave; el stock real lo valida el servidor al anadir
        Cantidad = n;
        DialogResult = true;
    }
}
