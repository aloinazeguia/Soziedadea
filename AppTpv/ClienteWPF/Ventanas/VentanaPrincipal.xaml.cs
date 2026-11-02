// ============================================================
//  AppTpv — Ventana principal: pestañas disponibles segun el rol
// ============================================================
using System.Windows;
using System.Windows.Controls;
using ClienteWPF.Servicios;

namespace ClienteWPF.Ventanas;

public partial class VentanaPrincipal : Window
{
    public VentanaPrincipal()
    {
        InitializeComponent();
        TxtQuien.Text = $"{Sesion.Nombre} ({Sesion.Login}) — rol: {Sesion.Rol}";

        // el usuario comun no ves las fichas de administracion (requisito 2)
        if (!Sesion.EsAdmin)
        {
            var fichaUsuarios = Fichas.Items.OfType<TabItem>().First(t => t.Header.ToString() == "Usuarios");
            var fichaStock    = Fichas.Items.OfType<TabItem>().First(t => t.Header.ToString() == "Stock");
            fichaUsuarios.Visibility = Visibility.Collapsed;
            fichaStock.Visibility = Visibility.Collapsed;
        }
    }

    private void BtnSalir_Click(object sender, RoutedEventArgs e)
    {
        Conexion.Enviar("LOGOUT");
        Sesion.Cerrar();
        Conexion.Cerrar();
        new VentanaLogin().Show();
        Close();
    }
}
