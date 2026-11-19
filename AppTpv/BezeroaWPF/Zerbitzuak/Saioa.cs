// ============================================================
//  AppTpv — Erabiltzailearen saio lokala (bezero-aldea)
//  Tokena zerbitzariak ematen du LOGIN ondoren; hemen bakarrik
//  gordetzen dugu, mezu bakoitzean sartzeko.
// ============================================================
namespace BezeroaWPF.Zerbitzuak;

public static class Saioa
{
    public static string Token = "";
    public static string ErabiltzaileIzena = "";
    public static string Izena = "";
    public static string Rola = "";

    public static bool AdminDa => Rola == "admin";

    public static void Itxi()
    {
        Token = ""; ErabiltzaileIzena = ""; Izena = ""; Rola = "";
    }
}
