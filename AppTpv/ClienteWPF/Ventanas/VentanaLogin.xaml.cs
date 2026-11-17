// ============================================================
//  AppTpv — 5 interfazetako 1.a: LOGINaren logika
// ============================================================
// WPF leihoak eta aplikazioaren elementuak erabiltzeko izen-eremua inportatzen da.
using System.Windows;
// Bezeroaren zerbitzuak, saio-kudeaketa eta zerbitzariarekiko konexioa eskuratzen dira.
using ClienteWPF.Servicios;

// Aplikazioaren leihoen izen-eremua zehazten da.
namespace ClienteWPF.Ventanas;

// Saioa hasteko leihoaren kodea definitzen da, WPF XAML leihoarekin lotuta.
public partial class VentanaLogin : Window
{
    // Saioa hasteko leihoaren instantzia berria sortzen du.
    public VentanaLogin()
    {
        // XAML fitxategian definitutako interfazea hasieratzen du.
        InitializeComponent();
        // Probetan erabiltzaile-izena aurrez betetzen du; entregan nahi izanez gero kendu.
        TxtLogin.Text = "admin";
        // Hasierako fokua pasahitzaren eremuan jartzen du.
        TxtPass.Focus();
    }

    // Saioa hasteko botoia sakatzean exekutatzen den gertaera-kudeatzailea.
    private void BtnEntrar_Click(object sender, RoutedEventArgs e)
    {
        // Aurreko saiakerako errore-mezua ezabatzen du.
        TxtError.Text = "";
        // Erabiltzaile-izena irakurri eta inguruko zuriuneak kentzen ditu.
        var login = TxtLogin.Text.Trim();
        // Pasahitzaren eremuan idatzitako balioa irakurtzen du.
        var pass = TxtPass.Password;

        // Erabiltzaile-izena edo pasahitza hutsik dagoen egiaztatzen du.
        if (login == "" || pass == "")
        {
            // Erabiltzaileari beharrezko datuak sartzeko eskatzen dio.
            TxtError.Text = "Sartu erabiltzaile-izena eta pasahitza.";
            // Sarrera hutsarekin eskaera bidaltzea saihesten du.
            return;
        }

        // Eskaera prozesatzen den bitartean botoia berriro sakatzea eragozten du.
        BtnEntrar.IsEnabled = false;
        // LOGIN eskaera bidaltzen dio zerbitzariari erabiltzaile-izena eta pasahitzarekin.
        var r = Conexion.Enviar("LOGIN", new { login, pass });
        // Eskaera amaitu ondoren, botoia berriro erabilgarri uzten du.
        BtnEntrar.IsEnabled = true;

        // Zerbitzariak saio-hasiera baztertu duen egiaztatzen du.
        if (!r.Ok)
        {
            // Zerbitzariak emandako errorea interfazean erakusten du.
            TxtError.Text = r.Error;
            // Saio-hasiera huts egin denez, metodoa amaitzen du.
            return;
        }

        // Zerbitzariak emandako tokena gordetzen du, beste leihoek erabil dezaten.
        Sesion.Token  = r.Datos.GetProperty("token").GetString() ?? "";
        // Uneko erabiltzaile-izena saioan gordetzen du.
        Sesion.Login  = login;
        // Zerbitzariak emandako izena gordetzen du; falta bada, erabiltzaile-izena erabiltzen du.
        Sesion.Nombre = r.Datos.GetProperty("nombre").GetString() ?? login;
        // Erabiltzailearen rola saioan gordetzen du.
        Sesion.Rol    = r.Datos.GetProperty("rol").GetString() ?? "";

        // Erabiltzaile autentifikatuarentzako leiho nagusia sortzen du.
        var principal = new VentanaPrincipal();
        // Leiho nagusia bistaratzen du.
        principal.Show();
        // Saioa hasteko leihoa ixten du.
        Close();
    }

    // Leihoa ixten denean WPFk deitzen duen metodoa gainidazten du.
    protected override void OnClosed(EventArgs e)
    {
        // Saioa hasteko leihoa bakarrik geratzen bada, zerbitzariarekiko konexioa ixten du.
        if (Application.Current.Windows.Count <= 1)
            Conexion.Cerrar();
        // Oinarrizko Window klasearen itxiera-prozesuarekin jarraitzen du.
        base.OnClosed(e);
    }
}
