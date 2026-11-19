// ============================================================
//  AppTpv — ZerbitzariaTPV zerbitzariarekiko TCP konexioa
//
//  Aplikazio-leiho bakoitzeko konexio iraunkor bakarra erabiltzen da
//  (3. eskakizuna: zerbitzariak 10 bezerori gehienez onartzen die).
//  docs/PROTOCOLO.md protokoloaren arabera, lerro bakoitza JSON mezu bat da:
//  { ekintza, token, datuak }; erantzuna, berriz, { arrakasta, datuak?, errorea? }.
//
//  Blokeoaren bidez bidalketak sekuentziatzen dira; deiak sinkronoak dira
//  (zerbitzariak milisegundo gutxitan erantzuten du sare lokalean).
// ============================================================
// Fitxategiak eta streamak erabiltzeko izen-eremua inportatzen da.
using System.IO;
// TCP bezeroa erabiltzeko izen-eremua inportatzen da.
using System.Net.Sockets;
// Testua kodetu eta deskodetzeko izen-eremua inportatzen da.
using System.Text;
// JSON mezuak serializatu eta aztertzeko izen-eremua inportatzen da.
using System.Text.Json;

// Bezeroaren zerbitzu-klaseen izen-eremua zehazten da.
namespace BezeroaWPF.Zerbitzuak;

// Zerbitzariak bidalitako erantzuna gordetzen duen mota definitzen da.
public class Erantzuna
{
    // Eragiketa arrakastatsua izan den adierazten du.
    public bool Arrakasta { get; init; }
    // Erantzunaren datuak JSON elementu gisa gordetzen ditu.
    public JsonElement Datuak { get; init; }
    // Eragiketak huts egiten badu, errorearen azalpena gordetzen du.
    public string Errorea { get; init; } = "";
}

// TCP konexioa eta eskaeren bidalketa kudeatzeko klase estatikoa definitzen da.
public static class Konexioa
{
    // Windows makina birtualean ostalariaren (Mac-aren) IP helbidea erabili behar da,
    // ez 127.0.0.1. Helbidea zerbitzari.txt fitxategian ezar daiteke,
    // exekutagarriaren karpetan, lehen lerroan: IP [:portua].
    // Ostalari-izen lehenetsia localhost da.
    public static string Ostalaria = "127.0.0.1";
    // Zerbitzariaren TCP ataka lehenetsia gordetzen da.
    public static int Ataka = 5001;

    // TCP bezeroaren erreferentzia gordetzen da; oraindik sortu gabe egon daiteke.
    private static TcpClient? _bezeroa;
    // Jasotako lerroak irakurtzeko stream-irakurlea gordetzen da.
    private static StreamReader? _irakurlea;
    // Zerbitzariari lerroak bidaltzeko stream-idazlea gordetzen da.
    private static StreamWriter? _idazlea;
    // Aldibereko eskaerak sekuentziatzeko blokeo-objektua gordetzen da.
    private static readonly object _blokeoa = new();

    // Klasea lehen aldiz erabiltzean konexio-ezarpenak kargatzen dira.
    static Konexioa()
    {
        try
        {
            // Ezarpen-fitxategiaren bidea aplikazioaren exekutagarriaren karpetan sortzen da.
            var bidea = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "servidor.txt");
            // Fitxategia badago bakarrik irakurtzen da.
            if (File.Exists(bidea))
            {
                // Fitxategiko lehen lerroa irakurri eta hasierako zein amaierako zuriuneak kentzen dira.
                var lehenengoa = File.ReadAllLines(bidea).FirstOrDefault()?.Trim();
                // Lehen lerroa hutsik ez badago, helbidea eta ataka aztertzen dira.
                if (!string.IsNullOrEmpty(lehenengoa))
                {
                    // Lerroa bi puntuen arabera zatitzen da, IP helbidea eta ataka bereizteko.
                    var zatiak = lehenengoa.Split(':');
                    // Lehen zatia dagoen eta hutsik ez dagoen egiaztatu ondoren, ostalaria ezartzen da.
                    if (zatiak.Length >= 1 && zatiak[0] != "") Ostalaria = zatiak[0];
                    // Bigarren zatia zenbaki oso balioduna bada, ataka eguneratzen da.
                    if (zatiak.Length >= 2 && int.TryParse(zatiak[1], out var p)) Ataka = p;
                }
            }
        }
        // Ezarpen-fitxategia aukerakoa denez, irakurtzeak huts egiten badu lehenetsitako balioak mantentzen dira.
        catch { /* Aukerako konfigurazioa da; lehenetsitako helbidea eta ataka mantentzen dira. */ }
    }

    /// <summary>Ekintza bat bidali eta zerbitzariaren JSON lerro-erantzunaren zain geratzen da.</summary>
    /// <param name="ekintza">Zerbitzarian exekutatu beharreko ekintzaren izena.</param>
    /// <param name="datuak">Ekintzari lotutako datuak; ematen ez badira, objektu hutsa bidaltzen da.</param>
    /// <returns>Zerbitzariaren erantzuna, arrakasta-egoerarekin, datuekin edo errore-mezuarekin.</returns>
    public static Erantzuna Bidali(string ekintza, object? datuak = null)
    {
        // Aldi berean eskaera bakarra bidali eta erantzuna irakurtzen dela bermatzen da.
        lock (_blokeoa)
        {
            try
            {
                // TCP konexioa irekita dagoela ziurtatzen da, beharrezkoa bada berria sortuz.
                KonexioaBermatu();

                // Ekintza, saio-tokena eta datuak dituen objektua JSON testu bihurtzen da.
                var mezua = JsonSerializer.Serialize(new
                {
                    // Zerbitzarian exekutatu beharreko ekintzaren izena gehitzen da.
                    ekintza,
                    // Uneko saioaren tokena gehitzen da, zerbitzariak erabiltzailea identifika dezan.
                    token = Saioa.Token,
                    // Eskatutako datuak gehitzen dira, edo objektu hutsa daturik ez badago.
                    datuak = datuak ?? new { }
                });
                // JSON eskaera zerbitzariari lerro gisa bidaltzen zaio.
                _idazlea!.WriteLine(mezua);
                // Idazketa-bufferrean geratutako datuak berehala bidaltzen dira.
                _idazlea.Flush();

                // Zerbitzariaren erantzun-lerroa irakurtzen da.
                var lerroa = _irakurlea!.ReadLine();
                // Erantzun-lerroa null bada, zerbitzariak konexioa itxi duela adierazten da.
                if (lerroa == null)
                    return new Erantzuna { Arrakasta = false, Errorea = "Zerbitzariak konexioa itxi du." };

                // Jasotako testua JSON dokumentu gisa aztertzen da.
                using var dokumentua = JsonDocument.Parse(lerroa);
                // JSON dokumentuaren erro-elementua eskuratzen da.
                var erroa = dokumentua.RootElement;
                // Eragiketaren arrakasta-eremua irakurri eta booleano bihurtzen da.
                bool arrakasta = erroa.GetProperty("arrakasta").GetBoolean();
                // Errore-eremua badago eta testua bada, haren balioa hartzen da; bestela, kate hutsa.
                string errorea = erroa.TryGetProperty("errorea", out var e) &&
                               e.ValueKind == JsonValueKind.String
                    ? e.GetString() ?? "" : "";
                // Gehienezko bezero-kopurua gainditzean, kode teknikoa erabiltzailearentzako mezu bihurtzen da.
                if (errorea == "SESSION_LIMIT")
                    errorea = "Zerbitzaria beteta dago (10/10). Saiatu berriro geroago.";
                // Datu-eremua badago, kopia independentea gordetzen da dokumentua itxi aurretik.
                JsonElement datuakR = erroa.TryGetProperty("datuak", out var d)
                    ? d.Clone() : default;
                // Erantzun-objektua sortu eta deitzaileari itzultzen zaio.
                return new Erantzuna { Arrakasta = arrakasta, Datuak = datuakR, Errorea = errorea };
            }
            // TCP konexioa ezartzean edo erabiltzean socket-errorea gertatzen bada kudeatzen da.
            catch (SocketException)
            {
                // Konexio baliogabea ixteko eta haren baliabideak askatzeko saiakera egiten da.
                Itxi();
                // Helbidea, ataka eta jatorrizko errorea biltzen dituen erantzuna itzultzen da.
                return new Erantzuna
                {
                    Arrakasta = false,
                    Errorea = "Ezin izan da zerbitzariarekin konektatu. Egiaztatu konexioa eta saiatu berriro."
                };
            }
            // Sarrerako edo irteerako streamarekin lotutako erroreak kudeatzen dira.
            catch (IOException)
            {
                // Erabil ezin daitekeen konexioa ixten da.
                Itxi();
                // Zerbitzariarekiko konexioa galdu dela adierazten da.
                return new Erantzuna { Arrakasta = false, Errorea = "Zerbitzariarekiko konexioa galdu da." };
            }
            // Zerbitzariak JSON formatu baliogabea bidaltzen badu kudeatzen da.
            catch (JsonException)
            {
                // Erantzuna ezin izan dela interpretatu adierazten da.
                return new Erantzuna { Arrakasta = false, Errorea = "Zerbitzariaren erantzuna ez da baliozkoa." };
            }
        }
    }

    // Bezeroaren eta zerbitzariaren arteko konexioa erabilgarri dagoela bermatzen du.
    private static void KonexioaBermatu()
    {
        // Bezeroa dagoeneko konektatuta badago, ez da beste konexiorik sortzen.
        if (_bezeroa is { Connected: true }) return;
        // Aurreko konexioa itxi eta hari lotutako streamak garbitzen dira.
        Itxi();
        // TCP bezero berria sortzen da zerbitzariarekin konektatzeko.
        _bezeroa = new TcpClient();
        // Konexioa modu asinkronoan abiarazten da.
        var zeregina = _bezeroa.ConnectAsync(Ostalaria, Ataka);
        // Bost segundo igaro eta konektatu ez bada, denbora-muga errorea sortzen da.
        if (!zeregina.Wait(5000))
            throw new SocketException((int)SocketError.TimedOut);

        // TCP bezeroaren sare-streama eskuratzen da.
        var fluxua = _bezeroa.GetStream();
        // UTF-8 kodetutako testua bidaltzeko idazlea sortzen da; BOM marka ez da gehitzen.
        _idazlea = new StreamWriter(fluxua, new UTF8Encoding(false)) { AutoFlush = true };
        // UTF-8 kodetutako erantzunak irakurtzeko irakurlea sortzen da.
        _irakurlea = new StreamReader(fluxua, Encoding.UTF8);
    }

    // TCP konexioa eta haren irakurle/idazleak ixten ditu.
    public static void Itxi()
    {
        // Bezeroa badago, ixten saiatzen da; dagoeneko itxita badago, ez du ezer egiten.
        try { _bezeroa?.Close(); } catch { /* Konexioa itxita dago jada. */ }
        // TCP bezeroaren erreferentzia ezabatzen da.
        _bezeroa = null;
        // Stream-irakurlearen erreferentzia ezabatzen da.
        _irakurlea = null;
        // Stream-idazlearen erreferentzia ezabatzen da.
        _idazlea = null;
    }
}
