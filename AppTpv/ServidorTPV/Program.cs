// ============================================================
//  ServidorTPV — Fase 1 (probar BD): crea/verifica la BD SQLite y sale.
//  Fase 2 (proxima parada): servidor TCP + sesiones + lock de escrituras.
// ============================================================
using ServidorTPV.Data;

Console.WriteLine("=== ServidorTPV ===");
Console.WriteLine($"BD en: {BaseDatos.RutaBD}");

BaseDatos.Inicializar();

var (resumen, _) = BaseDatos.Verificar();
Console.WriteLine($"BD OK -> {resumen}");
Console.WriteLine("(Fase 1 completada: la cerramos por consola. Fase 2: TCP ajo)");
