// ============================================================
//  AppTpv — 5 interfazetako 1.a: LOGINaren logika
// ============================================================
// WPF leihoak eta aplikazioaren elementuak erabiltzeko izen-eremua inportatzen da.
using System.Windows;
// Bezeroaren zerbitzuak, saio-kudeaketa eta zerbitzariarekiko konexioa eskuratzen dira.
using BezeroaWPF.Zerbitzuak;

// Aplikazioaren leihoen izen-eremua zehazten da.
namespace BezeroaWPF.Leihoak;

// Saioa hasteko leihoaren kodea definitzen da, WPF XAML leihoarekin lotuta.
public partial class SaioHasieraLeihoa : Window
{
    // Saioa hasteko leihoaren instantzia berria sortzen du.
    public SaioHasieraLeihoa()
    {
        // XAML fitxategian definitutako interfazea hasieratzen du.
        InitializeComponent();
        // Probetan erabiltzaile-izena aurrez betetzen du; entregan nahi izanez gero kendu.
        TxtErabiltzailea.Text = "admin";
        // Hasierako fokua pasahitzaren eremuan jartzen du.
        TxtPasahitza.Focus();
    }

    // Saioa hasteko botoia sakatzean exekutatzen den gertaera-kudeatzailea.
    private void BtnSartu_Click(object sender, RoutedEventArgs e)
    {
        // Aurreko saiakerako errore-mezua ezabatzen du.
        TxtErrorea.Text = "";
        // Erabiltzaile-izena irakurri eta inguruko zuriuneak kentzen ditu.
        var erabiltzailea = TxtErabiltzailea.Text.Trim();
        // Pasahitzaren eremuan idatzitako balioa irakurtzen du.
        var pasahitza = TxtPasahitza.Password;

        // Erabiltzaile-izena edo pasahitza hutsik dagoen egiaztatzen du.
        if (erabiltzailea == "" || pasahitza == "")
        {
            // Erabiltzaileari beharrezko datuak sartzeko eskatzen dio.
            TxtErrorea.Text = "Sartu erabiltzaile-izena eta pasahitza.";
            // Sarrera hutsarekin eskaera bidaltzea saihesten du.
            return;
        }

        // Eskaera prozesatzen den bitartean botoia berriro sakatzea eragozten du.
        BtnSartu.IsEnabled = false;
        // LOGIN eskaera bidaltzen dio zerbitzariari erabiltzaile-izena eta pasahitzarekin.
        var r = Konexioa.Bidali("LOGIN", new { erabiltzailea, pasahitza });
        // Eskaera amaitu ondoren, botoia berriro erabilgarri uzten du.
        BtnSartu.IsEnabled = true;

        // Zerbitzariak saio-hasiera baztertu duen egiaztatzen du.
        if (!r.Arrakasta)
        {
            // Zerbitzariak emandako errorea interfazean erakusten du.
            TxtErrorea.Text = r.Errorea;
            // Saio-hasiera huts egin denez, metodoa amaitzen du.
            return;
        }

        // Zerbitzariak emandako tokena gordetzen du, beste leihoek erabil dezaten.
        Saioa.Token  = r.Datuak.GetProperty("token").GetString() ?? "";
        // Uneko erabiltzaile-izena saioan gordetzen du.
        Saioa.ErabiltzaileIzena  = erabiltzailea;
        // Zerbitzariak emandako izena gordetzen du; falta bada, erabiltzaile-izena erabiltzen du.
        Saioa.Izena = r.Datuak.GetProperty("izena").GetString() ?? erabiltzailea;
        // Erabiltzailearen rola saioan gordetzen du.
        Saioa.Rola    = r.Datuak.GetProperty("rola").GetString() ?? "";

        // Erabiltzaile autentifikatuarentzako leiho nagusia sortzen du.
        var nagusia = new LeihoNagusia();
        // Leiho nagusia bistaratzen du.
        nagusia.Show();
        // Saioa hasteko leihoa ixten du.
        Close();
    }

    // Leihoa ixten denean WPFk deitzen duen metodoa gainidazten du.
    protected override void OnClosed(EventArgs e)
    {
        // Saioa hasteko leihoa bakarrik geratzen bada, zerbitzariarekiko konexioa ixten du.
        if (Application.Current.Windows.Count <= 1)
            Konexioa.Itxi();
        // Oinarrizko Window klasearen itxiera-prozesuarekin jarraitzen du.
        base.OnClosed(e);
    }
}
