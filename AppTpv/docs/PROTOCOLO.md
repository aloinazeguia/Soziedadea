# Protocolo TCP — AppTpv (Fase 2)

## Conexión
- `TcpListener` en **puerto 5000**.
- El servidor acepta hasta **10 clientes simultáneos** (enunciado).
  Cliente 11 → recibe error `SESSION_LIMIT` y cierra.
- **Un hilo por cliente** (lectura bloqueante), la UI del cliente no se
  bloquea mientras el servidor responde (otro requisito del enunciado).
- Framing: **una línea = un mensaje** (`\n` como separador). JSON UTF-8.

## Formato del mensaje (<*JSON>)

```json
{ "accion": "LOGIN", "datos": { "login": "admin", "pass": "admin123" } }
```

Respuesta:

```json
{ "ok": true, "datos": { "token": "a1b2...", "rol": "admin", "nombre": "Admin" } }
```

```json
{ "ok": false, "error": "Credenciales invalidas" }
```

## Sesión y token
- `LOGIN` devuelve un `token` (GUID). Después de eso, TODOS los mensajes
  del cliente llevan `"token"` y el servidor valida el rol asociado.
- El administrador tiene acciones extra (`USUARIO_*, STOCK_*)`; si un
  usuario común las llama → `{ok:false, error:"No autorizado"}`.

## Acciones previstas

| Acción | Rol | Datos | Devuelve |
|---|---|---|---|
| `LOGIN` | – | login, pass | token, rol, nombre |
| `PRODUCTOS_LISTA` | ambos | – | productos (con stock/ingredientes) |
| `PRODUCTO_AGREGAR / _MODIFICAR / _BORRAR` | admin | datos producto | ok |
| `STOCK_MODIFICAR` | admin | idProducto, nuevoStock | ok |
| `RESERVA_CREAR` | usuario | mesa, fecha (opcional), turno, comensales | ok |
| `RESERVA_MODIFICAR / _BORRAR` | usuario | idReserva + cambios | ok |
| `RESERVA_LISTA` | ambos | fecha, turno (opcional) | reservas |
| `PEDIDO_ABRIR` | usuario | – | idPedido |
| `PEDIDO_AGREGAR_LINEA` | usuario | idPedido, idProducto, cantidad | subtotal; valida stock |
| `PEDIDO_CONFIRMAR` | usuario | idPedido | total + decrementa stock (en un mismo lock) |
| `PEDIDO_CANCELAR` | usuario | idPedido | ok |
| `TICKET_OBTENER` | usuario | idPedido | lineas + total (para imprimir) |

## Concurrencia (otra exigencia del enunciado)
- **Un único `lock` global en el servidor** alrededor de TODA secuencia
  escribir-DB (crear pedido, lineas, decremento stock, contador de tickets).
- Con SQLite en modo WAL, las LECTURAS pueden ir en paralelo sin lock.
- Justificación en la defensa: con ≤10 clientes y escrituras cortas no
  hace falta lock por tabla (el propio enunciado lo permite) y así se
  evitan los deadlocks.
