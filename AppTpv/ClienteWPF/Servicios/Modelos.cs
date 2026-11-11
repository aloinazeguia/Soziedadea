// ============================================================
//  AppTpv — Modelos que mapean las respuestas JSON del servidor
//  (los nombres de propiedad deben coincidir con el protocolo)
// ============================================================
using System.Text.Json.Serialization;

namespace ClienteWPF.Servicios;

public class ProductoVista
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("nombre")] public string Nombre { get; set; } = "";
    [JsonPropertyName("precio")] public double Precio { get; set; }
    [JsonPropertyName("stock")] public int Stock { get; set; }
    [JsonPropertyName("ingredientes")] public string Ingredientes { get; set; } = "";
    [JsonPropertyName("categoria")] public string Categoria { get; set; } = "";
    [JsonPropertyName("activo")] public int Activo { get; set; }

    public bool Agotado => Stock <= 0;
}

public class MesaVista
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("numMesa")] public int NumMesa { get; set; }
    [JsonPropertyName("capacidad")] public int Capacidad { get; set; }

    public string Descripcion => $"Mesa {NumMesa} (capacidad {Capacidad})";
}

public class ReservaVista
{
    [JsonPropertyName("idReserva")] public int IdReserva { get; set; }
    [JsonPropertyName("numMesa")] public int NumMesa { get; set; }
    [JsonPropertyName("fecha")] public string Fecha { get; set; } = "";
    [JsonPropertyName("turno")] public string Turno { get; set; } = "";
    [JsonPropertyName("numComensales")] public int NumComensales { get; set; }
    [JsonPropertyName("nombreCliente")] public string NombreCliente { get; set; } = "";
    [JsonPropertyName("observaciones")] public string Observaciones { get; set; } = "";
    [JsonPropertyName("estado")] public string Estado { get; set; } = "";
}

public class UsuarioVista
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("login")] public string Login { get; set; } = "";
    [JsonPropertyName("nombre")] public string Nombre { get; set; } = "";
    [JsonPropertyName("rol")] public string Rol { get; set; } = "";
    [JsonPropertyName("activo")] public int Activo { get; set; }
}

public class LineaVista
{
    [JsonPropertyName("nombre")] public string Nombre { get; set; } = "";
    [JsonPropertyName("cantidad")] public int Cantidad { get; set; }
    [JsonPropertyName("precioUnitario")] public double PrecioUnitario { get; set; }

    public double Importe => Math.Round(Cantidad * PrecioUnitario, 2);
}

/// <summary>
/// Linea del comandero. Se mantiene en memoria mientras el camarero
/// prepara el pedido: "pendiente" (aun no enviada al servidor) o
/// "enviado" (ya es una LineaPedido firme de la comanda abierta).
/// </summary>
public class LineaPedidoVista
{
    public int IdProducto { get; set; }
    public string Nombre { get; set; } = "";
    public int Cantidad { get; set; }
    public double PrecioUnitario { get; set; }
    public string Estado { get; set; } = "pendiente";   // pendiente | enviado

    public bool Enviada => Estado == "enviado";
    public double Importe => Math.Round(Cantidad * PrecioUnitario, 2);
}
