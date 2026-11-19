// ============================================================
//  AppTpv — Leiho laguntzailea: kopurua eskatu
// ============================================================
using System.Windows;

namespace BezeroaWPF.Leihoak;

public partial class KopuruLeihoa : Window
{
    public int Kopurua { get; private set; }
    private readonly int _izakinMaximoak;

    // izakinak >= 1 bada abisua erakusten da; zerbitzariak balioztatzen du behin betiko
    public KopuruLeihoa(string produktua, int izakinak)
    {
        InitializeComponent();
        TxtIzenburua.Text = produktua;
        TxtIzakinak.Text = izakinak <= 0
            ? "Ez dago unitaterik biltegian"
            : $"Eskuragarri dauden unitateak: {izakinak}";
        _izakinMaximoak = izakinak;
    }

    private void BtnOnartu_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(TxtKopurua.Text, out var n) || n <= 0)
        {
            MessageBox.Show("Sartu unitate-kopuru baliozko bat.", "AppTpv");
            return;
        }
        // oharra: abisu biguna da; izakin errealak zerbitzariak egiaztatzen ditu gehitzean
        Kopurua = n;
        DialogResult = true;
    }
}
