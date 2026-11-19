// ============================================================
//  AppTpv — TCP zerbitzaria (enuntziatuaren 3. eskaria)
//  * 5001 portuan TcpListener erabiltzen da
//  * aldi berean 10 bezero maximo
//  * bezero bakoitzerako hari bat: zerbitzaria erantzuten bitartean,
//    bezeroa bere interfazea erabili ahal izango du
// ============================================================
using System.Net;
using System.Net.Sockets;

namespace ZerbitzariaTPV.Sarea;

public class TCPZerbitzaria
{
    // TCP zerbitzariaren portua, non bezeroak konektatu ahal izango diren.
    public const int Ataka = 5001;
    // Konektatutako bezero kopuru maximoa, aldi berean.
    public const int BezeroMaximoa = 10;

    // Eraikitzailea pribatua mantentzen da; singleton patroia erabili nahi delako.
    private TCPZerbitzaria() { }

    // Zerbitzariaren instantzia bakarra, sistema osoan erabili ahal dena.
    public static readonly TCPZerbitzaria Instantzia = new();
    // Uneko konektatutako bezero kopurua. Harien arteko sarrerak babesteko int da.
    private int _bezeroak;

    public void Abiarazi()
    {
        // TCP listenerra sortzen da interfaz guztietatik (IPAddress.Any) eta portuan entzuten hasten da.
        var zerbitzaria = new TcpListener(IPAddress.Any, Ataka);
        // Zerbitzaria martxan jartzen da; konexioak jasotzen hasiko da.
        zerbitzaria.Start();
        // Zerbitzaria entzuten ari dela logeatzen da; bezero maximo kopurua ere bistaratzen da.
        Console.WriteLine($"[TCP] {Ataka} atakan entzuten (gehienez {BezeroMaximoa} bezero)");

        // Onartutako bezero bakoitzeko begizta: bezero bakoitzeko bere hari propioa sortu.
        while (true)
        {
            // Bezero berria onartzen da eta socketa lortzen da.
            var bezeroa = zerbitzaria.AcceptTcpClient();
            // Konektatutako bezero kopurua berriro egiaztatzeko eta zerbitzariaren mugak ezartzeko.
            Berrezarri(ref _bezeroak, BezeroMaximoa);
            // Bezero berria konektatu denez, kontagailua handitu egiten da atomikoki.
            var n = Interlocked.Increment(ref _bezeroak);
            // Kontagailua muga gainditzen badu, bezeroaren konexioa ukatu eta mezua itzultzen da.
            if (n > BezeroMaximoa)
            {
                // Kontagailua handiagoa denez, konexioa errefusatua izan dela logeatzen da.
                Console.WriteLine("[TCP] Bezero gehiegi, konexioa baztertuta");
                // Bezeroari bidali behar zaion streama lortzen da.
                var ukatua = bezeroa.GetStream();
                // Erantzun formatuatu bat sortzen da JSON formatuan: eskaera ukatua.
                var byteak = System.Text.Encoding.UTF8.GetBytes
                    ("{\"arrakasta\":false,\"errorea\":\"SESSION_LIMIT\",\"datuak\":\"Zerbitzaria beteta dago (10/10). Saiatu berriro geroago\"}\n");
                // JSON mezua socketean idazten da.
                ukatua.Write(byteak, 0, byteak.Length);
                // Datuak deskargatu eta bidali daitezen flush egiten da.
                ukatua.Flush();
                // Konexioa itxi ondoren, erabili gabeko baliabideak askatzen dira.
                bezeroa.Close();
                // Konektatutako bezero kopurua berriro kendu egiten da, ukatu den konexioa kontuan hartuz.
                Interlocked.Decrement(ref _bezeroak);
                // Bezero hau baztertu denez, hurrengo iteraziora pasatzen da.
                continue;
            }

            // Bezeroa onartua izan dela logeatzen da, une horretako zenbakiarekin.
            Console.WriteLine($"[TCP] Bezeroa konektatuta ({n}/{BezeroMaximoa})");
            // Bezero bakoitzerako hari berezi bat sortzen da, erantzun asinkronoan gauzatzeko.
            var haria = new Thread(() =>
            {
                try
                {
                    // Bezeroari dagokion konexio objektua sortu eta bere atentzioa abiarazten da.
                    new BezeroKonexioa(bezeroa).Artatu();
                }
                finally
                {
                    // Hariak amaitzean, kontagailua jaitsi egiten da; beste bezeroak sartzeko leku librea sortuz.
                    Interlocked.Decrement(ref _bezeroak);
                    // Bezeroa deskonektatu denez, berriro logeatzen da egoera.
                    Console.WriteLine($"[TCP] Bezeroa deskonektatuta ({_bezeroak}/{BezeroMaximoa})");
                }
            })
            { IsBackground = true };
            // Sortutako haria abiarazten da, zerbitzaria kargatu gabe aktibo geratzen delarik.
            haria.Start();
        }
    }

    // Metodo honek etorkizuneko resetetarako errezerba gisa gordetzen da.
    private static void Berrezarri(ref int _, int __) { }
}
