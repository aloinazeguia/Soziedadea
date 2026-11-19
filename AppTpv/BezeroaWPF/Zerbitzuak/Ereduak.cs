// ============================================================
//  AppTpv — Zerbitzariaren JSON erantzunak mapatzen dituzten ereduak
//  (propietate-izenek protokoloarekin bat etorri behar dute)
// ============================================================
using System.Text.Json.Serialization;

namespace BezeroaWPF.Zerbitzuak;

public class ProduktuDatuak
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("izena")] public string Izena { get; set; } = "";
    [JsonPropertyName("prezioa")] public double Prezioa { get; set; }
    [JsonPropertyName("izakinak")] public int Izakinak { get; set; }
    [JsonPropertyName("osagaiak")] public string Osagaiak { get; set; } = "";
    [JsonPropertyName("kategoria")] public string Kategoria { get; set; } = "";
    [JsonPropertyName("aktibo")] public int Aktibo { get; set; }

    public bool Agortuta => Izakinak <= 0;
    public string AktiboErakutsi => Aktibo == 1 ? "Bai" : "Ez";
}

public class MahaiDatuak
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("mahaiZenbakia")] public int MahaiZenbakia { get; set; }
    [JsonPropertyName("edukiera")] public int Edukiera { get; set; }

    public string Deskribapena => $"Mahaia {MahaiZenbakia} (edukiera: {Edukiera})";
}

public class ErreserbaDatuak
{
    [JsonPropertyName("erreserbaId")] public int ErreserbaId { get; set; }
    [JsonPropertyName("mahaiZenbakia")] public int MahaiZenbakia { get; set; }
    [JsonPropertyName("data")] public string Data { get; set; } = "";
    [JsonPropertyName("txanda")] public string Txanda { get; set; } = "";
    [JsonPropertyName("pertsonaKopurua")] public int PertsonaKopurua { get; set; }
    [JsonPropertyName("bezeroIzena")] public string BezeroIzena { get; set; } = "";
    [JsonPropertyName("oharrak")] public string Oharrak { get; set; } = "";
    [JsonPropertyName("egoera")] public string Egoera { get; set; } = "";

    public string DataErakutsi => Data == "datarik gabe" ? "Datarik gabe" : Data;
}

public class ErabiltzaileDatuak
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("erabiltzailea")] public string ErabiltzaileIzena { get; set; } = "";
    [JsonPropertyName("izena")] public string Izena { get; set; } = "";
    [JsonPropertyName("rola")] public string Rola { get; set; } = "";
    [JsonPropertyName("aktibo")] public int Aktibo { get; set; }

    public string RolErakutsi => Rola == "admin" ? "Administratzailea" : "Erabiltzailea";
    public string AktiboErakutsi => Aktibo == 1 ? "Bai" : "Ez";
}

public class EskaeraLerroDatuak
{
    [JsonPropertyName("izena")] public string Izena { get; set; } = "";
    [JsonPropertyName("kopurua")] public int Kopurua { get; set; }
    [JsonPropertyName("prezioUnitarioa")] public double PrezioUnitarioa { get; set; }

    public double Zenbatekoa => Math.Round(Kopurua * PrezioUnitarioa, 2);
}

/// <summary>
/// Komandaren lerroa. Memorian mantentzen da zerbitzariak
/// eskaera prestatzen duen bitartean: "zain" (oraindik bidali gabe) edo
/// "bidalita" (komanda irekiaren LineaPedido gisa erregistratuta).
/// </summary>
public class EskaeraLerroa
{
    public int ProduktuId { get; set; }
    public string Izena { get; set; } = "";
    public int Kopurua { get; set; }
    public double PrezioUnitarioa { get; set; }
    public string Egoera { get; set; } = "zain";   // zain | bidalita

    public bool Bidalita => Egoera == "bidalita";
    public double Zenbatekoa => Math.Round(Kopurua * PrezioUnitarioa, 2);
}
