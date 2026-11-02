// ============================================================
//  AppTpv — Interfaz 3 de 5: RESERVAS (logica)
// ============================================================
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using ClienteWPF.Servicios;

namespace ClienteWPF.Vistas;

public partial class VistaReservas : UserControl
{
    public VistaReservas()
    {
        InitializeComponent();
        Loaded += (_, _) => { CargarMesas(); Refrescar(); };
    }

    private void CargarMesas()
    {
        var r = Conexion.Enviar("MESAS_LISTA");
        if (!r.Ok) return;
        var mesas = JsonSerializer.Deserialize<List<MesaVista>>(r.Datos.GetProperty("mesas").GetRawText()) ?? new();
        CboMesa.ItemsSource = mesas;
        if (mesas.Count > 0) CboMesa.SelectedIndex = 0;
    }

    private void Refrescar()
    {
        var filtro = new { fecha = TxtFiltroFecha.Text.Trim() };
        var r = Conexion.Enviar("RESERVA_LISTA", filtro);
        TxtAviso.Text = r.Ok ? "" : r.Error;
        var reservas = r.Ok
            ? JsonSerializer.Deserialize<List<ReservaVista>>(r.Datos.GetProperty("reservas").GetRawText()) ?? new()
            : new List<ReservaVista>();
        GridReservas.ItemsSource = reservas;
    }

    private void BtnRefrescar_Click(object sender, RoutedEventArgs e) => Refrescar();

    private (MesaVista mesa, string turno, int comensales, string fecha,
             string cliente, string obs, bool error) LeerForm()
    {
        var mesa = CboMesa.SelectedItem as MesaVista;
        var turno = (CboTurno.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
        int.TryParse(TxtComensales.Text, out var comensales);
        var fecha = TxtFecha.Text.Trim();
        var cliente = TxtCliente.Text.Trim();
        var obs = TxtObservaciones.Text.Trim();
        bool error = mesa == null || turno == "" || comensales <= 0 || cliente == "";
        return (mesa!, turno, comensales, fecha, cliente, obs, error);
    }

    private void BtnCrear_Click(object sender, RoutedEventArgs e)
    {
        var f = LeerForm();
        if (f.error)
        {
            TxtAviso.Text = "Completa mesa, turno, comensales (> 0) y nombre del cliente.";
            return;
        }

        var r = Conexion.Enviar("RESERVA_CREAR", new
        {
            idMesa = f.mesa.Id,
            turno = f.turno,
            numComensales = f.comensales,
            fecha = f.fecha,
            nombreCliente = f.cliente,
            observaciones = f.obs
        });
        TxtAviso.Text = r.Ok ? "Reserva creada." : r.Error;
        if (r.Ok) Refrescar();
    }

    private ReservaVista? ReservaElegida => GridReservas.SelectedItem as ReservaVista;

    private void BtnModificar_Click(object sender, RoutedEventArgs e)
    {
        if (ReservaElegida is not { } elegida)
        {
            TxtAviso.Text = "Elige una reserva de la lista para modificarla.";
            return;
        }
        var f = LeerForm();
        if (f.error)
        {
            TxtAviso.Text = "Completa mesa, turno, comensales (> 0) y nombre del cliente.";
            return;
        }
        // El protocolo modifica campos sueltos; aqui aplicamos lo del formulario
        var r = Conexion.Enviar("RESERVA_MODIFICAR", new
        {
            idReserva = elegida.IdReserva,
            numComensales = f.comensales,
            nombreCliente = f.cliente,
            fecha = f.fecha,
            turno = f.turno,
            observaciones = f.obs
        });
        TxtAviso.Text = r.Ok ? $"Reserva {elegida.IdReserva} modificada." : r.Error;
        if (r.Ok) Refrescar();
    }

    private void BtnBorrar_Click(object sender, RoutedEventArgs e)
    {
        if (ReservaElegida is not { } elegida)
        {
            TxtAviso.Text = "Elige una reserva de la lista para cancelarla.";
            return;
        }
        var confirmo = MessageBox.Show($"¿Cancelas la reserva {elegida.IdReserva}?",
            "AppTpv", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirmo != MessageBoxResult.Yes) return;

        var r = Conexion.Enviar("RESERVA_BORRAR", new { idReserva = elegida.IdReserva });
        TxtAviso.Text = r.Ok ? "Reserva cancelada." : r.Error;
        if (r.Ok) Refrescar();
    }

    // pulsar en una fila la rellena en el formulario para editarla
    private void GridReservas_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ReservaElegida is not { } elegida) return;
        var mesa = ((List<MesaVista>)CboMesa.ItemsSource!).FirstOrDefault(m => m.NumMesa == elegida.NumMesa);
        if (mesa != null) CboMesa.SelectedItem = mesa;
        CboTurno.SelectedIndex = elegida.Turno == "afaria" ? 1 : 0;
        TxtComensales.Text = elegida.NumComensales.ToString();
        TxtFecha.Text = elegida.Fecha == "sin fecha" ? "" : elegida.Fecha;
        TxtCliente.Text = elegida.NombreCliente;
        TxtObservaciones.Text = elegida.Observaciones;
    }
}
