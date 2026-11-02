// ============================================================
//  AppTpv — Conexion TCP con el ServidorTPV
//
//  Una sola conexion persistente por ventana de app (requisito 3:
//  el servidor admite hasta 10 clientes). Protocolo de
//  docs/PROTOCOLO.md: una linea = un mensaje JSON con
//  { accion, token, datos } y respuesta { ok, datos?, error? }.
//
//  El lock serializa los envios; las llamadas son sincronas
//  (el servidor responde en milisegundos en red local).
// ============================================================
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace ClienteWPF.Servicios;

public class Respuesta
{
    public bool Ok { get; init; }
    public JsonElement Datos { get; init; }
    public string Error { get; init; } = "";
}

public static class Conexion
{
    // En la VM de Windows hay que apuntar a la IP del HOST (Mac),
    // no a 127.0.0.1. Se puede fijar con un fichero "servidor.txt"
    // junto al ejecutable, en su primera linea: IP [:puerto]
    public static string Host = "127.0.0.1";
    public static int Puerto = 5001;

    private static TcpClient? _cliente;
    private static StreamReader? _lector;
    private static StreamWriter? _escritor;
    private static readonly object _candado = new();

    static Conexion()
    {
        try
        {
            var ruta = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "servidor.txt");
            if (File.Exists(ruta))
            {
                var primera = File.ReadAllLines(ruta).FirstOrDefault()?.Trim();
                if (!string.IsNullOrEmpty(primera))
                {
                    var trozos = primera.Split(':');
                    if (trozos.Length >= 1 && trozos[0] != "") Host = trozos[0];
                    if (trozos.Length >= 2 && int.TryParse(trozos[1], out var p)) Puerto = p;
                }
            }
        }
        catch { /* config opcional: ante lo peor, 127.0.0.1:5001 */ }
    }

    /// <summary>/envia una accion y espera su respuesta (una linea JSON).</summary>
    public static Respuesta Enviar(string accion, object? datos = null)
    {
        lock (_candado)
        {
            try
            {
                AsegurarConexion();

                var mensaje = JsonSerializer.Serialize(new
                {
                    accion,
                    token = Sesion.Token,
                    datos = datos ?? new { }
                });
                _escritor!.WriteLine(mensaje);
                _escritor.Flush();

                var linea = _lector!.ReadLine();
                if (linea == null)
                    return new Respuesta { Ok = false, Error = "El servidor cerro la conexion" };

                using var doc = JsonDocument.Parse(linea);
                var raiz = doc.RootElement;
                bool ok = raiz.GetProperty("ok").GetBoolean();
                string error = raiz.TryGetProperty("error", out var e) &&
                               e.ValueKind == JsonValueKind.String
                    ? e.GetString() ?? "" : "";
                JsonElement datosR = raiz.TryGetProperty("datos", out var d)
                    ? d.Clone() : default;
                return new Respuesta { Ok = ok, Datos = datosR, Error = error };
            }
            catch (SocketException ex)
            {
                Cerrar();
                return new Respuesta
                {
                    Ok = false,
                    Error = $"No se pudo conectar al servidor {Host}:{Puerto} ({ex.Message})"
                };
            }
            catch (IOException)
            {
                Cerrar();
                return new Respuesta { Ok = false, Error = "Se perdio la conexion con el servidor" };
            }
            catch (JsonException)
            {
                return new Respuesta { Ok = false, Error = "Respuesta no valida del servidor" };
            }
        }
    }

    private static void AsegurarConexion()
    {
        if (_cliente is { Connected: true }) return;
        Cerrar();
        // LeeLinea del servidor: cierra si el cliente murmura
        _cliente = new TcpClient();
        var tarea = _cliente.ConnectAsync(Host, Puerto);
        if (!tarea.Wait(5000))
            throw new SocketException((int)SocketError.TimedOut);

        var flujo = _cliente.GetStream();
        _escritor = new StreamWriter(flujo, new UTF8Encoding(false)) { AutoFlush = true };
        _lector = new StreamReader(flujo, Encoding.UTF8);
    }

    public static void Cerrar()
    {
        try { _cliente?.Close(); } catch { /* ya cerrado */ }
        _cliente = null;
        _lector = null;
        _escritor = null;
    }
}
