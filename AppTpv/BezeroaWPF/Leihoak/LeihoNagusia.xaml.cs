// ============================================================
//  AppTpv — Leiho nagusia: rolaren arabera ikusgai dauden fitxak
// ============================================================
using System.Windows;
using System.Windows.Controls;
using BezeroaWPF.Zerbitzuak;

namespace BezeroaWPF.Leihoak;

public partial class LeihoNagusia : Window
{
    public LeihoNagusia()
    {
        InitializeComponent();
        var rola = Saioa.AdminDa ? "Administratzailea" : "Erabiltzailea";
        TxtNorea.Text = $"{Saioa.Izena} ({Saioa.ErabiltzaileIzena}) — rola: {rola}";

        // erabiltzaile arruntak ez ditu administrazio-fitxak ikusten (2. eskakizuna)
        if (!Saioa.AdminDa)
        {
            var fitxaErabiltzaileak = Fitxak.Items.OfType<TabItem>().First(t => t.Tag?.ToString() == "Erabiltzaileak");
            var fitxaIzakinak    = Fitxak.Items.OfType<TabItem>().First(t => t.Tag?.ToString() == "Izakinak");
            fitxaErabiltzaileak.Visibility = Visibility.Collapsed;
            fitxaIzakinak.Visibility = Visibility.Collapsed;
        }
    }

    private void BtnIrten_Click(object sender, RoutedEventArgs e)
    {
        Konexioa.Bidali("LOGOUT");
        Saioa.Itxi();
        Konexioa.Itxi();
        new SaioHasieraLeihoa().Show();
        Close();
    }
}
