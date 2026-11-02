// ============================================================
//  AppTpv — Interfaz 4 de 5: ADMINISTRACION DE USUARIOS (logica)
// ============================================================
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using ClienteWPF.Servicios;

namespace ClienteWPF.Vistas;

public partial class VistaUsuarios : UserControl
{
    public VistaUsuarios()
    {
        InitializeComponent();
        Loaded += (_, _) => Refrescar();
    }

    private void Refrescar()
    {
        var r = Conexion.Enviar("USUARIOS_LISTA");
        TxtAviso.Text = r.Ok ? "" : r.Error;
        var usuarios = r.Ok
            ? JsonSerializer.Deserialize<List<UsuarioVista>>(r.Datos.GetProperty("usuarios").GetRawText()) ?? new()
            : new List<UsuarioVista>();
        GridUsuarios.ItemsSource = usuarios.OrderBy(u => u.Id).ToList();
    }

    private void BtnRefrescar_Click(object sender, RoutedEventArgs e) => Refrescar();

    private void BtnCrear_Click(object sender, RoutedEventArgs e)
    {
        var login = TxtLogin.Text.Trim();
        var pass = TxtPass.Password;
        var nombre = TxtNombre.Text.Trim();
        var rol = (CboRol.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "usuario";

        if (login == "" || pass == "" || nombre == "")
        {
            TxtAviso.Text = "Login, contrasena y nombre son obligatorios.";
            return;
        }

        var r = Conexion.Enviar("USUARIO_CREAR", new { login, pass, nombre, rol });
        TxtAviso.Text = r.Ok ? $"Usuario {login} creado." : r.Error;
        if (r.Ok) Refrescar();
    }

    private UsuarioVista? Elegido => GridUsuarios.SelectedItem as UsuarioVista;

    private void BtnGuardar_Click(object sender, RoutedEventArgs e)
    {
        if (Elegido is not { } elegido)
        {
            TxtAviso.Text = "Elige un usuario de la lista para guardarle cambios.";
            return;
        }

        var datos = new Dictionary<string, object> { ["id"] = elegido.Id };
        var nombre = TxtNombre.Text.Trim();
        var rol = (CboRol.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
        var pass = TxtPass.Password;

        if (nombre != "" && nombre != elegido.Nombre) datos["nombre"] = nombre;
        if (rol == "admin" || rol == "usuario") datos["rol"] = rol;
        if (pass != "") datos["pass"] = pass;

        var r = Conexion.Enviar("USUARIO_MODIFICAR", datos);
        TxtAviso.Text = r.Ok ? $"Usuario {elegido.Login} actualizado." : r.Error;
        if (r.Ok) Refrescar();
    }

    private void BtnBorrar_Click(object sender, RoutedEventArgs e)
    {
        if (Elegido is not { } elegido)
        {
            TxtAviso.Text = "Elige un usuario de la lista.";
            return;
        }
        if (elegido.Login == Sesion.Login)
        {
            TxtAviso.Text = "No puedes deshabilitarte a ti mismo.";
            return;
        }
        var confirmo = MessageBox.Show($"¿Deshabilitas a {elegido.Login}? Ya no podra iniciar sesion.",
            "AppTpv", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirmo != MessageBoxResult.Yes) return;

        var r = Conexion.Enviar("USUARIO_BORRAR", new { id = elegido.Id });
        TxtAviso.Text = r.Ok ? "Usuario deshabilitado." : r.Error;
        if (r.Ok) Refrescar();
    }

    // al pulsar una fila se rellena el formulario
    private void GridUsuarios_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Elegido is not { } elegido) return;
        TxtLogin.Text = elegido.Login;
        TxtPass.Clear();
        TxtNombre.Text = elegido.Nombre;
        CboRol.SelectedIndex = elegido.Rol == "admin" ? 0 : 1;
    }
}
