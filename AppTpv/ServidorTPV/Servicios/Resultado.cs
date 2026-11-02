// ============================================================
//  AppTpv — Resultado de una operacion de negocio
// ============================================================
namespace ServidorTPV.Servicios;

public class Resultado
{
    public bool Ok { get; init; }
    public object? Datos { get; init; }
    public string? Error { get; init; }

    public static Resultado Bien(object? datos = null)
        => new() { Ok = true, Datos = datos };

    public static Resultado Mal(string error)
        => new() { Ok = false, Error = error };
}
