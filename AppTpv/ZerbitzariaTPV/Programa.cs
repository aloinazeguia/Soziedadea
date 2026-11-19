// ============================================================
//  ZerbitzariaTPV — 2. fasea (TCP zerbitzaria martxan)
//  BD abiarazi eta bezeroei 5001 atakan erantzuten die.
//  Idatzi "parar" + Enter garbi ixteko (Ctrl+C ere balio du).
// ============================================================
using ZerbitzariaTPV.Datuak;
using ZerbitzariaTPV.Sarea;

Console.WriteLine("=== ZerbitzariaTPV (2. fasea: TCP) ===");

DatuBasea.Hasieratu();
var (laburpena, _) = DatuBasea.Egiaztatu();
Console.WriteLine($"BD OK -> {laburpena}");

var zerbitzaria = new Thread(TCPZerbitzaria.Instantzia.Abiarazi)
{ IsBackground = true };
zerbitzaria.Start();

Console.WriteLine("Zerbitzaria martxan. Idatzi 'parar' ixteko.");
while (true)
{
    var lerroa = Console.ReadLine();
    if (lerroa != null && lerroa.Trim().ToLower() == "parar")
    {
        Console.WriteLine("Zerbitzaria ixten...");
        Environment.Exit(0);
    }
}
