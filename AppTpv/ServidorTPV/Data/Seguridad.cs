// ============================================================
//  AppTpv — utilidades de seguridad (login, requisito 2)
//  SHA-256 con sal fija del servidor: suficiente para el alcance
//  de la erronka y honesto de explicar en defensa.
// ============================================================
using System.Security.Cryptography;
using System.Text;

namespace ServidorTPV.Data;

public static class Seguridad
{
    private const string Sal = "AppTpv::ErronkaWIDZPP::2026";

    public static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(Sal + password));
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
            sb.Append(b.ToString("x2"));
        return sb.ToString();
    }
}
