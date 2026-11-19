// ============================================================
//  AppTpv — Bezeroaren konexioa (irakurketa/erantzun begizta)
//  Markoa: LINEA BAT = MEZU JSON BAT (UTF-8, \n bidez bereizita)
// ============================================================
using System.IO;
using System.Net.Sockets;
using System.Text.Json;
using ZerbitzariaTPV.Zerbitzuak;

namespace ZerbitzariaTPV.Sarea;

public class BezeroKonexioa
{
    // TCP konexioa gordetzen du, bezeroaren socketa adierazten duena.
    private readonly TcpClient _bezeroa;

    // Konexio honetan sortu diren saio-tokenak. Socket-a bat-batean ixten bada
    // (prozesua kraskatu, konexioa galdu...) tokenak zerbitzarian pilatu ez daitezen
    // erabiltzen dira: Artatu()-ren finally blokean guztiak ezabatzen dira.
    private readonly HashSet<string> _saioTokenak = new();

    // Konexio objektua sortzean socketa jasotzen da.
    public BezeroKonexioa(TcpClient bezeroa) { _bezeroa = bezeroa; }

    public void Artatu()
    {
        try
        {
        // Socketa eta streamak automatikoki itxi daitezke erabilitako 'using' bidez.
        using (_bezeroa)
        using (var fluxua = _bezeroa.GetStream())
        // Lerroak irakurtzeko StreamReader erabiltzen da; UTF-8 kodeketa erabiltzen da.
        using (var irakurlea = new StreamReader(fluxua, System.Text.Encoding.UTF8))
        // Erantzunak idazteko StreamWriter erabiltzen da, eta AutoFlush true da mezua berehala bidaltzeko.
        using (var idazlea = new StreamWriter(fluxua, new System.Text.UTF8Encoding(false)) { AutoFlush = true })
        {
            // StreamWriter-ek linebreak-aren formatu zehatza ezartzen du.
            idazlea.NewLine = "\n";
            string? lerroa;
            // Bezeroak bidaltzen dituen mezurik irakurritzen da, lineaz lerroa.
            while ((lerroa = irakurlea.ReadLine()) != null)
            {
                // Lerro hutsik baldin bada, ez da prozesatzen eta hurrengo iteraziora pasatzen da.
                if (lerroa.Trim() == "") continue;

                // Lerroa prozesatu eta erantzun JSON bat sortu.
                var erantzuna = Prozesatu(lerroa);
                // Erantzuna socketera idazten da.
                idazlea.Write(erantzuna);
                // Mezu bakoitza lerro berriarekin amaitzen da.
                idazlea.Write("\n");
            }
        }
        }
        finally
        {
            // Socket-a ixten denean saioak garbitu, LOGOUT esplizitua ez badago ere.
            // Horrela erabiltzaile askok saioa ireki/itxi errepikatzean ez dira
            // token zaharrak zerbitzarian geratzen.
            foreach (var token in _saioTokenak) Saioak.Itxi(token);
            if (_saioTokenak.Count > 0)
                Console.WriteLine($"[SESION] Konexioa itxita: {_saioTokenak.Count} token garbituta");
            _saioTokenak.Clear();
        }
    }

    private string Prozesatu(string lerroa)
    {
        try
        {
            // JSON mezua analitikoa da, root elementua eskuratzeko.
            using var dokumentua = JsonDocument.Parse(lerroa);
            var erroa = dokumentua.RootElement;

            // 'token' propietatearen balioa lortzen da, eta String motakoa izan behar da.
            var token = erroa.TryGetProperty("token", out var t) &&
                        t.ValueKind == JsonValueKind.String ? t.GetString() : null;
            // 'ekintza' propietatearen balioa lortzen da, eta String motakoa izan behar da.
            var ekintza = erroa.TryGetProperty("ekintza", out var a) &&
                         a.ValueKind == JsonValueKind.String ? a.GetString() ?? "" : "";
            // 'datuak' propietatea objektu bat bada, kopia bat gordetzen da; bestela null/defektuzkoa.
            var datuak = erroa.TryGetProperty("datuak", out var d) &&
                        d.ValueKind == JsonValueKind.Object ? d.Clone() : default;

            // LOGOUT akzioa bereziki tratatu behar da: saioa itxi eta erantzun zuzena itzuli.
            if (ekintza == "LOGOUT")
            {
                Saioak.Itxi(token);
                if (token != null) _saioTokenak.Remove(token);
                return Serializatu(Emaitza.Ondo(new { mezua = "Saioa itxi da" }));
            }

            // Beste egoeretan, tokena validatu eta operazioa exekutatu.
            var saioa = Saioak.Lortu(token);
            if (saioa != null && token != null)
                _saioTokenak.Add(token);

            var emaitza = EragiketaKudeatzailea.Exekutatu(saioa, ekintza, datuak);

            // LOGINek sortutako tokena ere gorde: bezeroa berehala itxiz gero
            // Artatu()-ren finally blokeak ezabatu dezan.
            if (ekintza == "LOGIN" && emaitza.Arrakasta && emaitza.Datuak != null)
            {
                var json = JsonSerializer.Serialize(emaitza.Datuak);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("token", out var tv) &&
                    tv.ValueKind == JsonValueKind.String)
                {
                    var sortutakoa = tv.GetString();
                    if (sortutakoa != null) _saioTokenak.Add(sortutakoa);
                }
            }

            return Serializatu(emaitza);
        }
        catch (JsonException)
        {
            // JSON formatua baliogabea bada, errore-mezu bat itzultzen da.
            return Serializatu(Emaitza.Txar("JSON formatua ez da baliozkoa"));
        }
        catch (IOException)
        {
            // Saretik irakurtze/IDazteko errorea gertatzen bada, Artatu() metodoak kudeatuko du.
            throw; // se encarga el using/Artatu
        }
        catch (Exception ex)
        {
            // Unbeknown exceptiona harrapatzen da eta logean idazten da.
            Console.WriteLine($"[BEZEROA] Errorea: {ex.Message}");
            // Bezeroari errore orokor bat itzultzen zaio, baina konexioa ez da itxi behar hemen.
            return Serializatu(Emaitza.Txar("Errorea gertatu da mezua prozesatzean"));
        }
    }

    private static string Serializatu(Emaitza r)
    {
        // JSON serializaziorako aukerak definitzen dira, propietate izenak aldatu gabe.
        var aukerak = new JsonSerializerOptions { PropertyNamingPolicy = null };
        // Erantzuna arrakastatsua ala errorea egokitzeko eraikitzen da.
        var objektua = r.Arrakasta
            ? (object)new { arrakasta = true, datuak = r.Datuak }
            : new { arrakasta = false, errorea = r.Errorea };
        // Objektua JSON kate bihurtzen da eta itzultzen da.
        return JsonSerializer.Serialize(objektua, aukerak);
    }
}
