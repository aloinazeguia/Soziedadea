// ============================================================
//  AppTpv — 5 interfazetako 4.a: ERABILTZAILEEN ADMINISTRAZIOA (logika)
// ============================================================
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using BezeroaWPF.Zerbitzuak;

namespace BezeroaWPF.Ikuspegiak;

public partial class ErabiltzaileIkuspegia : UserControl
{
    public ErabiltzaileIkuspegia()
    {
        InitializeComponent();
        Loaded += (_, _) => Eguneratu();
    }

    private void Eguneratu()
    {
        var r = Konexioa.Bidali("USUARIOS_LISTA");
        TxtAbisua.Text = r.Arrakasta ? "" : r.Errorea;
        var erabiltzaileak = r.Arrakasta
            ? JsonSerializer.Deserialize<List<ErabiltzaileDatuak>>(r.Datuak.GetProperty("erabiltzaileak").GetRawText()) ?? new()
            : new List<ErabiltzaileDatuak>();
        GridErabiltzaileak.ItemsSource = erabiltzaileak.OrderBy(u => u.Id).ToList();
    }

    private void BtnEguneratu_Click(object sender, RoutedEventArgs e) => Eguneratu();

    private void BtnSortu_Click(object sender, RoutedEventArgs e)
    {
        var erabiltzailea = TxtErabiltzailea.Text.Trim();
        var pasahitza = TxtPasahitza.Password;
        var izena = TxtIzena.Text.Trim();
        var rola = (CboRola.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "usuario";

        if (erabiltzailea == "" || pasahitza == "" || izena == "")
        {
            TxtAbisua.Text = "Erabiltzaile-izena, pasahitza eta izena nahitaezkoak dira.";
            return;
        }

        var r = Konexioa.Bidali("USUARIO_CREAR", new { erabiltzailea, pasahitza, izena, rola });
        TxtAbisua.Text = r.Arrakasta ? $"{erabiltzailea} erabiltzailea sortu da." : r.Errorea;
        if (r.Arrakasta) Eguneratu();
    }

    private ErabiltzaileDatuak? Aukeratutakoa => GridErabiltzaileak.SelectedItem as ErabiltzaileDatuak;

    private void BtnGorde_Click(object sender, RoutedEventArgs e)
    {
        if (Aukeratutakoa is not { } aukeratutakoa)
        {
            TxtAbisua.Text = "Aukeratu zerrendako erabiltzaile bat aldaketak gordetzeko.";
            return;
        }

        var datuak = new Dictionary<string, object> { ["id"] = aukeratutakoa.Id };
        var izena = TxtIzena.Text.Trim();
        var rola = (CboRola.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "";
        var pasahitza = TxtPasahitza.Password;

        if (izena != "" && izena != aukeratutakoa.Izena) datuak["izena"] = izena;
        if (rola == "admin" || rola == "usuario") datuak["rola"] = rola;
        if (pasahitza != "") datuak["pasahitza"] = pasahitza;

        var r = Konexioa.Bidali("USUARIO_MODIFICAR", datuak);
        TxtAbisua.Text = r.Arrakasta ? $"{aukeratutakoa.ErabiltzaileIzena} erabiltzailea eguneratu da." : r.Errorea;
        if (r.Arrakasta) Eguneratu();
    }

    private void BtnDesgaitu_Click(object sender, RoutedEventArgs e)
    {
        if (Aukeratutakoa is not { } aukeratutakoa)
        {
            TxtAbisua.Text = "Aukeratu zerrendako erabiltzaile bat.";
            return;
        }
        if (aukeratutakoa.ErabiltzaileIzena == Saioa.ErabiltzaileIzena)
        {
            TxtAbisua.Text = "Ezin duzu zeure burua desgaitu.";
            return;
        }
        var berretsi = MessageBox.Show($"{aukeratutakoa.ErabiltzaileIzena} erabiltzailea desgaitu nahi duzu? Ezingo du saioa hasi aurrerantzean.",
            "AppTpv", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (berretsi != MessageBoxResult.Yes) return;

        var r = Konexioa.Bidali("USUARIO_BORRAR", new { id = aukeratutakoa.Id });
        TxtAbisua.Text = r.Arrakasta ? "Erabiltzailea desgaitu da." : r.Errorea;
        if (r.Arrakasta) Eguneratu();
    }

    // fila bat sakatzean formularioa betetzen da
    private void GridErabiltzaileak_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Aukeratutakoa is not { } aukeratutakoa) return;
        TxtErabiltzailea.Text = aukeratutakoa.ErabiltzaileIzena;
        TxtPasahitza.Clear();
        TxtIzena.Text = aukeratutakoa.Izena;
        CboRola.SelectedIndex = aukeratutakoa.Rola == "admin" ? 0 : 1;
    }
}
