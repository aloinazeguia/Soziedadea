// ============================================================
//  AppTpv — Sesiones activas (requisito 3: clientes simultaneos)
//  Token por cliente tras LOGIN; el rol viaja pegado al token.
// ============================================================
using System.Collections.Concurrent;

namespace ServidorTPV.Servicios;

public record SesionUsuario(
    int IdUsuario,
    string Login,
    string Nombre,
    string Rol);

public static class Sesiones
{
    private static readonly ConcurrentDictionary<string, SesionUsuario> Activas = new();

    public static string Crear(SesionUsuario s)
    {
        var token = Guid.NewGuid().ToString("N");
        Activas[token] = s;
        return token;
    }

    public static SesionUsuario? Obtener(string? token)
        => token != null && Activas.TryGetValue(token, out var s) ? s : null;

    public static void Cerrar(string? token)
    {
        if (token != null) Activas.TryRemove(token, out _);
    }

    public static int Total => Activas.Count;
}
