// ============================================================
//  AppTpv — utilidades de seguridad (erabiltzailea, requisito 2)
//  SHA-256 zerbitzariko sal konstantearekin: nahikoa da erronkaren
//  esparrurako, eta defentsan zintzo azal daiteke.
// ============================================================
using System.Security.Cryptography;
using System.Text;

namespace ZerbitzariaTPV.Datuak;

public static class Segurtasuna
{
    private const string Gatza = "AppTpv::ErronkaWIDZPP::2026";

    public static string PasahitzaHash(string pasahitza)
    {
        var byteak = SHA256.HashData(Encoding.UTF8.GetBytes(Gatza + pasahitza));
        var eraikitzailea = new StringBuilder(byteak.Length * 2);
        foreach (var b in byteak)
            eraikitzailea.Append(b.ToString("x2"));
        return eraikitzailea.ToString();
    }
}
