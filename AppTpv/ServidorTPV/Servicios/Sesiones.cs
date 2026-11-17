// ============================================================
//  AppTpv — Sesiones activas (requisito 3: clientes simultaneos)
//  Token por cliente tras LOGIN; el rol viaja pegado al token.
// ============================================================
using System.Collections.Concurrent;

namespace ServidorTPV.Servicios;

/**
 * Erabiltzailearen informazio publikoa adierazten du, saioan gordeko dena.
 * Bezero autentifikatuaren identifikazioan eta rolaren arabera baimenak egiaztatzeko erabiltzen da.
 */
public record SesionUsuario(
    int IdUsuario,
    string Login,
    string Nombre,
    string Rol);

/**
 * Zerbitzariaren saio aktiboak memoria barruan mantentzen ditu.
 * Erabiltzaile autentifikatu bakoitzari token bakarra ematen zaio, eta hura erabiltzen da
 * bere datuak berreskuratzeko kredentzialak eskaera bakoitzean bidali gabe.
 */
public static class Sesiones
{
    // Tokenen arabera saio aktiboak gordetzen ditu. ConcurrentDictionary-ek
    // konexio anitzek aldi berean sartzen direnean sortzen diren arazoak ekiditen ditu.
    private static readonly ConcurrentDictionary<string, SesionUsuario> Activas = new();

    /**
     * Saio berria sortzen du autentifikatutako erabiltzaile baterako.
     * @param s Saioari lotuko zaizkion erabiltzailearen datuak.
     * @returns Saioaren bizitza aktiboan identifikatzeko erabiliko den token bakarra.
     */
    public static string Crear(SesionUsuario s)
    {
        //128 biteko identifikatzaile bakar global bat sortzen du
        var token = Guid.NewGuid().ToString("N");
        Activas[token] = s;
        return token;
    }

    /**
     * Token bati lotutako erabiltzailearen informazioa berreskuratzen du.
     * @param token Login egitean sortutako saio-identifikatzailea.
     * @returns Saio-tokena baliozkoa bada, saioaren datuak; bestela null.
     */
    public static SesionUsuario? Obtener(string? token)
        => token != null && Activas.TryGetValue(token, out var s) ? s : null;

    /**
     * Erabiltzailearen saioa itxi eta bere tokena saio aktiboen diccionarioan ezabatzen du.
     * @param token Ezeztatu behar den saio-tokena.
     */
    public static void Cerrar(string? token)
    {
        if (token != null) Activas.TryRemove(token, out _);
    }

    /**
     * Une honetan memoria barruan dauden saio aktiboen kopurua.
     */
    public static int Total => Activas.Count;
}
