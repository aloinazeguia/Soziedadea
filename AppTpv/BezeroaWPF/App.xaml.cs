// ============================================================
//  AppTpv — Abiaraztea: lehenik LOGIN pantaila; aplikazioa ixten denean
//  zerbitzariarekiko TCP konexioa eten egiten da.
// ============================================================
using System.Windows;
using BezeroaWPF.Zerbitzuak;
using BezeroaWPF.Leihoak;

namespace BezeroaWPF;

public partial class Aplikazioa : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        new SaioHasieraLeihoa().Show();
    }

    private void Aplikazioa_Irten(object sender, ExitEventArgs e)
    {
        // zerbitzariarekiko konexioa itxi: 10 bezeroen lekua askatu
        if (Saioa.Token != "")
            Konexioa.Bidali("LOGOUT");
        Konexioa.Itxi();
    }
}
