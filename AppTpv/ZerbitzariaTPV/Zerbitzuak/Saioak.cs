// ============================================================
//  AppTpv — Saio aktiboak (3. eskakizuna: bezero aldi berean)
//  Bezero bakoitzeko tokena LOGIN ondoren; rola tokenari lotuta doa.
// ============================================================
using System.Collections.Concurrent;

namespace ZerbitzariaTPV.Zerbitzuak;

/**
 * Erabiltzailearen informazio publikoa adierazten du, saioan gordeko dena.
 * Bezero autentifikatuaren identifikazioan eta rolaren arabera baimenak egiaztatzeko erabiltzen da.
 */
public record ErabiltzaileSaioa(
    int ErabiltzaileId,
    string ErabiltzaileIzena,
    string Izena,
    string Rola);

/**
 * Zerbitzariaren saio aktiboak memoria barruan mantentzen ditu.
 * Erabiltzaile autentifikatu bakoitzari token bakarra ematen zaio, eta hura erabiltzen da
 * bere datuak berreskuratzeko kredentzialak eskaera bakoitzean bidali gabe.
 */
public static class Saioak
{
    // Tokenen arabera saio aktiboak gordetzen ditu. ConcurrentDictionary-ek
    // konexio anitzek aldi berean sartzen direnean sortzen diren arazoak ekiditen ditu.
    private static readonly ConcurrentDictionary<string, ErabiltzaileSaioa> Aktiboak = new();

    /**
     * Saio berria sortzen du autentifikatutako erabiltzaile baterako.
     * @param s Saioari lotuko zaizkion erabiltzailearen datuak.
     * @returns Saioaren bizitza aktiboan identifikatzeko erabiliko den token bakarra.
     */
    public static string Sortu(ErabiltzaileSaioa s)
    {
        //128 biteko identifikatzaile bakar global bat sortzen du
        var token = Guid.NewGuid().ToString("N");
        Aktiboak[token] = s;
        return token;
    }

    /**
     * Token bati lotutako erabiltzailearen informazioa berreskuratzen du.
     * @param token ErabiltzaileIzena egitean sortutako saio-identifikatzailea.
     * @returns Saio-tokena baliozkoa bada, saioaren datuak; bestela null.
     */
    public static ErabiltzaileSaioa? Lortu(string? token)
        => token != null && Aktiboak.TryGetValue(token, out var s) ? s : null;

    /**
     * Erabiltzailearen saioa itxi eta bere tokena saio aktiboen diccionarioan ezabatzen du.
     * @param token Ezeztatu behar den saio-tokena.
     */
    public static void Itxi(string? token)
    {
        if (token != null) Aktiboak.TryRemove(token, out _);
    }

    /**
     * Une honetan memoria barruan dauden saio aktiboen kopurua.
     */
    public static int Guztira => Aktiboak.Count;
}
