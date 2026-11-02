// ============================================================
//  AppTpv — Gestor de operaciones (logica de negocio)
//
//  Cada accion del protocolo (docs/PROTOCOLO.md) llega aqui.
//  Reglas:
//   * Las ESCRITURAS a DB van dentro de BaseDatos.LockEscritura
//     (un solo lock global, como permite el enunciado).
//   * Los roles se comprueban aqui: solo 'admin' puede CRUD de
//     usuarios y stock.
//   * La compra (PEDIDO_CONFIRMAR) valida stock Y decrementa
//     dentro de la MISMA seccion critica y transaccion.
// ============================================================
using System.Text.Json;
using Microsoft.Data.Sqlite;
using ServidorTPV.Data;

namespace ServidorTPV.Servicios;

public static class GestorOperaciones
{
    public static Resultado Ejecutar(SesionUsuario? sesion, string accion, JsonElement datos)
    {
        try
        {
            return accion switch
            {
                // ---- Sesión (no necesita token) ----
                "LOGIN" => Login(datos),

                // ---- Carta / stock (solo admin para mutar) ----
                "PRODUCTOS_LISTA"     => ProductosLista(),
                "PRODUCTO_AGREGAR"    => RequiereAdmin(sesion, () => Pro_Agregar(datos)),
                "PRODUCTO_MODIFICAR"  => RequiereAdmin(sesion, () => Pro_Modificar(datos)),
                "PRODUCTO_BORRAR"     => RequiereAdmin(sesion, () => Pro_Borrar(datos)),
                "STOCK_MODIFICAR"     => RequiereAdmin(sesion, () => Stock_Modificar(datos)),

                // ---- Usuarios (solo admin) ----
                "USUARIO_CREAR"       => RequiereAdmin(sesion, () => Usu_Crear(datos)),
                "USUARIO_MODIFICAR"   => RequiereAdmin(sesion, () => Usu_Modificar(datos)),
                "USUARIO_BORRAR"      => RequiereAdmin(sesion, () => Usu_Borrar(datos)),
                "USUARIOS_LISTA"      => RequiereAdmin(sesion, () => Usu_Lista()),

                // ---- Mesas y reservas (usuario comun) ----
                "MESAS_LISTA"         => RequiereSesion(sesion, () => Mesas_Lista()),
                "RESERVA_CREAR"       => RequiereSesion(sesion, () => Res_Crear(sesion!, datos)),
                "RESERVA_MODIFICAR"   => RequiereSesion(sesion, () => Res_Modificar(sesion!, datos)),
                "RESERVA_BORRAR"      => RequiereSesion(sesion, () => Res_Borrar(sesion!, datos)),
                "RESERVA_LISTA"       => RequiereSesion(sesion, () => Res_Lista(datos)),

                // ---- Consumiciones / ticket ----
                "PEDIDO_ABRIR"        => RequiereSesion(sesion, () => Ped_Abrir(sesion!)),
                "PEDIDO_AGREGAR_LINEA"=> RequiereSesion(sesion, () => Ped_AgregarLinea(sesion!, datos)),
                "PEDIDO_CONFIRMAR"    => RequiereSesion(sesion, () => Ped_Confirmar(sesion!, datos)),
                "PEDIDO_CANCELAR"     => RequiereSesion(sesion, () => Ped_Cancelar(sesion!, datos)),
                "TICKET_OBTENER"      => RequiereSesion(sesion, () => Ped_Ticket(datos)),

                _ => Resultado.Mal($"Accion desconocida: {accion}")
            };
        }
        catch (ExNoAutorizado ex)
        {
            return Resultado.Mal(ex.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] {accion}: {ex.Message}");
            return Resultado.Mal($"Error interno en {accion}: {ex.Message}");
        }
    }

    // ---------- Guardas de rol ----------

    private sealed class ExNoAutorizado : Exception
    {
        public ExNoAutorizado(string msg) : base(msg) { }
    }

    private static Resultado RequiereAdmin(SesionUsuario? s, Func<Resultado> accion)
    {
        if (s == null) throw new ExNoAutorizado("No hay sesion abierta (falta el token)");
        if (s.Rol != "admin") throw new ExNoAutorizado("Accion reservada al administrador");
        return accion();
    }

    private static Resultado RequiereSesion(SesionUsuario? s, Func<Resultado> accion)
    {
        if (s == null) throw new ExNoAutorizado("No hay sesion abierta (falta el token)");
        return accion();
    }

    // ---------- Lecturas auxiliares de "datos" ----------

    private static string Str(JsonElement d, string k)
        => d.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString() ?? "" : "";

    private static int Int(JsonElement d, string k)
        => d.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.Number
            ? v.GetInt32() : 0;

    private static double Num(JsonElement d, string k)
        => d.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.Number
            ? v.GetDouble() : 0;

    private static string? StrOpt(JsonElement d, string k)
        => d.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString() : null;

    private static bool Tiene(JsonElement d, string k) => d.TryGetProperty(k, out _);

    // =====================================================
    //                 SESION
    // =====================================================

    private static Resultado Login(JsonElement d)
    {
        var login = Str(d, "login");
        var pass  = Str(d, "pass");
        if (login == "" || pass == "")
            return Resultado.Mal("Faltan login o pass");

        using var con = BaseDatos.Conectar();
        con.Open();
        using var cmd = con.CreateCommand();
        cmd.CommandText = @"SELECT id, passHash, nombre, rol FROM Usuario
                             WHERE login = @l AND activo = 1";
        cmd.Parameters.AddWithValue("@l", login);
        using var r = cmd.ExecuteReader();
        if (!r.Read())
            return Resultado.Mal("Credenciales invalidas");

        var hash = Seguridad.HashPassword(pass);
        if (r.GetString(1) != hash)
            return Resultado.Mal("Credenciales invalidas");

        var sesion = new SesionUsuario(r.GetInt32(0), login, r.GetString(2), r.GetString(3));
        var token = Sesiones.Crear(sesion);
        Console.WriteLine($"[SESION] {login} ({sesion.Rol}) - sesiones: {Sesiones.Total}");
        return Resultado.Bien(new { token, rol = sesion.Rol, nombre = sesion.Nombre });
    }

    // =====================================================
    //      CARTA / STOCK (mutaciones solo admin)
    // =====================================================

    private static Resultado ProductosLista()
    {
        using var con = BaseDatos.Conectar();
        con.Open();
        using var cmd = con.CreateCommand();
        cmd.CommandText = @"SELECT id, nombre, precio, stock, ingredientes, categoria, activo
                              FROM Producto ORDER BY categoria, nombre";
        var lista = new List<object>();
        using var r = cmd.ExecuteReader();
        while (r.Read())
            lista.Add(new { id = r.GetInt32(0), nombre = r.GetString(1),
                precio = r.GetDouble(2), stock = r.GetInt32(3),
                ingredientes = r.IsDBNull(4) ? "" : r.GetString(4),
                categoria = r.GetString(5), activo = r.GetInt32(6) });
        return Resultado.Bien(new { productos = lista });
    }

    private static Resultado Pro_Agregar(JsonElement d)
    {
        var nombre = Str(d, "nombre");
        var precio = Num(d, "precio");
        var stock  = Int(d, "stock");
        if (nombre == "" || precio <= 0)
            return Resultado.Mal("Nombre y precio son obligatorios (precio > 0)");

        return Escritura(con =>
        {
            using var cmd = con.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Producto (nombre, precio, stock, ingredientes, categoria)
                VALUES (@n, @p, @s, @i, @c)";
            cmd.Parameters.AddWithValue("@n", nombre);
            cmd.Parameters.AddWithValue("@p", precio);
            cmd.Parameters.AddWithValue("@s", stock);
            cmd.Parameters.AddWithValue("@i", (object?)StrOpt(d, "ingredientes") ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@c", Str(d, "categoria") is "" ? "general" : Str(d, "categoria"));
            var filas = cmd.ExecuteNonQuery();
            return filas == 0
                ? Resultado.Mal("Ya existe un producto con ese nombre")
                : Resultado.Bien(new { mensaje = "Producto creado" });
        });
    }

    private static Resultado Pro_Modificar(JsonElement d)
    {
        var id = Int(d, "id");
        if (id <= 0) return Resultado.Mal("Falta el id del producto");

        return Escritura(con =>
        {
            using var cmd = con.CreateCommand();
            cmd.CommandText = @"
                UPDATE Producto
                   SET nombre      = COALESCE(@n, nombre),
                       precio      = CASE WHEN @p2 > 0 THEN @p ELSE precio END,
                       stock       = CASE WHEN @tieneS = 1 THEN @s ELSE stock END,
                       ingredientes= COALESCE(@i, ingredientes),
                       categoria   = COALESCE(@c, categoria),
                       activo      = COALESCE(@a, activo)
                 WHERE id = @id";
            cmd.Parameters.AddWithValue("@n", (object?)StrOpt(d, "nombre") ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@p", Num(d, "precio"));
            cmd.Parameters.AddWithValue("@p2", Num(d, "precio"));
            cmd.Parameters.AddWithValue("@tieneS", Tiene(d, "stock") ? 1 : 0);
            cmd.Parameters.AddWithValue("@s", Int(d, "stock"));
            cmd.Parameters.AddWithValue("@i", (object?)StrOpt(d, "ingredientes") ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@c", (object?)StrOpt(d, "categoria") ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@a", Tiene(d, "activo") ? (object)Int(d, "activo") : DBNull.Value);
            cmd.Parameters.AddWithValue("@id", id);
            return cmd.ExecuteNonQuery() == 0
                ? Resultado.Mal("Producto no encontrado")
                : Resultado.Bien(new { mensaje = "Producto actualizado" });
        });
    }

    private static Resultado Pro_Borrar(JsonElement d)
    {
        var id = Int(d, "id");
        if (id <= 0) return Resultado.Mal("Falta el id del producto");

        // Borrado LOGICO: si el producto ya aparece en lineas de pedidos,
        // lo desactivamos para no romper la historia del ticket.
        return Escritura(con =>
        {
            using var cmd = con.CreateCommand();
            cmd.CommandText = @"
                UPDATE Producto SET activo = 0 WHERE id = @id";
            cmd.Parameters.AddWithValue("@id", id);
            return cmd.ExecuteNonQuery() == 0
                ? Resultado.Mal("Producto no encontrado")
                : Resultado.Bien(new { mensaje = "Producto eliminado (desactivado)" });
        });
    }

    private static Resultado Stock_Modificar(JsonElement d)
    {
        var id = Int(d, "id");
        var stock = Int(d, "stock");
        if (id <= 0 || stock < 0)
            return Resultado.Mal("id invalido o stock negativo");

        return Escritura(con =>
        {
            using var cmd = con.CreateCommand();
            cmd.CommandText = "UPDATE Producto SET stock = @s WHERE id = @id";
            cmd.Parameters.AddWithValue("@s", stock);
            cmd.Parameters.AddWithValue("@id", id);
            return cmd.ExecuteNonQuery() == 0
                ? Resultado.Mal("Producto no encontrado")
                : Resultado.Bien(new { mensaje = $"Stock actualizado a {stock}" });
        });
    }

    // =====================================================
    //    USUARIOS (solo admin, requisito 1.2 y 2)
    // =====================================================

    private static Resultado Usu_Crear(JsonElement d)
    {
        var login = Str(d, "login");
        var pass  = Str(d, "pass");
        var nombre = Str(d, "nombre");
        var rol   = Str(d, "rol");
        if (login == "" || pass == "" || nombre == "")
            return Resultado.Mal("Login, pass y nombre son obligatorios");
        if (rol != "admin" && rol != "usuario")
            return Resultado.Mal("El rol debe ser 'admin' o 'usuario'");

        return Escritura(con =>
        {
            using var cmd = con.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Usuario (login, passHash, nombre, rol)
                VALUES (@l, @h, @n, @r)";
            cmd.Parameters.AddWithValue("@l", login);
            cmd.Parameters.AddWithValue("@h", Seguridad.HashPassword(pass));
            cmd.Parameters.AddWithValue("@n", nombre);
            cmd.Parameters.AddWithValue("@r", rol);
            var filas = cmd.ExecuteNonQuery();
            return filas == 0
                ? Resultado.Mal("Ya existe ese login")
                : Resultado.Bien(new { mensaje = $"Usuario {login} creado" });
        });
    }

    private static Resultado Usu_Modificar(JsonElement d)
    {
        var id = Int(d, "id");
        if (id <= 0) return Resultado.Mal("Falta el id de usuario");

        return Escritura(con =>
        {
            using var cmd = con.CreateCommand();
            cmd.CommandText = @"
                UPDATE Usuario
                   SET nombre = COALESCE(@n, nombre),
                       passHash = COALESCE(@h, passHash),
                       rol = COALESCE(@r, rol),
                       activo = COALESCE(@a, activo)
                 WHERE id = @id";
            cmd.Parameters.AddWithValue("@n", (object?)StrOpt(d, "nombre") ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@h", Str(d, "pass") == ""
                ? DBNull.Value : (object)Seguridad.HashPassword(Str(d, "pass")));
            var rol = Str(d, "rol");
            cmd.Parameters.AddWithValue("@r", (rol == "admin" || rol == "usuario")
                ? (object)rol : DBNull.Value);
            cmd.Parameters.AddWithValue("@a", Tiene(d, "activo")
                ? (object)Int(d, "activo") : DBNull.Value);
            cmd.Parameters.AddWithValue("@id", id);
            return cmd.ExecuteNonQuery() == 0
                ? Resultado.Mal("Usuario no encontrado")
                : Resultado.Bien(new { mensaje = "Usuario actualizado" });
        });
    }

    private static Resultado Usu_Borrar(JsonElement d)
    {
        var id = Int(d, "id");
        if (id <= 0) return Resultado.Mal("Falta el id de usuario");

        return Escritura(con =>
        {
            using var cmd = con.CreateCommand();
            cmd.CommandText = "UPDATE Usuario SET activo = 0 WHERE id = @id";
            cmd.Parameters.AddWithValue("@id", id);
            return cmd.ExecuteNonQuery() == 0
                ? Resultado.Mal("Usuario no encontrado")
                : Resultado.Bien(new { mensaje = "Usuario deshabilitado" });
        });
    }

    private static Resultado Usu_Lista()
    {
        using var con = BaseDatos.Conectar();
        con.Open();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "SELECT id, login, nombre, rol, activo FROM Usuario ORDER BY id";
        var lista = new List<object>();
        using var r = cmd.ExecuteReader();
        while (r.Read())
            lista.Add(new { id = r.GetInt32(0), login = r.GetString(1),
                nombre = r.GetString(2), rol = r.GetString(3), activo = r.GetInt32(4) });
        return Resultado.Bien(new { usuarios = lista });
    }

    // =====================================================
    //    MESAS Y RESERVAS (requisito 1.5 y 4)
    // =====================================================

    private static Resultado Mesas_Lista()
    {
        using var con = BaseDatos.Conectar();
        con.Open();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "SELECT id, numMesa, capacidad FROM Mesa ORDER BY numMesa";
        var lista = new List<object>();
        using var r = cmd.ExecuteReader();
        while (r.Read())
            lista.Add(new { id = r.GetInt32(0), numMesa = r.GetInt32(1), capacidad = r.GetInt32(2) });
        return Resultado.Bien(new { mesas = lista });
    }

    private static Resultado Res_Crear(SesionUsuario ses, JsonElement d)
    {
        var idMesa = Int(d, "idMesa");
        var turno = Str(d, "turno");
        var comensales = Int(d, "numComensales");
        var fecha = StrOpt(d, "fecha");           // puede venir vacia: reserva SIN fecha
        var nombre = Str(d, "nombreCliente");
        var obs = StrOpt(d, "observaciones");

        if (idMesa <= 0 || comensales <= 0)
            return Resultado.Mal("Falta la mesa o los comensales no son validos");
        if (turno != "bazkaria" && turno != "afaria")
            return Resultado.Mal("El turno debe ser 'bazkaria' o 'afaria'");
        if (nombre == "")
            return Resultado.Mal("Falta el nombre del cliente");
        DateTime? f = null;
        if (!string.IsNullOrEmpty(fecha))
        {
            if (!DateTime.TryParseExact(fecha, "yyyy-MM-dd", null,
                    System.Globalization.DateTimeStyles.None, out var parsed))
                return Resultado.Mal("La fecha debe ser YYYY-MM-DD (o venir vacia)");
            f = parsed;
        }

        return Escritura(con =>
        {
            using var trans = con.BeginTransaction();

            // capacidad
            using (var c = con.CreateCommand())
            {
                c.Transaction = trans;
                c.CommandText = "SELECT capacidad FROM Mesa WHERE id = @i";
                c.Parameters.AddWithValue("@i", idMesa);
                var cap = c.ExecuteScalar();
                if (cap == null)
                {
                    trans.Rollback();
                    return Resultado.Mal("La mesa no existe");
                }
                if (comensales > (long)cap)
                {
                    trans.Rollback();
                    return Resultado.Mal($"La mesa admite como maximo {cap} comensales");
                }
            }

            // duple: mesa + fecha + turno, solo si hay fecha
            if (f != null)
            {
                using var c2 = con.CreateCommand();
                c2.Transaction = trans;
                c2.CommandText = @"
                    SELECT 1 FROM Reserva
                     WHERE idMesa = @m AND fecha = @f AND turno = @t
                       AND estado = 'activa'";
                c2.Parameters.AddWithValue("@m", idMesa);
                c2.Parameters.AddWithValue("@f", f!.Value.ToString("yyyy-MM-dd"));
                c2.Parameters.AddWithValue("@t", turno);
                if (c2.ExecuteScalar() != null)
                {
                    trans.Rollback();
                    return Resultado.Mal($"Esa mesa ya esta reservada {turno} en esa fecha");
                }
            }

            using var ins = con.CreateCommand();
            ins.Transaction = trans;
            ins.CommandText = @"
                INSERT INTO Reserva (idMesa, fecha, turno, numComensales,
                                     idUsuario, nombreCliente, observaciones, estado)
                VALUES (@m, @f, @t, @n, @u, @c, @o, 'activa')
                RETURNING id";
            ins.Parameters.AddWithValue("@m", idMesa);
            ins.Parameters.AddWithValue("@f", f == null ? DBNull.Value : (object)f.Value.ToString("yyyy-MM-dd"));
            ins.Parameters.AddWithValue("@t", turno);
            ins.Parameters.AddWithValue("@n", comensales);
            ins.Parameters.AddWithValue("@u", ses.IdUsuario);
            ins.Parameters.AddWithValue("@c", nombre);
            ins.Parameters.AddWithValue("@o", (object?)obs ?? DBNull.Value);
            var idRes = Convert.ToInt32(ins.ExecuteScalar());
            trans.Commit();
            return Resultado.Bien(new { idReserva = idRes,
                mensaje = "Reserva creada", fecha = (object?)fecha ?? "sin fecha", turno });
        });
    }

    private static Resultado Res_Modificar(SesionUsuario ses, JsonElement d)
    {
        var id = Int(d, "idReserva");
        if (id <= 0) return Resultado.Mal("Falta el id de la reserva");

        return Escritura(con =>
        {
            using var c = con.CreateCommand();
            c.CommandText = @"SELECT idUsuario FROM Reserva WHERE id = @i AND estado = 'activa'";
            c.Parameters.AddWithValue("@i", id);
            var dueno = c.ExecuteScalar();
            if (dueno == null) return Resultado.Mal("Reserva no encontrada o ya cancelada");
            if (ses.Rol != "admin" && (long)dueno != ses.IdUsuario)
                return Resultado.Mal("Solo puedes modificar tus propias reservas");

            using var u = con.CreateCommand();
            u.CommandText = @"
                UPDATE Reserva
                   SET fecha         = COALESCE(@f, fecha),
                       turno         = COALESCE(@t, turno),
                       numComensales = COALESCE(@n, numComensales),
                       nombreCliente = COALESCE(@c, nombreCliente),
                       observaciones = COALESCE(@o, observaciones)
                 WHERE id = @id";
            var fecha = StrOpt(d, "fecha");
            u.Parameters.AddWithValue("@f", fecha != null && fecha != ""
                ? (object)fecha : DBNull.Value);
            var turno = Str(d, "turno");
            u.Parameters.AddWithValue("@t", (turno == "bazkaria" || turno == "afaria")
                ? (object)turno : DBNull.Value);
            var n = Int(d, "numComensales");
            u.Parameters.AddWithValue("@n", n > 0 ? (object)n : DBNull.Value);
            u.Parameters.AddWithValue("@c", (object?)StrOpt(d, "nombreCliente") ?? DBNull.Value);
            u.Parameters.AddWithValue("@o", (object?)StrOpt(d, "observaciones") ?? DBNull.Value);
            u.Parameters.AddWithValue("@id", id);
            u.ExecuteNonQuery();
            return Resultado.Bien(new { mensaje = "Reserva modificada" });
        });
    }

    private static Resultado Res_Borrar(SesionUsuario ses, JsonElement d)
    {
        var id = Int(d, "idReserva");
        if (id <= 0) return Resultado.Mal("Falta el id de la reserva");

        return Escritura(con =>
        {
            using var c = con.CreateCommand();
            c.CommandText = @"SELECT idUsuario FROM Reserva WHERE id = @i AND estado = 'activa'";
            c.Parameters.AddWithValue("@i", id);
            var dueno = c.ExecuteScalar();
            if (dueno == null) return Resultado.Mal("Reserva no encontrada o ya cancelada");
            if (ses.Rol != "admin" && (long)dueno != ses.IdUsuario)
                return Resultado.Mal("Solo puedes borrar tus propias reservas");

            using var u = con.CreateCommand();
            u.CommandText = "UPDATE Reserva SET estado = 'cancelada' WHERE id = @i";
            u.Parameters.AddWithValue("@i", id);
            u.ExecuteNonQuery();
            return Resultado.Bien(new { mensaje = "Reserva cancelada" });
        });
    }

    private static Resultado Res_Lista(JsonElement d)
    {
        using var con = BaseDatos.Conectar();
        con.Open();
        using var cmd = con.CreateCommand();
        var fecha = StrOpt(d, "fecha");
        var turno = Str(d, "turno");
        cmd.CommandText = @"
            SELECT r.id, m.numMesa, r.fecha, r.turno, r.numComensales,
                   r.nombreCliente, r.observaciones, r.estado
              FROM Reserva r
              JOIN Mesa m ON m.id = r.idMesa
             WHERE r.estado = 'activa'
               AND (@f IS NULL OR r.fecha = @f)
               AND (@t IS NULL OR r.turno = @t)
             ORDER BY m.numMesa, r.turno";
        cmd.Parameters.AddWithValue("@f", string.IsNullOrEmpty(fecha)
            ? DBNull.Value : (object)fecha);
        cmd.Parameters.AddWithValue("@t", (turno == "bazkaria" || turno == "afaria")
            ? (object)turno : DBNull.Value);
        var lista = new List<object>();
        using var r = cmd.ExecuteReader();
        while (r.Read())
            lista.Add(new { idReserva = r.GetInt32(0), numMesa = r.GetInt32(1),
                fecha = r.IsDBNull(2) ? "sin fecha" : r.GetString(2),
                turno = r.GetString(3), numComensales = r.GetInt32(4),
                nombreCliente = r.GetString(5),
                observaciones = r.IsDBNull(6) ? "" : r.GetString(6),
                estado = r.GetString(7) });
        return Resultado.Bien(new { reservas = lista });
    }

    // =====================================================
    //    PEDIDOS / TICKET (requisito 4 y stock)
    // =====================================================

    private static Resultado Ped_Abrir(SesionUsuario ses)
    {
        if (HayPedidoAbierto(ses.IdUsuario, out var yaAbierto))
            return Resultado.Mal($"Ya tienes el pedido {yaAbierto} abierto");

        return Escritura(con =>
        {
            using var cmd = con.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Pedido (idUsuario, fechaHora, estado, total)
                VALUES (@u, @f, 'abierta', 0)
                RETURNING id";
            cmd.Parameters.AddWithValue("@u", ses.IdUsuario);
            cmd.Parameters.AddWithValue("@f", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            var id = Convert.ToInt32(cmd.ExecuteScalar());
            return Resultado.Bien(new { idPedido = id });
        });
    }

    private static Resultado Ped_AgregarLinea(SesionUsuario ses, JsonElement d)
    {
        var idPedido = Int(d, "idPedido");
        var idProducto = Int(d, "idProducto");
        var cantidad = Int(d, "cantidad");
        if (idPedido <= 0 || idProducto <= 0 || cantidad <= 0)
            return Resultado.Mal("idPedido, idProducto y cantidad (> 0) son obligatorios");

        return Escritura(con =>
        {
            using var trans = con.BeginTransaction();

            // El pedido debe ser del usuario y estar abierto
            using (var c = con.CreateCommand())
            {
                c.Transaction = trans;
                c.CommandText = @"SELECT idUsuario FROM Pedido
                                   WHERE id = @p AND estado = 'abierta'";
                c.Parameters.AddWithValue("@p", idPedido);
                var dueno = c.ExecuteScalar();
                if (dueno == null)
                {
                    trans.Rollback();
                    return Resultado.Mal("Pedido no encontrado o ya cobrado");
                }
                if (ses.Rol != "admin" && (long)dueno != ses.IdUsuario)
                {
                    trans.Rollback();
                    return Resultado.Mal("Ese pedido no es tuyo");
                }
            }

            // Producto con stock suficiente (sumando lo ya metido en el pedido)
            double precio;
            using (var c2 = con.CreateCommand())
            {
                c2.Transaction = trans;
                c2.CommandText = @"
                    SELECT precio, stock, activo,
                           IFNULL((SELECT SUM(l.cantidad) FROM LineaPedido l
                                    WHERE l.idPedido = @p
                                      AND l.idProducto = pr.id), 0)
                      FROM Producto pr
                     WHERE pr.id = @pr";
                c2.Parameters.AddWithValue("@p", idPedido);
                c2.Parameters.AddWithValue("@pr", idProducto);
                using var r2 = c2.ExecuteReader();
                if (!r2.Read())
                {
                    trans.Rollback();
                    return Resultado.Mal("Producto no encontrado");
                }
                if (r2.GetInt32(2) != 1)
                {
                    trans.Rollback();
                    return Resultado.Mal("Producto no disponible");
                }
                precio = r2.GetDouble(0);
                var stock = r2.GetInt32(1);
                var yaEnPedido = r2.GetInt64(3);
                if (yaEnPedido + cantidad > stock)
                {
                    trans.Rollback();
                    return Resultado.Mal($"Stock insuficiente: quedan {stock} unidades de ese producto");
                }
            }

            using (var ins = con.CreateCommand())
            {
                ins.Transaction = trans;
                ins.CommandText = @"
                    INSERT INTO LineaPedido (idPedido, idProducto, cantidad, precioUnitario)
                    VALUES (@p, @pr, @c, @pu)";
                ins.Parameters.AddWithValue("@p", idPedido);
                ins.Parameters.AddWithValue("@pr", idProducto);
                ins.Parameters.AddWithValue("@c", cantidad);
                ins.Parameters.AddWithValue("@pu", precio);
                ins.ExecuteNonQuery();
            }

            using (var upd = con.CreateCommand())
            {
                upd.Transaction = trans;
                upd.CommandText = @"
                    UPDATE Pedido
                       SET total = (SELECT IFNULL(SUM(precioUnitario * cantidad), 0)
                                      FROM LineaPedido WHERE idPedido = @p)
                     WHERE id = @p";
                upd.Parameters.AddWithValue("@p", idPedido);
                upd.ExecuteNonQuery();
            }

            trans.Commit();
            return Resultado.Bien(new { mensaje = "Linea anadida", precioUnitario = precio });
        });
    }

    // Confirmar compra: estado cobrada + DECREMENTO DE STOCK en el mismo lock
    private static Resultado Ped_Confirmar(SesionUsuario ses, JsonElement d)
    {
        var idPedido = Int(d, "idPedido");
        if (idPedido <= 0) return Resultado.Mal("Falta el id del pedido");

        return Escritura(con =>
        {
            using var trans = con.BeginTransaction();

            using (var c = con.CreateCommand())
            {
                c.Transaction = trans;
                c.CommandText = @"SELECT idUsuario, total, estado FROM Pedido WHERE id = @p";
                c.Parameters.AddWithValue("@p", idPedido);
                using var r = c.ExecuteReader();
                if (!r.Read())
                {
                    trans.Rollback();
                    return Resultado.Mal("Pedido no encontrado");
                }
                if (ses.Rol != "admin" && (long)r.GetInt64(0) != ses.IdUsuario)
                {
                    trans.Rollback();
                    return Resultado.Mal("Ese pedido no es tuyo");
                }
                if (r.GetString(2) != "abierta")
                {
                    trans.Rollback();
                    return Resultado.Mal("El pedido ya no esta abierto");
                }
            }

            // lineas agregadas por producto
            var lineas = new List<(int prod, int cant)>();
            using (var c2 = con.CreateCommand())
            {
                c2.Transaction = trans;
                c2.CommandText = @"
                    SELECT idProducto, SUM(cantidad)
                      FROM LineaPedido WHERE idPedido = @p
                     GROUP BY idProducto";
                c2.Parameters.AddWithValue("@p", idPedido);
                using var r2 = c2.ExecuteReader();
                while (r2.Read()) lineas.Add((r2.GetInt32(0), r2.GetInt32(1)));
            }
            if (lineas.Count == 0)
            {
                trans.Rollback();
                return Resultado.Mal("El pedido esta vacio");
            }

            // verificar stock disponible de una vez
            foreach (var (prod, cant) in lineas)
            {
                using var c3 = con.CreateCommand();
                c3.Transaction = trans;
                c3.CommandText = "SELECT stock FROM Producto WHERE id = @i";
                c3.Parameters.AddWithValue("@i", prod);
                var stock = Convert.ToInt64(c3.ExecuteScalar());
                if (stock < cant)
                {
                    trans.Rollback();
                    return Resultado.Mal($"Stock insuficiente del producto {prod}: quedan {stock}");
                }
            }

            // decremento de stock (requisito 4: actualizacion automatica)
            foreach (var (prod, cant) in lineas)
            {
                using var u = con.CreateCommand();
                u.Transaction = trans;
                u.CommandText = @"UPDATE Producto SET stock = stock - @c WHERE id = @i";
                u.Parameters.AddWithValue("@c", cant);
                u.Parameters.AddWithValue("@i", prod);
                u.ExecuteNonQuery();
            }

            double total;
            using (var u2 = con.CreateCommand())
            {
                u2.Transaction = trans;
                u2.CommandText = @"UPDATE Pedido SET estado = 'cobrada' WHERE id = @p";
                u2.Parameters.AddWithValue("@p", idPedido);
                u2.ExecuteNonQuery();
                using var c4 = con.CreateCommand();
                c4.Transaction = trans;
                c4.CommandText = "SELECT total FROM Pedido WHERE id = @p";
                c4.Parameters.AddWithValue("@p", idPedido);
                total = Convert.ToDouble(c4.ExecuteScalar());
            }

            trans.Commit();
            return Resultado.Bien(new
            {
                mensaje = "Compra confirmada y stock actualizado",
                idPedido, total,
                cronologia = lineas.Select(l => new { producto = l.prod, cantidad = l.cant })
            });
        });
    }

    private static Resultado Ped_Cancelar(SesionUsuario ses, JsonElement d)
    {
        var idPedido = Int(d, "idPedido");
        if (idPedido <= 0) return Resultado.Mal("Falta el id del pedido");

        return Escritura(con =>
        {
            using var c = con.CreateCommand();
            c.CommandText = @"SELECT idUsuario, estado FROM Pedido WHERE id = @p";
            c.Parameters.AddWithValue("@p", idPedido);
            using var r = c.ExecuteReader();
            if (!r.Read()) return Resultado.Mal("Pedido no encontrado");
            if (ses.Rol != "admin" && r.GetInt64(0) != ses.IdUsuario)
                return Resultado.Mal("Ese pedido no es tuyo");
            if (r.GetString(1) != "abierta") return Resultado.Mal("El pedido ya no esta abierto");

            using var u = con.CreateCommand();
            u.CommandText = "UPDATE Pedido SET estado = 'anulada' WHERE id = @p";
            u.Parameters.AddWithValue("@p", idPedido);
            u.ExecuteNonQuery();
            return Resultado.Bien(new { mensaje = "Pedido anulado" });
        });
    }

    private static Resultado Ped_Ticket(JsonElement d)
    {
        var idPedido = Int(d, "idPedido");
        if (idPedido <= 0) return Resultado.Mal("Falta el id del pedido");

        using var con = BaseDatos.Conectar();
        con.Open();
        string? cliente = null; string fecha = ""; string estado = ""; double total = 0;
        using (var c = con.CreateCommand())
        {
            c.CommandText = @"SELECT u.nombre, p.fechaHora, p.estado, p.total
                                FROM Pedido p JOIN Usuario u ON u.id = p.idUsuario
                               WHERE p.id = @p";
            c.Parameters.AddWithValue("@p", idPedido);
            using var r = c.ExecuteReader();
            if (!r.Read()) return Resultado.Mal("Pedido no encontrado");
            cliente = r.GetString(0); fecha = r.GetString(1);
            estado = r.GetString(2); total = r.GetDouble(3);
        }

        var lineas = new List<object>();
        using (var c2 = con.CreateCommand())
        {
            c2.CommandText = @"
                SELECT pr.nombre, l.cantidad, l.precioUnitario
                  FROM LineaPedido l JOIN Producto pr ON pr.id = l.idProducto
                 WHERE l.idPedido = @p ORDER BY l.id";
            c2.Parameters.AddWithValue("@p", idPedido);
            using var r2 = c2.ExecuteReader();
            while (r2.Read())
                lineas.Add(new { nombre = r2.GetString(0), cantidad = r2.GetInt32(1),
                    precioUnitario = Math.Round(r2.GetDouble(2), 2) });
        }

        return Resultado.Bien(new
        {
            ticket = new { idPedido, cliente, fecha, estado, total,
                lineas, iva = "10% incluido" }
        });
    }

    // =====================================================
    //    UTILIDADES
    // =====================================================

    private static bool HayPedidoAbierto(int idUsuario, out int idPedido)
    {
        using var con = BaseDatos.Conectar();
        con.Open();
        using var cmd = con.CreateCommand();
        cmd.CommandText = @"SELECT id FROM Pedido
                             WHERE idUsuario = @u AND estado = 'abierta'";
        cmd.Parameters.AddWithValue("@u", idUsuario);
        var res = cmd.ExecuteScalar();
        idPedido = res == null ? 0 : Convert.ToInt32(res);
        return res != null;
    }

    // TODA escritura de negocio pasa por aqui: ONE LOCK + transaccion
    private static Resultado Escritura(Func<SqliteConnection, Resultado> bloque)
    {
        lock (BaseDatos.LockEscritura)
        {
            using var con = BaseDatos.Conectar();
            con.Open();
            return bloque(con);
        }
    }

}
