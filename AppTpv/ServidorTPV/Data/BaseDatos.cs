// ============================================================
//  AppTpv — capa de acceso a datos (SQLite)
//
//  Reglas de la erronka que se resuelven aquí:
//   * Todo vive contra la BD (fichero tpv.db).
//   * ESCRITURAS serializadas con UN lock global (lo permite el
//     propio enunciado y evita corrupciones de segunda intención).
//   * La BD se crea sola al primer arranque desde Data/Esquema.sql.
// ============================================================
using Microsoft.Data.Sqlite;

namespace ServidorTPV.Data;

public static class BaseDatos
{
    // Ruta del fichero: carpeta data/ junto al ejecutable
    public static string RutaBD { get; } =
        Path.Combine(AppContext.BaseDirectory, "data", "tpv.db");

    // LOCK GLOBAL de escrituras (requisito 3 del enunciado):
    // toda mutacion pasa por este lock; las lecturas van sin lock (WAL).
    public static readonly object LockEscritura = new();

    public static SqliteConnection Conectar()
    {
        return new SqliteConnection($"Data Source={RutaBD}");
    }

    /// <summary>Crea la BD e inicializa Esquema + datos si no existia.</summary>
    public static void Inicializar()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(RutaBD)!);

        var nueva = !File.Exists(RutaBD) || new FileInfo(RutaBD).Length == 0;

        using var con = Conectar();
        con.Open();

        // Ejecuta el esquema (idempotente: IF NOT EXISTS en todo)
        var esquema = File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "Data", "Esquema.sql"));
        using (var cmd = con.CreateCommand())
        {
            cmd.CommandText = esquema;
            cmd.ExecuteNonQuery();
        }

        if (nueva)
            Sembrar(con);
    }

    private static void Sembrar(SqliteConnection con)
    {
        // Usuario administrador inicial (entregable: credenciales documentadas)
        // password "admin123" -> SHA-256 con sal fija del archivo
        using (var cmd = con.CreateCommand())
        {
            cmd.CommandText = @"
                INSERT OR IGNORE INTO Usuario (login, passHash, nombre, rol)
                VALUES ('admin', @hash, 'Administrador', 'admin');";
            cmd.Parameters.AddWithValue("@hash",
                Seguridad.HashPassword("admin123"));
            cmd.ExecuteNonQuery();
        }

        // Mesa(s) de ejemplo
        using (var cmd = con.CreateCommand())
        {
            cmd.CommandText = @"
                INSERT OR IGNORE INTO Mesa (numMesa, capacidad) VALUES (1, 4);
                INSERT OR IGNORE INTO Mesa (numMesa, capacidad) VALUES (2, 4);
                INSERT OR IGNORE INTO Mesa (numMesa, capacidad) VALUES (3, 6);";
            cmd.ExecuteNonQuery();
        }

        // Productos de ejemplo CON stock (requisito de almacen)
        using (var cmd = con.CreateCommand())
        {
            cmd.CommandText = @"
                INSERT OR IGNORE INTO Producto (nombre, precio, stock, ingredientes, categoria) VALUES
                ('Kola kontxa',   2.5, 24, 'kola, haragi eskuragabea', 'edariak'),
                ('Kalimotxo',     3.0, 40, 'ardo gorria, pilare',      'edariak'),
                ('Prentxikoa',    3.0, 12, 'tiegno, flan',             'edariak'),
                ('Sakramento burger', 6.5, 8, 'ogia, haragitxoa, gazta atxurra', 'janari');";
            cmd.ExecuteNonQuery();
        }
    }

    /// <summary>Pequeña prueba de saneamiento usada por Program.cs.</summary>
    public static (string Resumen, string Conexion) Verificar()
    {
        using var con = Conectar();
        con.Open();
        using var cmd = con.CreateCommand();
        cmd.CommandText = @"
            SELECT (SELECT COUNT(*) FROM Usuario) AS usuarios,
                   (SELECT COUNT(*) FROM Mesa)    AS mesas,
                   (SELECT COUNT(*) FROM Producto) AS productos;";
        using var reader = cmd.ExecuteReader();
        reader.Read();
        return ($"usuarios={reader.GetInt32(0)} mesas={reader.GetInt32(1)} productos={reader.GetInt32(2)}",
                con.ConnectionString);
    }
}
