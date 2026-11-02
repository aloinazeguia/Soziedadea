// ============================================================
//  ServidorTPV — Fase 2 (servidor TCP funcionando)
//  Arranca la BD y sirve a los clientes por el puerto 5000.
//  Escribe "parar" + Enter para terminar limpio (Ctrl+C tambien).
// ============================================================
using ServidorTPV.Data;
using ServidorTPV.Red;

Console.WriteLine("=== ServidorTPV (Fase 2: TCP) ===");

BaseDatos.Inicializar();
var (resumen, _) = BaseDatos.Verificar();
Console.WriteLine($"BD OK -> {resumen}");

var servidor = new Thread(ServidorTCP.Instancia.Arrancar)
{ IsBackground = true };
servidor.Start();

Console.WriteLine("Server running. Escribe 'parar' para cerrar.");
while (true)
{
    var linea = Console.ReadLine();
    if (linea != null && linea.Trim().ToLower() == "parar")
    {
        Console.WriteLine("Cerrando servidor...");
        Environment.Exit(0);
    }
}
