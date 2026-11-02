# PLAN — AppTpv (Erronka WID+ZPP)

## Fase 1 — Completada ✅ (2/11/2026)

- [x] Solución `AppTpv.sln` con proyecto `ServidorTPV` (console, net10.0)
- [x] Paquete `Microsoft.Data.Sqlite` integrado
- [x] Esquema SQLite completo (`Data/Esquema.sql`) cubriendo TODOS los requisitos de la erronka:
  - Usuario/rol (admin | usuario) + passHash
  - Mesa, Reserva (fecha NULL = sin fecha, turno bazkaria/afaria)
  - Producto con **stock** e **ingredientes (osagaiak)**
  - Pedido/LineaPedido (ticket + decremento de stock al confirmar)
- [x] `BaseDatos.Inicializar()` crea la BD sola al primer arranque
- [x] Lock global de escrituras (`LockEscritura`) preparado (exigencia de concurrencia del enunciado)
- [x] Semillas: admin/admin123, 3 mesas (cap. 4/4/6), 4 productos con stock
- [x] Verificado: `dotnet run` → "BD OK -> usuarios=1 mesas=3 productos=4"

## Fase 2 — Completada ✅ (2/11/2026)

### Qué está hecho
- [x] `Red/ServidorTCP.cs`: TcpListener en **5001** (5000 lo ocupa ControlCenter/AirPlay de macOS),
      máximo 10 clientes simultáneos (la 11ª recibe `{"error":"SESSION_LIMIT"}`), un hilo por cliente
- [x] `Red/ClienteConexion.cs`: framing JSON una-línea-por-mensaje, dispatch por token/acción, LOGOUT
- [x] `Servicios/Sesiones.cs`: token GUID tras LOGIN, diccionario concurrente, contador de sesiones
- [x] `Servicios/GestorOperaciones.cs`: **19 acciones** del protocolo con guardas de rol
      (RequiereAdmin / RequiereSesion), lock global de escritura + transacciones
- [x] `Program.cs` arranca BD + TCP; comando "parar" para cierre limpio

### Bugs corregidos durante las pruebas
- FKs mal referenciadas en `Esquema.sql` (`Mesa(idMesa)`/`Usuario(idUsuario)` → `...id`):
  provocaba "foreign key mismatch" al crear Reservas y Pedidos
- `INSERT ... ExecuteScalar()` devolvía 0 → `RETURNING id` en Res_Crear y Ped_Abrir

### Evidencias de pruebas (cliente TCP de node, suite de 40 pasos)
- LOGIN correcto/incorrecto, USUARIO_CREAR (+duple, +rol inválido), USUARIOS_LISTA
- Guardas de rol: "Accion reservada al administrador" para un usuario corriente
- Acciones sin token: "No hay sesion abierta (falta el token)"
- MESAS_LISTA / PRODUCTOS_LISTA con usuario común
- RESERVA_CREAR: sin fecha ✅, con fecha ✅, duple rechazada ✅, capacidad rechazada
  ("La mesa admite como maximo 4 comensales"), turno inválido, formato de fecha
- RESERVA_LISTA (total y filtrada por fecha), MODIFICAR, BORRAR (cancelada), otra persona/sin id
- PEDIDO_ABRIR, AGREGAR_LINEA (+stock insuficiente: "quedan 12"), producto inexistente
- PEDIDO_CONFIRMAR: total 30 ✅ y **decremento de stock verificado** (Prentxikoa 12 → 2)
- AGREGAR tras cobrar rechazado, segundo pedido, "Ya tienes el pedido 2 abierto", cancelar
- TICKET_OBTENER: cliente, fecha, estado cobrada, lineas, total ✅
- LOGOUT y acción tras logout rechazada
- Límite de 10 clientes: script con 11 conexiones → la 11ª rechazada con SESSION_LIMIT

### Fichero de BD durante pruebas
```
AppTpv/ServidorTPV/bin/Debug/net10.0/data/tpv.db   (se recrea con semillas si se borra)
```

### Pendientes conocidos (no bloqueantes)
- Mensaje "Cliente desconectado" del log a veces desordenado (contador leído entre hilos): cosmético
- RESERVA_MODIFICAR no revalida capacidad/duple al cambiar mesa/fecha: pulirlo si la UI lo pide
- Copias de seguridad de BD y credenciales documentadas: para la entrega final

## Próxima parada — Fase 3
ClienteWPF: las 5 interfaces del enunciado sobre `docs/PROTOCOLO.md`, probado en la VM UTM
(Windows); carpetas compartidas de UTM para pasar binarios. El servidor ya cumple el contrato.
