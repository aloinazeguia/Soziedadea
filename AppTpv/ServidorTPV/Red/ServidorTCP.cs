// ============================================================
//  AppTpv — Servidor TCP (requisito 3 del enunciado)
//  * TcpListener en el puerto 5000
//  * maximo 10 clientes simultaneos
//  * un hilo por cliente: mientras el servidor responde, el
//    cliente puede seguir usando su interfaz
// ============================================================
using System.Net;
using System.Net.Sockets;

namespace ServidorTPV.Red;

public class ServidorTCP
{
    public const int Puerto = 5001;
    public const int MaxClientes = 10;

    private ServidorTCP() { }

    public static readonly ServidorTCP Instancia = new();
    private int _clientes;

    public void Arrancar()
    {
        var ouvinte = new TcpListener(IPAddress.Any, Puerto);
        ouvinte.Start();
        Console.WriteLine($"[TCP] Escuchando en el puerto {Puerto} (max {MaxClientes} clientes)");

        // bucle de aceptacion: cada cliente, un hilo propio
        while (true)
        {
            var cliente = ouvinte.AcceptTcpClient();
            Reset(ref _clientes, MaxClientes);
            var n = Interlocked.Increment(ref _clientes);
            if (n > MaxClientes)
            {
                Console.WriteLine("[TCP] Demasiados clientes, conexion rechazada");
                var denegado = cliente.GetStream();
                var bytes = System.Text.Encoding.UTF8.GetBytes
                    ("{\"ok\":false,\"error\":\"SESSION_LIMIT\",\"datos\":\"Servicio lleno (10/10). Reintentelo mas tarde\"}\n");
                denegado.Write(bytes, 0, bytes.Length);
                denegado.Flush();
                cliente.Close();
                Interlocked.Decrement(ref _clientes);
                continue;
            }

            Console.WriteLine($"[TCP] Cliente conectado ({n}/{MaxClientes})");
            var hilo = new Thread(() =>
            {
                try { new ClienteConexion(cliente).Atender(); }
                finally
                {
                    Interlocked.Decrement(ref _clientes);
                    Console.WriteLine($"[TCP] Cliente desconectado ({_clientes}/{MaxClientes})");
                }
            })
            { IsBackground = true };
            hilo.Start();
        }
    }

    private static void Reset(ref int _, int __) { } // (reserva de nombre para futuros resets)
}
