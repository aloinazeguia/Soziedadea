// ============================================================
//  AppTpv — Eragiketa baten emaitza
// ============================================================
namespace ZerbitzariaTPV.Zerbitzuak;

public class Emaitza
{
    public bool Arrakasta { get; init; }
    public object? Datuak { get; init; }
    public string? Errorea { get; init; }

    public static Emaitza Ondo(object? datuak = null)
        => new() { Arrakasta = true, Datuak = datuak };

    public static Emaitza Txar(string errorea)
        => new() { Arrakasta = false, Errorea = errorea };
}
