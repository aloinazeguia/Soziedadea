// ============================================================
//  AppTpv — 5 interfazetako 5.a: IZAKINEN/KARTAREN ADMINISTRAZIOA (logika)
// ============================================================
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using BezeroaWPF.Zerbitzuak;

namespace BezeroaWPF.Ikuspegiak;

public partial class IzakinIkuspegia : UserControl
{
    public IzakinIkuspegia()
    {
        InitializeComponent();
        Loaded += (_, _) => Eguneratu();
    }

    private void Eguneratu()
    {
        var r = Konexioa.Bidali("PRODUCTOS_LISTA");
        TxtAbisua.Text = r.Arrakasta ? "" : r.Errorea;
        var produktuak = r.Arrakasta
            ? JsonSerializer.Deserialize<List<ProduktuDatuak>>(r.Datuak.GetProperty("produktuak").GetRawText()) ?? new()
            : new List<ProduktuDatuak>();
        GridProduktuak.ItemsSource = produktuak;
    }

    private void BtnEguneratu_Click(object sender, RoutedEventArgs e) => Eguneratu();

    private (string izena, double prezioa, int izakinak, string osagaiak, string kategoria, bool errorea) FormularioaIrakurri()
    {
        var izena = TxtIzena.Text.Trim();
        double.TryParse(TxtPrezioa.Text, out var prezioa);
        int.TryParse(TxtIzakinak.Text, out var izakinak);
        var osagaiak = TxtOsagaiak.Text.Trim();
        var kategoria = TxtKategoria.Text.Trim();
        if (kategoria == "") kategoria = "general";
        bool errorea = izena == "" || prezioa <= 0 || izakinak < 0;
        return (izena, prezioa, izakinak, osagaiak, kategoria, errorea);
    }

    private void BtnSortu_Click(object sender, RoutedEventArgs e)
    {
        var f = FormularioaIrakurri();
        if (f.errorea)
        {
            TxtAbisua.Text = "Izena, prezioa (> 0) eta izakinak (>= 0) nahitaezkoak dira.";
            return;
        }
        var r = Konexioa.Bidali("PRODUCTO_AGREGAR", new
        {
            izena = f.izena, prezioa = f.prezioa, izakinak = f.izakinak,
            osagaiak = f.osagaiak, kategoria = f.kategoria
        });
        TxtAbisua.Text = r.Arrakasta ? "Produktua sortu da." : r.Errorea;
        if (r.Arrakasta) Eguneratu();
    }

    private ProduktuDatuak? Aukeratutakoa => GridProduktuak.SelectedItem as ProduktuDatuak;

    private void BtnGorde_Click(object sender, RoutedEventArgs e)
    {
        if (Aukeratutakoa is not { } aukeratutakoa)
        {
            TxtAbisua.Text = "Aukeratu zerrendako produktu bat.";
            return;
        }
        var f = FormularioaIrakurri();
        var datuak = new Dictionary<string, object>
        {
            ["id"] = aukeratutakoa.Id,
            ["osagaiak"] = f.osagaiak,
            ["kategoria"] = f.kategoria
        };
        if (f.izena != "" && f.izena != aukeratutakoa.Izena) datuak["izena"] = f.izena;
        if (f.prezioa > 0) datuak["prezioa"] = f.prezioa;
        if (f.izakinak >= 0) datuak["izakinak"] = f.izakinak;

        var r = Konexioa.Bidali("PRODUCTO_MODIFICAR", datuak);
        TxtAbisua.Text = r.Arrakasta ? $"{aukeratutakoa.Izena} produktua eguneratu da." : r.Errorea;
        if (r.Arrakasta) Eguneratu();
    }

    private void BtnDesgaitu_Click(object sender, RoutedEventArgs e)
    {
        if (Aukeratutakoa is not { } aukeratutakoa)
        {
            TxtAbisua.Text = "Aukeratu zerrendako produktu bat.";
            return;
        }
        var berretsi = MessageBox.Show(
            $"'{aukeratutakoa.Izena}' produktua menutik kendu nahi duzu? (ezabatze logikoa; " +
            "tiketen historikoari ez dio eragingo)", "AppTpv",
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (berretsi != MessageBoxResult.Yes) return;

        var r = Konexioa.Bidali("PRODUCTO_BORRAR", new { id = aukeratutakoa.Id });
        TxtAbisua.Text = r.Arrakasta ? "Produktua menutik kendu da." : r.Errorea;
        if (r.Arrakasta) Eguneratu();
    }

    private void BtnIzakinak_Click(object sender, RoutedEventArgs e)
    {
        if (Aukeratutakoa is not { } aukeratutakoa)
        {
            TxtAbisua.Text = "Aukeratu zerrendako produktu bat.";
            return;
        }
        if (!int.TryParse(TxtIzakinBerriak.Text, out var izakinak) || izakinak < 0)
        {
            TxtAbisua.Text = "Sartu izakin-kopuru baliozko bat (>= 0).";
            return;
        }
        var r = Konexioa.Bidali("STOCK_MODIFICAR", new { id = aukeratutakoa.Id, izakinak });
        TxtAbisua.Text = r.Arrakasta ? $"'{aukeratutakoa.Izena}' produktuaren izakinak {izakinak} dira." : r.Errorea;
        if (r.Arrakasta) Eguneratu();
    }

    // fila bat sakatzean formularioa betetzen da
    private void GridProduktuak_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Aukeratutakoa is not { } aukeratutakoa) return;
        TxtIzena.Text = aukeratutakoa.Izena;
        TxtPrezioa.Text = aukeratutakoa.Prezioa.ToString("0.00");
        TxtIzakinak.Text = aukeratutakoa.Izakinak.ToString();
        TxtOsagaiak.Text = aukeratutakoa.Osagaiak;
        TxtKategoria.Text = aukeratutakoa.Kategoria;
        TxtIzakinBerriak.Text = "";
    }
}
