# Fase 1 — Completada ✅ (2/11/2026)

## Qué está hecho
- [x] Solución `AppTpv.sln` con proyecto `ServidorTPV` (console, net10.0)
- [x] Paquete `Microsoft.Data.Sqlite` integrado
- [x] Esquema SQLite completo (`Data/Esquema.sql`) cubriendo TODOS los requisitos de la erronka:
  - Usuario/rol (admin | usuario) + passHash
  - Mesa, Reserva (fecha NULL = sin fecha, turno bazkaria/afaria)
  - Producto con **stock** e **ingredientes (osagaiak)**
  - Pedido/LineaPedido (ticket + decremento de stock al confirmar)
- [x] `BaseDatos.Inicializar()` crea la BD sola al primer arranque
- [x] Lock global de escrituras (`LockEscritura`) preparado (exigencia de concurrencia del enunciado)
- [x] Semillas: admin/admin123, 3 mesas, 4 productos con stock
- [x] Verificado: `dotnet run` → "BD OK -> usuarios=1 mesas=3 productos=4"

## Escrache del fichero de BD
```
AppTpv/ServidorTPV/bin/Debug/net10.0/data/tpv.db   (56 KB, 7 tablas)
```

## Próxima parada — Fase 2 (ver docs/PROTOCOLO.md)
Servidor TCP: TcpListener puerto 5000, sesión por cliente (máx 10),
mensajes JSON línea-a-línea, login → token, operaciones CRUD + compra con
decremento de stock y cálculo de ticket dentro del lock de escritura.
