// ============================================================
//  AppTpv — Interfaz 1 de 5: LOGIN (logica)
// ============================================================
using System.Windows;
using ClienteWPF.Servicios;

namespace ClienteWPF.Ventanas;

public partial class VentanaLogin : Window
{
    public VentanaLogin()
    {
        InitializeComponent();
        TxtLogin.Text = "admin";     // comodidad en pruebas; quitar en entrega si molesta
        TxtPass.Focus();
    }

    private void BtnEntrar_Click(object sender, RoutedEventArgs e)
    {
        TxtError.Text = "";
        var login = TxtLogin.Text.Trim();
        var pass = TxtPass.Password;

        if (login == "" || pass == "")
        {
            TxtError.Text = "Escribe el usuario y la contrasena.";
            return;
        }

        BtnEntrar.IsEnabled = false;
        var r = Conexion.Enviar("LOGIN", new { login, pass });
        BtnEntrar.IsEnabled = true;

        if (!r.Ok)
        {
            TxtError.Text = r.Error;
            return;
        }

        // guardamos la sesion para el resto de ventanas
        Sesion.Token  = r.Datos.GetProperty("token").GetString() ?? "";
        Sesion.Login  = login;
        Sesion.Nombre = r.Datos.GetProperty("nombre").GetString() ?? login;
        Sesion.Rol    = r.Datos.GetProperty("rol").GetString() ?? "";

        var principal = new VentanaPrincipal();
        principal.Show();
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        // si el usuario cierra la ventana de login con la X, se corta la conexion
        if (Application.Current.Windows.Count <= 1)
            Conexion.Cerrar();
        base.OnClosed(e);
    }
}
