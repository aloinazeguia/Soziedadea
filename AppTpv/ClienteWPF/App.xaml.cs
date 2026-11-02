// ============================================================
//  AppTpv — Arranque: pantalla de LOGIN primero; al cerrar la app
//  se corta la conexion TCP con el servidor.
// ============================================================
using System.Windows;
using ClienteWPF.Servicios;
using ClienteWPF.Ventanas;

namespace ClienteWPF;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        new VentanaLogin().Show();
    }

    private void App_Exit(object sender, ExitEventArgs e)
    {
        // cortesia con el servidor: libera el hueco de los 10 clientes
        if (Sesion.Token != "")
            Conexion.Enviar("LOGOUT");
        Conexion.Cerrar();
    }
}
