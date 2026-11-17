// ============================================================
//  AppTpv — TCP zerbitzaria (enuntziatuaren 3. eskaria)
//  * 5001 portuan TcpListener erabiltzen da
//  * aldi berean 10 bezero maximo
//  * bezero bakoitzerako hari bat: zerbitzaria erantzuten bitartean,
//    bezeroa bere interfazea erabili ahal izango du
// ============================================================
using System.Net;
using System.Net.Sockets;

namespace ServidorTPV.Red;

public class ServidorTCP
{
    // TCP zerbitzariaren portua, non bezeroak konektatu ahal izango diren.
    public const int Puerto = 5001;
    // Konektatutako bezero kopuru maximoa, aldi berean.
    public const int MaxClientes = 10;

    // Eraikitzailea pribatua mantentzen da; singleton patroia erabili nahi delako.
    private ServidorTCP() { }

    // Zerbitzariaren instantzia bakarra, sistema osoan erabili ahal dena.
    public static readonly ServidorTCP Instancia = new();
    // Uneko konektatutako bezero kopurua. Harien arteko sarrerak babesteko int da.
    private int _clientes;

    public void Arrancar()
    {
        // TCP listenerra sortzen da interfaz guztietatik (IPAddress.Any) eta portuan entzuten hasten da.
        var server = new TcpListener(IPAddress.Any, Puerto);
        // Zerbitzaria martxan jartzen da; konexioak jasotzen hasiko da.
        server.Start();
        // Zerbitzaria entzuten ari dela logeatzen da; bezero maximo kopurua ere bistaratzen da.
        Console.WriteLine($"[TCP] Escuchando en el puerto {Puerto} (max {MaxClientes} clientes)");

        // Onartutako bezero bakoitzeko begizta: bezero bakoitzeko bere hari propioa sortu.
        while (true)
        {
            // Bezero berria onartzen da eta socketa lortzen da.
            var cliente = server.AcceptTcpClient();
            // Konektatutako bezero kopurua berriro egiaztatzeko eta zerbitzariaren mugak ezartzeko.
            Reset(ref _clientes, MaxClientes);
            // Bezero berria konektatu denez, kontagailua handitu egiten da atomikoki.
            var n = Interlocked.Increment(ref _clientes);
            // Kontagailua muga gainditzen badu, bezeroaren konexioa ukatu eta mezua itzultzen da.
            if (n > MaxClientes)
            {
                // Kontagailua handiagoa denez, konexioa errefusatua izan dela logeatzen da.
                Console.WriteLine("[TCP] Demasiados clientes, conexion rechazada");
                // Bezeroari bidali behar zaion streama lortzen da.
                var denegado = cliente.GetStream();
                // Erantzun formatuatu bat sortzen da JSON formatuan: eskaera ukatua.
                var bytes = System.Text.Encoding.UTF8.GetBytes
                    ("{\"ok\":false,\"error\":\"SESSION_LIMIT\",\"datos\":\"Servicio lleno (10/10). Reintentelo mas tarde\"}\n");
                // JSON mezua socketean idazten da.
                denegado.Write(bytes, 0, bytes.Length);
                // Datuak deskargatu eta bidali daitezen flush egiten da.
                denegado.Flush();
                // Konexioa itxi ondoren, erabili gabeko baliabideak askatzen dira.
                cliente.Close();
                // Konektatutako bezero kopurua berriro kendu egiten da, ukatu den konexioa kontuan hartuz.
                Interlocked.Decrement(ref _clientes);
                // Bezero hau baztertu denez, hurrengo iteraziora pasatzen da.
                continue;
            }

            // Bezeroa onartua izan dela logeatzen da, une horretako zenbakiarekin.
            Console.WriteLine($"[TCP] Cliente conectado ({n}/{MaxClientes})");
            // Bezero bakoitzerako hari berezi bat sortzen da, erantzun asinkronoan gauzatzeko.
            var hilo = new Thread(() =>
            {
                try
                {
                    // Bezeroari dagokion konexio objektua sortu eta bere atentzioa abiarazten da.
                    new ClienteConexion(cliente).Atender();
                }
                finally
                {
                    // Hariak amaitzean, kontagailua jaitsi egiten da; beste bezeroak sartzeko leku librea sortuz.
                    Interlocked.Decrement(ref _clientes);
                    // Bezeroa deskonektatu denez, berriro logeatzen da egoera.
                    Console.WriteLine($"[TCP] Cliente desconectado ({_clientes}/{MaxClientes})");
                }
            })
            { IsBackground = true };
            // Sortutako haria abiarazten da, zerbitzaria kargatu gabe aktibo geratzen delarik.
            hilo.Start();
        }
    }

    // Metodo honek etorkizuneko resetetarako errezerba gisa gordetzen da.
    private static void Reset(ref int _, int __) { }
}
