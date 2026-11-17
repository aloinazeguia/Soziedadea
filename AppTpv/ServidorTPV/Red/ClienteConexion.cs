// ============================================================
//  AppTpv — Bezeroaren konexioa (irakurketa/erantzun begizta)
//  Markoa: LINEA BAT = MEZU JSON BAT (UTF-8, \n bidez bereizita)
// ============================================================
using System.IO;
using System.Net.Sockets;
using System.Text.Json;
using ServidorTPV.Servicios;

namespace ServidorTPV.Red;

public class ClienteConexion
{
    // TCP konexioa gordetzen du, bezeroaren socketa adierazten duena.
    private readonly TcpClient _cliente;

    // Konexio objektua sortzean socketa jasotzen da.
    public ClienteConexion(TcpClient cliente) { _cliente = cliente; }

    public void Atender()
    {
        // Socketa eta streamak automatikoki itxi daitezke erabilitako 'using' bidez.
        using (_cliente)
        using (var flujo = _cliente.GetStream())
        // Lerroak irakurtzeko StreamReader erabiltzen da; UTF-8 kodeketa erabiltzen da.
        using (var lector = new StreamReader(flujo, System.Text.Encoding.UTF8))
        // Erantzunak idazteko StreamWriter erabiltzen da, eta AutoFlush true da mezua berehala bidaltzeko.
        using (var escritor = new StreamWriter(flujo, new System.Text.UTF8Encoding(false)) { AutoFlush = true })
        {
            // StreamWriter-ek linebreak-aren formatu zehatza ezartzen du.
            escritor.NewLine = "\n";
            string? linea;
            // Bezeroak bidaltzen dituen mezurik irakurritzen da, lineaz linea.
            while ((linea = lector.ReadLine()) != null)
            {
                // Lerro hutsik baldin bada, ez da prozesatzen eta hurrengo iteraziora pasatzen da.
                if (linea.Trim() == "") continue;

                // Lerroa prozesatu eta erantzun JSON bat sortu.
                var respuesta = Procesar(linea);
                // Erantzuna socketera idazten da.
                escritor.Write(respuesta);
                // Mezu bakoitza lerro berriarekin amaitzen da.
                escritor.Write("\n");
            }
        }
    }

    private string Procesar(string linea)
    {
        try
        {
            // JSON mezua analitikoa da, root elementua eskuratzeko.
            using var doc = JsonDocument.Parse(linea);
            var raiz = doc.RootElement;

            // 'token' propietatearen balioa lortzen da, eta String motakoa izan behar da.
            var token = raiz.TryGetProperty("token", out var t) &&
                        t.ValueKind == JsonValueKind.String ? t.GetString() : null;
            // 'accion' propietatearen balioa lortzen da, eta String motakoa izan behar da.
            var accion = raiz.TryGetProperty("accion", out var a) &&
                         a.ValueKind == JsonValueKind.String ? a.GetString() ?? "" : "";
            // 'datos' propietatea objektu bat bada, kopia bat gordetzen da; bestela null/defektuzkoa.
            var datos = raiz.TryGetProperty("datos", out var d) &&
                        d.ValueKind == JsonValueKind.Object ? d.Clone() : default;

            // LOGOUT akzioa bereziki tratatu behar da: saioa itxi eta erantzun zuzena itzuli.
            if (accion == "LOGOUT")
            {
                Sesiones.Cerrar(token);
                return Json(Resultado.Bien(new { mensaje = "Saioa itxi da" }));
            }

            // Beste egoeretan, tokena validatu eta operazioa exekutatu.
            var sesion = Sesiones.Obtener(token);
            var resultado = GestorOperaciones.Ejecutar(sesion, accion, datos);
            return Json(resultado);
        }
        catch (JsonException)
        {
            // JSON formatua baliogabea bada, errore-mezu bat itzultzen da.
            return Json(Resultado.Mal("JSON formatua ez da baliozkoa"));
        }
        catch (IOException)
        {
            // Saretik irakurtze/IDazteko errorea gertatzen bada, Atender() metodoak kudeatuko du.
            throw; // se encarga el using/Atender
        }
        catch (Exception ex)
        {
            // Unbeknown exceptiona harrapatzen da eta logean idazten da.
            Console.WriteLine($"[CLIENTE] Error: {ex.Message}");
            // Bezeroari errore orokor bat itzultzen zaio, baina konexioa ez da itxi behar hemen.
            return Json(Resultado.Mal("Errorea gertatu da mezua prozesatzean"));
        }
    }

    private static string Json(Resultado r)
    {
        // JSON serializaziorako aukerak definitzen dira, propietate izenak aldatu gabe.
        var opciones = new JsonSerializerOptions { PropertyNamingPolicy = null };
        // Erantzuna arrakastatsua ala errorea egokitzeko eraikitzen da.
        var objeto = r.Ok
            ? (object)new { ok = true, datos = r.Datos }
            : new { ok = false, error = r.Error };
        // Objektua JSON kate bihurtzen da eta itzultzen da.
        return JsonSerializer.Serialize(objeto, opciones);
    }
}
