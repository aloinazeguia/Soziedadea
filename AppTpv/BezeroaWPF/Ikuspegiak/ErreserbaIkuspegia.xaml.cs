// ============================================================
//  AppTpv — Interfaz 3 de 5: RESERVAS (logica)
// ============================================================
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using BezeroaWPF.Zerbitzuak;

namespace BezeroaWPF.Ikuspegiak;

public partial class ErreserbaIkuspegia : UserControl
{
    public ErreserbaIkuspegia()
    {
        InitializeComponent();
        Loaded += (_, _) => { MahaiakKargatu(); Eguneratu(); };
    }

    private void MahaiakKargatu()
    {
        var r = Konexioa.Bidali("MESAS_LISTA");
        if (!r.Arrakasta) return;
        var mahaiak = JsonSerializer.Deserialize<List<MahaiDatuak>>(r.Datuak.GetProperty("mahaiak").GetRawText()) ?? new();
        CboMahia.ItemsSource = mahaiak;
        if (mahaiak.Count > 0) CboMahia.SelectedIndex = 0;
    }

    private void Eguneratu()
    {
        var iragazkia = new { data = TxtDataIragazkia.Text.Trim() };
        var r = Konexioa.Bidali("RESERVA_LISTA", iragazkia);
        TxtAbisua.Text = r.Arrakasta ? "" : r.Errorea;
        var erreserbak = r.Arrakasta
            ? JsonSerializer.Deserialize<List<ErreserbaDatuak>>(r.Datuak.GetProperty("erreserbak").GetRawText()) ?? new()
            : new List<ErreserbaDatuak>();
        GridErreserbak.ItemsSource = erreserbak;
    }

    private void BtnEguneratu_Click(object sender, RoutedEventArgs e) => Eguneratu();

    private (MahaiDatuak mahia, string txanda, int pertsonak, string data,
             string bezeroa, string oharrak, bool errorea) FormularioaIrakurri()
    {
        var mahia = CboMahia.SelectedItem as MahaiDatuak;
        var txanda = (CboTxanda.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
        int.TryParse(TxtPertsonak.Text, out var pertsonak);
        var data = TxtData.Text.Trim();
        var bezeroa = TxtBezeroa.Text.Trim();
        var oharrak = TxtOharrak.Text.Trim();
        bool errorea = mahia == null || txanda == "" || pertsonak <= 0 || bezeroa == "";
        return (mahia!, txanda, pertsonak, data, bezeroa, oharrak, errorea);
    }

    private void BtnSortu_Click(object sender, RoutedEventArgs e)
    {
        var f = FormularioaIrakurri();
        if (f.errorea)
        {
            TxtAbisua.Text = "Bete mahaia, txanda, pertsona kopurua (> 0) eta bezeroaren izena.";
            return;
        }

        var r = Konexioa.Bidali("RESERVA_CREAR", new
        {
            mahaiId = f.mahia.Id,
            txanda = f.txanda,
            pertsonaKopurua = f.pertsonak,
            data = f.data,
            bezeroIzena = f.bezeroa,
            oharrak = f.oharrak
        });
        TxtAbisua.Text = r.Arrakasta ? "Erreserba sortu da." : r.Errorea;
        if (r.Arrakasta) Eguneratu();
    }

    private ErreserbaDatuak? AukeratutakoErreserba => GridErreserbak.SelectedItem as ErreserbaDatuak;

    private void BtnAldatu_Click(object sender, RoutedEventArgs e)
    {
        if (AukeratutakoErreserba is not { } aukeratutakoa)
        {
            TxtAbisua.Text = "Aukeratu zerrendako erreserba bat aldatzeko.";
            return;
        }
        var f = FormularioaIrakurri();
        if (f.errorea)
        {
            TxtAbisua.Text = "Bete mahaia, txanda, pertsona kopurua (> 0) eta bezeroaren izena.";
            return;
        }
        // Protokoloak eremu batzuk aldatzen ditu; hemen formularioko balioak aplikatzen dira
        var r = Konexioa.Bidali("RESERVA_MODIFICAR", new
        {
            erreserbaId = aukeratutakoa.ErreserbaId,
            pertsonaKopurua = f.pertsonak,
            bezeroIzena = f.bezeroa,
            data = f.data,
            txanda = f.txanda,
            oharrak = f.oharrak
        });
        TxtAbisua.Text = r.Arrakasta ? $"{aukeratutakoa.ErreserbaId} erreserba aldatu da." : r.Errorea;
        if (r.Arrakasta) Eguneratu();
    }

    private void BtnDesgaitu_Click(object sender, RoutedEventArgs e)
    {
        if (AukeratutakoErreserba is not { } aukeratutakoa)
        {
            TxtAbisua.Text = "Aukeratu zerrendako erreserba bat bertan behera uzteko.";
            return;
        }
        var berretsi = MessageBox.Show($"{aukeratutakoa.ErreserbaId} erreserba bertan behera utzi nahi duzu?",
            "AppTpv", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (berretsi != MessageBoxResult.Yes) return;

        var r = Konexioa.Bidali("RESERVA_BORRAR", new { erreserbaId = aukeratutakoa.ErreserbaId });
        TxtAbisua.Text = r.Arrakasta ? "Erreserba bertan behera utzi da." : r.Errorea;
        if (r.Arrakasta) Eguneratu();
    }

    // fila bat sakatzean formularioa betetzen da editatzeko
    private void GridErreserbak_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (AukeratutakoErreserba is not { } aukeratutakoa) return;
        var mahia = ((List<MahaiDatuak>)CboMahia.ItemsSource!).FirstOrDefault(m => m.MahaiZenbakia == aukeratutakoa.MahaiZenbakia);
        if (mahia != null) CboMahia.SelectedItem = mahia;
        CboTxanda.SelectedIndex = aukeratutakoa.Txanda == "afaria" ? 1 : 0;
        TxtPertsonak.Text = aukeratutakoa.PertsonaKopurua.ToString();
        TxtData.Text = aukeratutakoa.Data == "datarik gabe" ? "" : aukeratutakoa.Data;
        TxtBezeroa.Text = aukeratutakoa.BezeroIzena;
        TxtOharrak.Text = aukeratutakoa.Oharrak;
    }
}
