// ============================================================
//  AppTpv — ServidorTPV zerbitzariarekiko TCP konexioa
//
//  Aplikazio-leiho bakoitzeko konexio iraunkor bakarra erabiltzen da
//  (3. eskakizuna: zerbitzariak 10 bezerori gehienez onartzen die).
//  docs/PROTOCOLO.md protokoloaren arabera, lerro bakoitza JSON mezu bat da:
//  { accion, token, datos }; erantzuna, berriz, { ok, datos?, error? }.
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
namespace ClienteWPF.Servicios;

// Zerbitzariak bidalitako erantzuna gordetzen duen mota definitzen da.
public class Respuesta
{
    // Eragiketa arrakastatsua izan den adierazten du.
    public bool Ok { get; init; }
    // Erantzunaren datuak JSON elementu gisa gordetzen ditu.
    public JsonElement Datos { get; init; }
    // Eragiketak huts egiten badu, errorearen azalpena gordetzen du.
    public string Error { get; init; } = "";
}

// TCP konexioa eta eskaeren bidalketa kudeatzeko klase estatikoa definitzen da.
public static class Conexion
{
    // Windows makina birtualean ostalariaren (Mac-aren) IP helbidea erabili behar da,
    // ez 127.0.0.1. Helbidea zerbitzari.txt fitxategian ezar daiteke,
    // exekutagarriaren karpetan, lehen lerroan: IP [:portua].
    // Ostalari-izen lehenetsia localhost da.
    public static string Host = "127.0.0.1";
    // Zerbitzariaren TCP ataka lehenetsia gordetzen da.
    public static int Puerto = 5001;

    // TCP bezeroaren erreferentzia gordetzen da; oraindik sortu gabe egon daiteke.
    private static TcpClient? _cliente;
    // Jasotako lerroak irakurtzeko stream-irakurlea gordetzen da.
    private static StreamReader? _lector;
    // Zerbitzariari lerroak bidaltzeko stream-idazlea gordetzen da.
    private static StreamWriter? _escritor;
    // Aldibereko eskaerak sekuentziatzeko blokeo-objektua gordetzen da.
    private static readonly object _candado = new();

    // Klasea lehen aldiz erabiltzean konexio-ezarpenak kargatzen dira.
    static Conexion()
    {
        try
        {
            // Ezarpen-fitxategiaren bidea aplikazioaren exekutagarriaren karpetan sortzen da.
            var ruta = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "servidor.txt");
            // Fitxategia badago bakarrik irakurtzen da.
            if (File.Exists(ruta))
            {
                // Fitxategiko lehen lerroa irakurri eta hasierako zein amaierako zuriuneak kentzen dira.
                var primera = File.ReadAllLines(ruta).FirstOrDefault()?.Trim();
                // Lehen lerroa hutsik ez badago, helbidea eta ataka aztertzen dira.
                if (!string.IsNullOrEmpty(primera))
                {
                    // Lerroa bi puntuen arabera zatitzen da, IP helbidea eta ataka bereizteko.
                    var trozos = primera.Split(':');
                    // Lehen zatia dagoen eta hutsik ez dagoen egiaztatu ondoren, ostalaria ezartzen da.
                    if (trozos.Length >= 1 && trozos[0] != "") Host = trozos[0];
                    // Bigarren zatia zenbaki oso balioduna bada, ataka eguneratzen da.
                    if (trozos.Length >= 2 && int.TryParse(trozos[1], out var p)) Puerto = p;
                }
            }
        }
        // Ezarpen-fitxategia aukerakoa denez, irakurtzeak huts egiten badu lehenetsitako balioak mantentzen dira.
        catch { /* Aukerako konfigurazioa da; lehenetsitako helbidea eta ataka mantentzen dira. */ }
    }

    /// <summary>Ekintza bat bidali eta zerbitzariaren JSON lerro-erantzunaren zain geratzen da.</summary>
    /// <param name="accion">Zerbitzarian exekutatu beharreko ekintzaren izena.</param>
    /// <param name="datos">Ekintzari lotutako datuak; ematen ez badira, objektu hutsa bidaltzen da.</param>
    /// <returns>Zerbitzariaren erantzuna, arrakasta-egoerarekin, datuekin edo errore-mezuarekin.</returns>
    public static Respuesta Enviar(string accion, object? datos = null)
    {
        // Aldi berean eskaera bakarra bidali eta erantzuna irakurtzen dela bermatzen da.
        lock (_candado)
        {
            try
            {
                // TCP konexioa irekita dagoela ziurtatzen da, beharrezkoa bada berria sortuz.
                AsegurarConexion();

                // Ekintza, saio-tokena eta datuak dituen objektua JSON testu bihurtzen da.
                var mensaje = JsonSerializer.Serialize(new
                {
                    // Zerbitzarian exekutatu beharreko ekintzaren izena gehitzen da.
                    accion,
                    // Uneko saioaren tokena gehitzen da, zerbitzariak erabiltzailea identifika dezan.
                    token = Sesion.Token,
                    // Eskatutako datuak gehitzen dira, edo objektu hutsa daturik ez badago.
                    datos = datos ?? new { }
                });
                // JSON eskaera zerbitzariari lerro gisa bidaltzen zaio.
                _escritor!.WriteLine(mensaje);
                // Idazketa-bufferrean geratutako datuak berehala bidaltzen dira.
                _escritor.Flush();

                // Zerbitzariaren erantzun-lerroa irakurtzen da.
                var linea = _lector!.ReadLine();
                // Erantzun-lerroa null bada, zerbitzariak konexioa itxi duela adierazten da.
                if (linea == null)
                    return new Respuesta { Ok = false, Error = "Zerbitzariak konexioa itxi du." };

                // Jasotako testua JSON dokumentu gisa aztertzen da.
                using var doc = JsonDocument.Parse(linea);
                // JSON dokumentuaren erro-elementua eskuratzen da.
                var raiz = doc.RootElement;
                // Eragiketaren arrakasta-eremua irakurri eta booleano bihurtzen da.
                bool ok = raiz.GetProperty("ok").GetBoolean();
                // Errore-eremua badago eta testua bada, haren balioa hartzen da; bestela, kate hutsa.
                string error = raiz.TryGetProperty("error", out var e) &&
                               e.ValueKind == JsonValueKind.String
                    ? e.GetString() ?? "" : "";
                // Gehienezko bezero-kopurua gainditzean, kode teknikoa erabiltzailearentzako mezu bihurtzen da.
                if (error == "SESSION_LIMIT")
                    error = "Zerbitzaria beteta dago (10/10). Saiatu berriro geroago.";
                // Datu-eremua badago, kopia independentea gordetzen da dokumentua itxi aurretik.
                JsonElement datosR = raiz.TryGetProperty("datos", out var d)
                    ? d.Clone() : default;
                // Erantzun-objektua sortu eta deitzaileari itzultzen zaio.
                return new Respuesta { Ok = ok, Datos = datosR, Error = error };
            }
            // TCP konexioa ezartzean edo erabiltzean socket-errorea gertatzen bada kudeatzen da.
            catch (SocketException)
            {
                // Konexio baliogabea ixteko eta haren baliabideak askatzeko saiakera egiten da.
                Cerrar();
                // Helbidea, ataka eta jatorrizko errorea biltzen dituen erantzuna itzultzen da.
                return new Respuesta
                {
                    Ok = false,
                    Error = "Ezin izan da zerbitzariarekin konektatu. Egiaztatu konexioa eta saiatu berriro."
                };
            }
            // Sarrerako edo irteerako streamarekin lotutako erroreak kudeatzen dira.
            catch (IOException)
            {
                // Erabil ezin daitekeen konexioa ixten da.
                Cerrar();
                // Zerbitzariarekiko konexioa galdu dela adierazten da.
                return new Respuesta { Ok = false, Error = "Zerbitzariarekiko konexioa galdu da." };
            }
            // Zerbitzariak JSON formatu baliogabea bidaltzen badu kudeatzen da.
            catch (JsonException)
            {
                // Erantzuna ezin izan dela interpretatu adierazten da.
                return new Respuesta { Ok = false, Error = "Zerbitzariaren erantzuna ez da baliozkoa." };
            }
        }
    }

    // Bezeroaren eta zerbitzariaren arteko konexioa erabilgarri dagoela bermatzen du.
    private static void AsegurarConexion()
    {
        // Bezeroa dagoeneko konektatuta badago, ez da beste konexiorik sortzen.
        if (_cliente is { Connected: true }) return;
        // Aurreko konexioa itxi eta hari lotutako streamak garbitzen dira.
        Cerrar();
        // TCP bezero berria sortzen da zerbitzariarekin konektatzeko.
        _cliente = new TcpClient();
        // Konexioa modu asinkronoan abiarazten da.
        var tarea = _cliente.ConnectAsync(Host, Puerto);
        // Bost segundo igaro eta konektatu ez bada, denbora-muga errorea sortzen da.
        if (!tarea.Wait(5000))
            throw new SocketException((int)SocketError.TimedOut);

        // TCP bezeroaren sare-streama eskuratzen da.
        var flujo = _cliente.GetStream();
        // UTF-8 kodetutako testua bidaltzeko idazlea sortzen da; BOM marka ez da gehitzen.
        _escritor = new StreamWriter(flujo, new UTF8Encoding(false)) { AutoFlush = true };
        // UTF-8 kodetutako erantzunak irakurtzeko irakurlea sortzen da.
        _lector = new StreamReader(flujo, Encoding.UTF8);
    }

    // TCP konexioa eta haren irakurle/idazleak ixten ditu.
    public static void Cerrar()
    {
        // Bezeroa badago, ixten saiatzen da; dagoeneko itxita badago, ez du ezer egiten.
        try { _cliente?.Close(); } catch { /* Konexioa itxita dago jada. */ }
        // TCP bezeroaren erreferentzia ezabatzen da.
        _cliente = null;
        // Stream-irakurlearen erreferentzia ezabatzen da.
        _lector = null;
        // Stream-idazlearen erreferentzia ezabatzen da.
        _escritor = null;
    }
}
