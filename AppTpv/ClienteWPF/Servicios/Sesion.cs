// ============================================================
//  AppTpv — Sesion local del usuario (lado cliente)
//  El token lo entrega el servidor tras LOGIN; aqui solo lo
//  guardamos para incluirlo en cada mensaje.
// ============================================================
namespace ClienteWPF.Servicios;

public static class Sesion
{
    public static string Token = "";
    public static string Login = "";
    public static string Nombre = "";
    public static string Rol = "";

    public static bool EsAdmin => Rol == "admin";

    public static void Cerrar()
    {
        Token = ""; Login = ""; Nombre = ""; Rol = "";
    }
}
