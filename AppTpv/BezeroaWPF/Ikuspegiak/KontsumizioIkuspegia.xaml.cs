// ============================================================
//  AppTpv — 5 interfazetako 2.a: KONTSUMIZIOAK (logika)
//
//  Comandero/js/menu.js + pedido.js logika bera:
//   1. Zerbitzariak kategoria aukeratzen du -> produktuak.
//   2. Produktua sakatzean, eskaera ZAINean metatzen da
//      (oraindik ez da zerbitzarira bidaltzen).
//   3. "Bidali" komanda irekia ireki/birerabili eta lerro zainak
//      bidaltzen ditu (berde bihurtzen dira).
//   4. "AZPITOTALA"k xehetasuna erakusten du; zainik ez badago soilik.
//   5. "KOBRATU"k erosketa baieztatzen du (zerbitzariak izakinak murrizten ditu).
// ============================================================
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using BezeroaWPF.Zerbitzuak;

namespace BezeroaWPF.Ikuspegiak;

public partial class KontsumizioIkuspegia : UserControl
{
    private int _eskaeraId;               // 0 = oraindik eskaerarik gabe
    private int _azkenEskaeraId;          // kobratutako azken eskaera (txartela ikusteko)
    private int _mahaiId;                 // eskaera irekiari lotutako mahia (0 = gabe)

    // Komandaren lerroak: zainak (lokala) + bidalitakoak (komanda)
    private readonly List<EskaeraLerroa> _lerroak = new();

    private List<ProduktuDatuak> _menua = new();
    private List<MahaiDatuak> _mahaiak = new();
    private string _kategoriaUnekoa = "";

    public KontsumizioIkuspegia()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            MenuaKargatu();
            MahaiakKargatu();
            EskaeraBerreskuratu();   // gailu berean zein bestean: saioa irekitzean osagaiak agertu
        };
    }

    // ---------- Carta / kategoriak ----------

    private void MenuaKargatu()
    {
        var r = Konexioa.Bidali("PRODUCTOS_LISTA");
        if (!r.Arrakasta) { TxtMenuInfo.Text = r.Errorea; return; }

        _menua = JsonSerializer.Deserialize<List<ProduktuDatuak>>(
            r.Datuak.GetProperty("produktuak").GetRawText()) ?? new();
        _menua = _menua.Where(p => p.Aktibo == 1).ToList();

        var kategoriak = _menua
            .Select(p => p.Kategoria)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct()
            .OrderBy(c => c)
            .ToList();

        ListaKategoriak.ItemsSource = kategoriak;

        if (kategoriak.Count == 0)
        {
            _kategoriaUnekoa = "";
            TxtKategoriaIzenburua.Text = "PRODUKTUAK";
            ProduktuakErakutsi(new List<ProduktuDatuak>());
            return;
        }

        var indizea = kategoriak.IndexOf(_kategoriaUnekoa);
        ListaKategoriak.SelectedIndex = indizea >= 0 ? indizea : 0;
    }

    // ---------- Mahaiak / eskaera irekiaren berreskuratzea ----------

    private void MahaiakKargatu()
    {
        var r = Konexioa.Bidali("MESAS_LISTA");
        if (!r.Arrakasta) return;

        _mahaiak = JsonSerializer.Deserialize<List<MahaiDatuak>>(
            r.Datuak.GetProperty("mahaiak").GetRawText()) ?? new();
        CboMahaiak.ItemsSource = _mahaiak;
    }

    /// <summary>
    /// Saioa itxi/ireki ondoren (gailu berean nahiz beste batean) zerbitzariari
    /// eskatzen dio ordaindu gabeko eskaera eta haren osagai-zerrenda.
    /// Datuak BDn daudenez, UI-egoera berreraiki egiten da.
    /// </summary>
    private void EskaeraBerreskuratu()
    {
        var r = Konexioa.Bidali("PEDIDO_REKUPERATU");
        if (!r.Arrakasta)
        {
            TxtMenuInfo.Text = r.Errorea;
            return;
        }

        var berreskuratua = JsonSerializer.Deserialize<EskaeraBerreskuratzea>(
            r.Datuak.GetRawText());
        if (berreskuratua == null || berreskuratua.EskaeraId == 0)
        {
            _eskaeraId = 0;
            _mahaiId = 0;
            _lerroak.Clear();
            CboMahaiak.SelectedIndex = -1;
            EskaeraEguneratu();
            return;
        }

        _eskaeraId = berreskuratua.EskaeraId;
        _mahaiId = berreskuratua.MahaiId ?? 0;
        _lerroak.Clear();
        foreach (var l in berreskuratua.Lerroak)
        {
            _lerroak.Add(new EskaeraLerroa
            {
                ProduktuId = l.ProduktuId,
                Izena = l.Izena,
                Kopurua = l.Kopurua,
                PrezioUnitarioa = l.PrezioUnitarioa,
                Egoera = "bidalita"
            });
        }

        if (_mahaiId > 0)
            CboMahaiak.SelectedValue = _mahaiId;
        EskaeraEguneratu();

        if (_lerroak.Count > 0)
            TxtMenuInfo.Text = $"{_lerroak.Count} osagai berreskuratu dira (eskaera zk. {_eskaeraId}).";
    }

    /// <summary>Eskaera irekia mahai bati lotzen dio, mapak egoera erakutsi dezan.</summary>
    private void BtnMahiaEsleitu_Click(object sender, RoutedEventArgs e)
    {
        if (CboMahaiak.SelectedItem is not MahaiDatuak mahai)
        {
            MessageBox.Show("Aukeratu mahai bat zerrendatik.", "AppTpv",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (_eskaeraId == 0)
        {
            MessageBox.Show("Lehenik gehitu produktuak eta bidali eskaera.", "AppTpv",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var r = Konexioa.Bidali("PEDIDO_MAHAIA_ESLEITU", new
        {
            eskaeraId = _eskaeraId,
            mahaiId = mahai.Id
        });
        if (!r.Arrakasta)
        {
            MessageBox.Show(r.Errorea, "Ezin izan da mahaia esleitu", MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        _mahaiId = mahai.Id;
        EskaeraEguneratu();
        TxtMenuInfo.Text = $"{mahai.MahaiZenbakia}. mahaia eskaerari lotu zaio.";
    }

    private void ListaKategoriak_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ListaKategoriak.SelectedItem is not string kategoria) return;

        _kategoriaUnekoa = kategoria;
        TxtKategoriaIzenburua.Text = kategoria.ToUpperInvariant();
        ProduktuakErakutsi(_menua.Where(p => p.Kategoria == kategoria).ToList());
    }

    private void ProduktuakErakutsi(List<ProduktuDatuak> produktuak)
    {
        ListaProduktuak.ItemsSource = produktuak;
        TxtMenuInfo.Text = produktuak.Count == 0
            ? "Ez dago produkturik kategoria honetan."
            : "";
    }

    // ---------- Eskaera: lerroak gehitu / editatu ----------

    private void Produktua_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.Tag is not ProduktuDatuak produktua) return;

        if (produktua.Agortuta)
        {
            TxtMenuInfo.Text = $"{produktua.Izena}: ez da unitaterik geratzen.";
            return;
        }

        // pedido.js bezala: produktu beraren lerro zain bat badago,
        // kopurua metatzen da; bidalitakoak ezin dira aldatu.
        var zain = _lerroak.FirstOrDefault(l => !l.Bidalita && l.ProduktuId == produktua.Id);
        if (zain != null)
        {
            zain.Kopurua++;
        }
        else
        {
            _lerroak.Add(new EskaeraLerroa
            {
                ProduktuId = produktua.Id,
                Izena = produktua.Izena,
                Kopurua = 1,
                PrezioUnitarioa = produktua.Prezioa,
                Egoera = "zain"
            });
        }

        EskaeraEguneratu();
        TxtMenuInfo.Text = $"{produktua.Izena} eskaerara gehitu da.";
    }

    private void ListaEskaera_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // editorea lerro zainetarako soilik irekitzen da
        if (ListaEskaera.SelectedItem is EskaeraLerroa { Bidalita: false } lerroa)
        {
            TxtKopuruEditorea.Text = lerroa.Kopurua.ToString();
            EditorePanela.Visibility = Visibility.Visible;
        }
        else
        {
            EditorePanela.Visibility = Visibility.Collapsed;
        }
    }

    private void BtnGordeLerroa_Click(object sender, RoutedEventArgs e)
    {
        if (ListaEskaera.SelectedItem is not EskaeraLerroa { Bidalita: false } lerroa) return;

        if (!int.TryParse(TxtKopuruEditorea.Text, out var kopurua) || kopurua < 1)
        {
            MessageBox.Show("Sartu kopuru baliozko bat (1 edo gehiago).", "AppTpv",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        lerroa.Kopurua = kopurua;
        EskaeraEguneratu();
    }

    private void BtnEzabatuLerroa_Click(object sender, RoutedEventArgs e)
    {
        if (ListaEskaera.SelectedItem is not EskaeraLerroa { Bidalita: false } lerroa) return;

        _lerroak.Remove(lerroa);
        EskaeraEguneratu();
    }

    // ---------- Komandara bidali ----------

    private void BtnBidali_Click(object sender, RoutedEventArgs e)
    {
        var zainek = _lerroak.Where(l => !l.Bidalita).ToList();
        if (zainek.Count == 0)
        {
            MessageBox.Show("Ez dago bidaltzeko produktu berririk.", "AppTpv",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (_eskaeraId == 0)
        {
            if (!EskaeraIrekiaBermatu()) return;
            BidalitakoLerroakKargatu();     // komandan lehendik zegoena berreskuratu
        }

        BtnBidali.IsEnabled = false;
        BtnBidali.Content = "Bidaltzen...";

        try
        {
            foreach (var lerroa in zainek)
            {
                var r = Konexioa.Bidali("PEDIDO_AGREGAR_LINEA", new
                {
                    eskaeraId = _eskaeraId,
                    produktuId = lerroa.ProduktuId,
                    kopurua = lerroa.Kopurua
                });

                if (!r.Arrakasta)
                {
                    MessageBox.Show(r.Errorea, "Ezin izan da bidali", MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    EskaeraEguneratu();
                    MenuaKargatu();
                    return;
                }

                // zerbitzaria da prezioaren iturri egiazkoa
                if (r.Datuak.TryGetProperty("prezioUnitarioa", out var prezioa) &&
                    prezioa.ValueKind == JsonValueKind.Number)
                {
                    lerroa.PrezioUnitarioa = prezioa.GetDouble();
                }

                lerroa.Egoera = "bidalita";
            }

            EskaeraEguneratu();
            MenuaKargatu();
            TxtMenuInfo.Text = "Eskaera bidali da.";
        }
        finally
        {
            BtnBidali.Content = "Bidali";
            BtnBidali.IsEnabled = true;
        }
    }

    /// <summary>
    /// Eskaera berria irekitzen du edo erabiltzaileak irekita duena hartzen du
    /// (zerbitzariak "Zure N zenbakiko eskaera irekita dago" erantzuten du).
    /// </summary>
    private bool EskaeraIrekiaBermatu()
    {
        var r = Konexioa.Bidali("PEDIDO_ABRIR");

        if (r.Arrakasta)
        {
            _eskaeraId = r.Datuak.GetProperty("eskaeraId").GetInt32();
        }
        else
        {
            var m = Regex.Match(r.Errorea, @"Zure (\d+) zenbakiko eskaera irekita dago");
            if (!m.Success)
            {
                MessageBox.Show(r.Errorea, "AppTpv", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return false;
            }
            _eskaeraId = int.Parse(m.Groups[1].Value);
        }

        EskaeraEguneratu();
        return true;
    }

    /// <summary>
    /// Komanda irekian jada bidalitako lerroak zerbitzaritik ekartzen ditu
    /// (menu.js-eko pintarPedidoUlConEstado baliokidea).
    /// </summary>
    private void BidalitakoLerroakKargatu()
    {
        if (_eskaeraId == 0) return;

        var r = Konexioa.Bidali("TICKET_OBTENER", new { eskaeraId = _eskaeraId });
        if (!r.Arrakasta) return;

        var txartela = r.Datuak.GetProperty("txartela");
        if (!txartela.TryGetProperty("lerroak", out var zerrenda) ||
            zerrenda.ValueKind != JsonValueKind.Array) return;

        var bidalitakoak = JsonSerializer.Deserialize<List<EskaeraLerroDatuak>>(zerrenda.GetRawText()) ?? new();

        _lerroak.RemoveAll(l => l.Bidalita);
        foreach (var l in bidalitakoak)
        {
            // PEDIDO_REKUPERATUk produktuId ekartzen du; TICKET_OBTENERek izena soilik
            var produktuId = l.ProduktuId > 0
                ? l.ProduktuId
                : _menua.FirstOrDefault(p => p.Izena == l.Izena)?.Id ?? 0;
            _lerroak.Add(new EskaeraLerroa
            {
                ProduktuId = produktuId,
                Izena = l.Izena,
                Kopurua = l.Kopurua,
                PrezioUnitarioa = l.PrezioUnitarioa,
                Egoera = "bidalita"
            });
        }

        EskaeraEguneratu();
    }

    // ---------- Subtotal / cobrar / txartela ----------

    private void BtnAzpitotala_Click(object sender, RoutedEventArgs e)
    {
        if (_lerroak.Any(l => !l.Bidalita))
        {
            MessageBox.Show("Ezabatu edo bidali zain dauden produktuak.", "AppTpv",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (_lerroak.Count == 0)
        {
            MessageBox.Show("Ez dago produkturik eskaeran.", "AppTpv",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var xehetasuna = string.Join("\n", _lerroak.Select(l =>
            $" {l.Kopurua} x {l.Izena,-24} {l.Zenbatekoa:0.00} €"));
        var guztira = _lerroak.Sum(l => l.Zenbatekoa);

        MessageBox.Show($"AZPITOTALA\n\n{xehetasuna}\n\nGUZTIRA: {guztira:0.00} €\n(BEZ %10 barne)",
            "Azpitotala", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnKobratu_Click(object sender, RoutedEventArgs e)
    {
        if (_lerroak.Any(l => !l.Bidalita))
        {
            MessageBox.Show("Ezabatu edo bidali zain dauden produktuak.", "AppTpv",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (_eskaeraId == 0)
        {
            MessageBox.Show("Ez dago eskaera irekirik.", "AppTpv",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (_lerroak.Count == 0)
        {
            MessageBox.Show("Eskaera hutsik dago.", "AppTpv",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var r = Konexioa.Bidali("PEDIDO_CONFIRMAR", new { eskaeraId = _eskaeraId });
        if (!r.Arrakasta)
        {
            MessageBox.Show(r.Errorea, "Ezin izan da kobratu", MessageBoxButton.OK,
                MessageBoxImage.Warning);
            MenuaKargatu();
            return;
        }

        var guztira = r.Datuak.GetProperty("guztira").GetDouble();
        _azkenEskaeraId = _eskaeraId;
        _eskaeraId = 0;
        _mahaiId = 0;
        CboMahaiak.SelectedIndex = -1;
        _lerroak.Clear();
        EskaeraEguneratu();
        MenuaKargatu();

        MessageBox.Show(
            $"Erosketa baieztatu da. Guztira: {guztira:0.00} €\nBiltegiko izakinak eguneratu dira.",
            "AppTpv", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnTxartela_Click(object sender, RoutedEventArgs e)
    {
        var id = _eskaeraId != 0 ? _eskaeraId : _azkenEskaeraId;
        if (id == 0)
        {
            MessageBox.Show("Oraindik ez dago tiketa ikusteko eskaerarik.", "AppTpv",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var r = Konexioa.Bidali("TICKET_OBTENER", new { eskaeraId = id });
        if (!r.Arrakasta)
        {
            MessageBox.Show(r.Errorea, "AppTpv", MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var t = r.Datuak.GetProperty("txartela");
        var lerroak = JsonSerializer.Deserialize<List<EskaeraLerroDatuak>>(
            t.GetProperty("lerroak").GetRawText()) ?? new();
        var xehetasuna = string.Join("\n", lerroak.Select(l =>
            $" {l.Kopurua} x {l.Izena,-24} {l.Zenbatekoa:0.00} €"));

        var testua = $"{t.GetProperty("eskaeraId").GetInt32()} eskaeraren tiketa\n" +
                    $"Bezeroa: {t.GetProperty("bezeroa").GetString()}\n" +
                    $"Data:    {t.GetProperty("data").GetString()}\n" +
                    $"\n{xehetasuna}\n" +
                    $"\nGUZTIRA: {t.GetProperty("guztira").GetDouble():0.00} € (BEZ %10 barne)";
        MessageBox.Show(testua, "Tiketa", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    // ---------- Ikuspegia freskatu ----------

    private void EskaeraEguneratu()
    {
        BidalitakoLerroakTaldekatu();

        ListaEskaera.ItemsSource = null;
        ListaEskaera.ItemsSource = _lerroak;

        var guztira = _lerroak.Sum(l => l.Zenbatekoa);
        TxtTotal.Text = $"{guztira:0.00} €";

        if (_eskaeraId == 0)
        {
            TxtEskaeraEgoera.Text = "Ez dago eskaera irekirik";
        }
        else
        {
            var mahai = _mahaiak.FirstOrDefault(m => m.Id == _mahaiId);
            TxtEskaeraEgoera.Text = mahai != null
                ? $"Eskaera zk. {_eskaeraId} · {mahai.MahaiZenbakia}. mahaia"
                : $"Eskaera zk. {_eskaeraId}";
        }
    }

    /// <summary>
    /// menu.js-ek bidalitako lerroak produktuz elkartzen ditu. Hemen gauza bera
    /// egiten dugu, berriro irekitako komandak lerro bera bi aldiz ez erakusteko
    /// (zainak bereizita mantentzen dira, web komanderoan bezala).
    /// </summary>
    private void BidalitakoLerroakTaldekatu()
    {
        var bidalitakoak = _lerroak.Where(l => l.Bidalita).ToList();
        if (bidalitakoak.Count <= 1) return;

        foreach (var taldea in bidalitakoak.GroupBy(l =>
                     l.ProduktuId != 0 ? $"id:{l.ProduktuId}" : $"n:{l.Izena}"))
        {
            var lehenengoa = taldea.First();
            if (taldea.Count() == 1) continue;

            lehenengoa.Kopurua = taldea.Sum(l => l.Kopurua);
            foreach (var errepikatua in taldea.Skip(1)) _lerroak.Remove(errepikatua);
        }
    }
}
