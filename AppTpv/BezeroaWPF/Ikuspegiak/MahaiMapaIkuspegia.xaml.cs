// ============================================================
//  AppTpv — Mahaien mapa (6. fitxa): 7 mahaien egoera
//  Datuak zerbitzaritik ekartzen dira (MESAS_LISTA):
//    librea / erreserbatuta / okupatuta + eskaera irekiaren zenbakia.
// ============================================================
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using BezeroaWPF.Zerbitzuak;

namespace BezeroaWPF.Ikuspegiak;

public partial class MahaiMapaIkuspegia : UserControl
{
    public MahaiMapaIkuspegia()
    {
        InitializeComponent();
        Loaded += (_, _) => Kargatu();
    }

    private void BtnFreskatu_Click(object sender, RoutedEventArgs e) => Kargatu();

    private void Kargatu()
    {
        var r = Konexioa.Bidali("MESAS_LISTA");
        if (!r.Arrakasta)
        {
            TxtMapaInfo.Text = r.Errorea;
            return;
        }

        var mahaiak = JsonSerializer.Deserialize<List<MahaiDatuak>>(
            r.Datuak.GetProperty("mahaiak").GetRawText()) ?? new();
        ListaMahaiak.ItemsSource = mahaiak;

        var okupatuta = mahaiak.Count(m => m.Egoera == "okupatuta");
        var erreserbatuta = mahaiak.Count(m => m.Egoera == "erreserbatuta");
        TxtMapaInfo.Text = $"{mahaiak.Count} mahai · okupatuta: {okupatuta} · erreserbatuta: {erreserbatuta}";
    }
}
