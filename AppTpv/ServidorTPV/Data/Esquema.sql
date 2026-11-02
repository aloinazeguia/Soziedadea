-- ============================================================
--  AppTpv (Erronka WID+ZPP) — Esquema de base de datos SQLite
--
--  Diferencias de diseño respecto al prototipo Hostelería (MySQL):
--   * Usuario/rol       -> login (nuevo requisito 2)
--   * Producto.stock    -> stock de almacen (nuevo requisito 1.3)
--   * Producto.ingredientes -> "Ikus Osagaiak" (nuevo requisito 4)
--   * Reserva.turno     -> bazkaria/afaria separados (requisito 1.5)
--   * Reserva.fecha NULL-> reserva SIN fecha (requisito 1.5)
--   * Pedido/LineaPedido-> consumiciones + ticket + decremento de stock
--
--  Concurrencia: el servidor serializa ESCRITURAS con UN lock global
--  (el propio enunciado lo permite) + WAL de SQLite para lecturas.
-- ============================================================

PRAGMA foreign_keys = ON;
PRAGMA journal_mode = WAL;

-- ------------------------------------------------------------
-- Usuarios y roles (requisito 2)
--  rol: 'admin' gestiona usuarios y stock
--       'usuario' consume, imprime ticket, gestiona sus reservas
-- ------------------------------------------------------------

CREATE TABLE IF NOT EXISTS Usuario (
    id          INTEGER PRIMARY KEY AUTOINCREMENT,
    login       TEXT    NOT NULL UNIQUE,
    passHash    TEXT    NOT NULL,           -- SHA-256 del password + sal
    nombre      TEXT    NOT NULL,
    rol         TEXT    NOT NULL CHECK (rol IN ('admin', 'usuario')),
    activo      INTEGER NOT NULL DEFAULT 1  -- 0 = deshabilitado, no borramos filas de historia
);

-- ------------------------------------------------------------
-- Mesas (para interfaz de reservas y planos)
-- ------------------------------------------------------------

CREATE TABLE IF NOT EXISTS Mesa (
    id          INTEGER PRIMARY KEY AUTOINCREMENT,
    numMesa     INTEGER NOT NULL UNIQUE,
    capacidad   INTEGER NOT NULL DEFAULT 4
);

-- ------------------------------------------------------------
-- Reservas (requisito 1.5)
--  fecha NULL      -> reserva SIN fecha (solo turno)
--  turno           -> 'bazkaria' | 'afaria' SIEMPRE distinguidos
--  estado          -> 'activa' | 'cancelada' (borrar/cancelar no destruye)
-- ------------------------------------------------------------

CREATE TABLE IF NOT EXISTS Reserva (
    id               INTEGER PRIMARY KEY AUTOINCREMENT,
    idMesa           INTEGER NOT NULL,
    fecha            TEXT,                          -- NULL = sin fecha, solo turno
    turno            TEXT    NOT NULL CHECK (turno IN ('bazkaria', 'afaria')),
    numComensales    INTEGER NOT NULL,
    idUsuario        INTEGER NOT NULL,              -- quien la creó (usuario común)
    nombreCliente    TEXT    NOT NULL,
    observaciones    TEXT,
    estado           TEXT    NOT NULL DEFAULT 'activa'
                     CHECK (estado IN ('activa', 'cancelada')),
    FOREIGN KEY (idMesa)    REFERENCES Mesa(idMesa),
    FOREIGN KEY (idUsuario) REFERENCES Usuario(idUsuario)
);

CREATE INDEX IF NOT EXISTS idx_reserva_fecha_turno ON Reserva(fecha, turno);
CREATE INDEX IF NOT EXISTS idx_reserva_mesa ON Reserva(idMesa, fecha);

-- ------------------------------------------------------------
-- Carta / almacen (requisitos 1.3 y 4)
--  stock + activo        -> CRUD de stock, avisos de agotado
--  ingredientes ("osagaiak") -> visibles al elegir producto y al reservar
-- ------------------------------------------------------------

CREATE TABLE IF NOT EXISTS Producto (
    id            INTEGER PRIMARY KEY AUTOINCREMENT,
    nombre        TEXT    NOT NULL UNIQUE,
    precio        REAL    NOT NULL CHECK (precio >= 0),
    stock         INTEGER NOT NULL DEFAULT 0 CHECK (stock >= 0),
    ingredientes  TEXT,                           -- texto separado por comas
    categoria     TEXT    NOT NULL DEFAULT 'general',
    activo        INTEGER NOT NULL DEFAULT 1    -- oculto en la carta si stock=0
);

-- ------------------------------------------------------------
-- Consumiciones (interfaz principal del TPV, requisito 1.4)
--  Pedido abierto -> añade lineas -> confirmar (ticketa) -> cobrada
-- ------------------------------------------------------------

CREATE TABLE IF NOT EXISTS Pedido (
    id           INTEGER PRIMARY KEY AUTOINCREMENT,
    idUsuario    INTEGER NOT NULL,
    fechaHora    TEXT    NOT NULL,               -- "YYYY-MM-DD HH:mm:ss"
    estado       TEXT    NOT NULL DEFAULT 'abierta'
                         CHECK (estado IN ('abierta', 'cobrada', 'anulada')),
    total        REAL    NOT NULL DEFAULT 0,
    FOREIGN KEY (idUsuario) REFERENCES Usuario(idUsuario)
);

CREATE TABLE IF NOT EXISTS LineaPedido (
    id              INTEGER PRIMARY KEY AUTOINCREMENT,
    idPedido        INTEGER NOT NULL,
    idProducto      INTEGER NOT NULL,
    cantidad        INTEGER NOT NULL CHECK (cantidad > 0),
    precioUnitario  REAL    NOT NULL,            -- precio historizado en el momento
    FOREIGN KEY (idPedido)   REFERENCES Pedido(id) ON DELETE CASCADE,
    FOREIGN KEY (idProducto) REFERENCES Producto(id)
);

CREATE INDEX IF NOT EXISTS idx_linea_pedido ON LineaPedido(idPedido);
