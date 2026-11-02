// ============================================================
//  AppTpv — Conexion de un cliente (bucle de lectura/respuesta)
//  Framing: UNA LINEA = UN MENSAJE JSON (UTF-8, separado por \n)
// ============================================================
using System.IO;
using System.Net.Sockets;
using System.Text.Json;
using ServidorTPV.Servicios;

namespace ServidorTPV.Red;

public class ClienteConexion
{
    private readonly TcpClient _cliente;

    public ClienteConexion(TcpClient cliente) { _cliente = cliente; }

    public void Atender()
    {
        using (_cliente)
        using (var flujo = _cliente.GetStream())
        using (var lector = new StreamReader(flujo, System.Text.Encoding.UTF8))
        using (var escritor = new StreamWriter(flujo, new System.Text.UTF8Encoding(false)) { AutoFlush = true })
        {
            escritor.NewLine = "\n";
            string? linea;
            while ((linea = lector.ReadLine()) != null)
            {
                if (linea.Trim() == "") continue;

                var respuesta = Procesar(linea);
                escritor.Write(respuesta);
                escritor.Write("\n");
            }
        }
    }

    private string Procesar(string linea)
    {
        try
        {
            using var doc = JsonDocument.Parse(linea);
            var raiz = doc.RootElement;

            var token = raiz.TryGetProperty("token", out var t) &&
                        t.ValueKind == JsonValueKind.String ? t.GetString() : null;
            var accion = raiz.TryGetProperty("accion", out var a) &&
                         a.ValueKind == JsonValueKind.String ? a.GetString() ?? "" : "";
            var datos = raiz.TryGetProperty("datos", out var d) &&
                        d.ValueKind == JsonValueKind.Object ? d.Clone() : default;

            if (accion == "LOGOUT")
            {
                Sesiones.Cerrar(token);
                return Json(Resultado.Bien(new { mensaje = "Sesion cerrada" }));
            }

            var sesion = Sesiones.Obtener(token);
            var resultado = GestorOperaciones.Ejecutar(sesion, accion, datos);
            return Json(resultado);
        }
        catch (JsonException)
        {
            return Json(Resultado.Mal("JSON invalido"));
        }
        catch (IOException)
        {
            throw; // se encarga el using/Atender
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CLIENTE] Error: {ex.Message}");
            return Json(Resultado.Mal("Error procesando el mensaje"));
        }
    }

    private static string Json(Resultado r)
    {
        var opciones = new JsonSerializerOptions { PropertyNamingPolicy = null };
        var objeto = r.Ok
            ? (object)new { ok = true, datos = r.Datos }
            : new { ok = false, error = r.Error };
        return JsonSerializer.Serialize(objeto, opciones);
    }
}
