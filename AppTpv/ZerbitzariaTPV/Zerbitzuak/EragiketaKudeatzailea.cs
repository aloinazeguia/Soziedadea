// ============================================================
//  AppTpv — Eragiketa kudeatzailea (negozio-logika)
//
//  Protokoloko ekintza bakoitza (docs/PROTOCOLO.md) hona iristen da.
//  Arauak:
//   * BDko IDAZKETAK DatuBasea.IdazketaBlokeoa barruan egiten dira
//     (lock global bakarra, enuntziatuak baimentzen duen bezala).
//   * Rola hemen egiaztatzen da: 'admin' bakarrik aritu daiteke
//     erabiltzaileen eta izakinen CRUD-a egiten.
//   * Erosketak (PEDIDO_CONFIRMAR) izakinak balioztatu eta murrizten ditu
//     ATAL KRITIKO ETA TRANSABERA berean.
// ============================================================
using System.Text.Json;
using Microsoft.Data.Sqlite;
using ZerbitzariaTPV.Datuak;

namespace ZerbitzariaTPV.Zerbitzuak;

public static class EragiketaKudeatzailea
{
    public static Emaitza Exekutatu(ErabiltzaileSaioa? saioa, string ekintza, JsonElement datuak)
    {
        try
        {
            return ekintza switch
            {
                // ---- Saioa (ez du tokenik behar) ----
                "LOGIN" => SaioaHasi(datuak),

                // ---- Karta / izakinak (aldatzeko admin soilik) ----
                "PRODUCTOS_LISTA"     => ProduktuakZerrenda(),
                "PRODUCTO_AGREGAR"    => AdminEskatzenDu(saioa, () => Pro_Gehitu(datuak)),
                "PRODUCTO_MODIFICAR"  => AdminEskatzenDu(saioa, () => Pro_Aldatu(datuak)),
                "PRODUCTO_BORRAR"     => AdminEskatzenDu(saioa, () => Pro_Ezabatu(datuak)),
                "STOCK_MODIFICAR"     => AdminEskatzenDu(saioa, () => Izakinak_Aldatu(datuak)),

                // ---- Erabiltzaileak (admin soilik) ----
                "USUARIO_CREAR"       => AdminEskatzenDu(saioa, () => Erab_Sortu(datuak)),
                "USUARIO_MODIFICAR"   => AdminEskatzenDu(saioa, () => Erab_Aldatu(datuak)),
                "USUARIO_BORRAR"      => AdminEskatzenDu(saioa, () => Erab_Ezabatu(datuak)),
                "USUARIOS_LISTA"      => AdminEskatzenDu(saioa, () => Erab_Zerrenda()),

                // ---- Mahaiak eta erreserbak (erabiltzaile arrunta) ----
                "MESAS_LISTA"         => SaioaEskatzenDu(saioa, () => Mahaiak_Zerrenda()),
                "RESERVA_CREAR"       => SaioaEskatzenDu(saioa, () => Erres_Sortu(saioa!, datuak)),
                "RESERVA_MODIFICAR"   => SaioaEskatzenDu(saioa, () => Erres_Aldatu(saioa!, datuak)),
                "RESERVA_BORRAR"      => SaioaEskatzenDu(saioa, () => Erres_Ezabatu(saioa!, datuak)),
                "RESERVA_LISTA"       => SaioaEskatzenDu(saioa, () => Erres_Zerrenda(datuak)),

                // ---- Kontsumizioak / txartela ----
                "PEDIDO_ABRIR"        => SaioaEskatzenDu(saioa, () => Eska_Irekia(saioa!)),
                "PEDIDO_AGREGAR_LINEA"=> SaioaEskatzenDu(saioa, () => Eska_LerroGehitu(saioa!, datuak)),
                "PEDIDO_CONFIRMAR"    => SaioaEskatzenDu(saioa, () => Eska_Baieztatu(saioa!, datuak)),
                "PEDIDO_CANCELAR"     => SaioaEskatzenDu(saioa, () => Eska_BertanBehera(saioa!, datuak)),
                "PEDIDO_REKUPERATU"   => SaioaEskatzenDu(saioa, () => Eska_Berreskuratu(saioa!, datuak)),
                "PEDIDO_MAHAIA_ESLEITU" => SaioaEskatzenDu(saioa, () => Eska_MahaiEsleitu(saioa!, datuak)),
                "TICKET_OBTENER"      => SaioaEskatzenDu(saioa, () => Eska_Txartela(saioa!, datuak)),

                _ => Emaitza.Txar("Eskatutako ekintza ez da ezagutzen")
            };
        }
        catch (BaimenikEzekoSalbuespena ex)
        {
            return Emaitza.Txar(ex.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] {ekintza}: {ex.Message}");
            return Emaitza.Txar("Errorea gertatu da eskaera prozesatzean");
        }
    }

    // ---------- Guardas de rola ----------

    private sealed class BaimenikEzekoSalbuespena : Exception
    {
        public BaimenikEzekoSalbuespena(string msg) : base(msg) { }
    }

    private static Emaitza AdminEskatzenDu(ErabiltzaileSaioa? s, Func<Emaitza> ekintza)
    {
        if (s == null) throw new BaimenikEzekoSalbuespena("Ez dago saio aktiborik (tokena falta da)");
        if (s.Rola != "admin") throw new BaimenikEzekoSalbuespena("Ekintza hau administratzaileentzat bakarrik dago baimenduta");
        return ekintza();
    }

    private static Emaitza SaioaEskatzenDu(ErabiltzaileSaioa? s, Func<Emaitza> ekintza)
    {
        if (s == null) throw new BaimenikEzekoSalbuespena("Ez dago saio aktiborik (tokena falta da)");
        return ekintza();
    }

    // ---------- Lecturas auxiliares de "datuak" ----------

    private static string Katea(JsonElement d, string k)
        => d.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString() ?? "" : "";

    private static int Zenbakia(JsonElement d, string k)
        => d.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.Number
            ? v.GetInt32() : 0;

    private static double ZenbakiD(JsonElement d, string k)
        => d.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.Number
            ? v.GetDouble() : 0;

    private static string? KateaAukerakoa(JsonElement d, string k)
        => d.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString() : null;

    private static bool Badu(JsonElement d, string k) => d.TryGetProperty(k, out _);

    // =====================================================
    //                 SESION
    // =====================================================

    private static Emaitza SaioaHasi(JsonElement d)
    {
        var erabiltzailea = Katea(d, "erabiltzailea");
        var pasahitza  = Katea(d, "pasahitza");
        if (erabiltzailea == "" || pasahitza == "")
            return Emaitza.Txar("Erabiltzaile-izena edo pasahitza falta da");

        using var konexioa = DatuBasea.Konektatu();
        konexioa.Open();
        using var agindua = konexioa.CreateCommand();
        agindua.CommandText = @"SELECT id, passHash, nombre, rol FROM Usuario
                             WHERE login = @l AND activo = 1";
        agindua.Parameters.AddWithValue("@l", erabiltzailea);
        using var r = agindua.ExecuteReader();
        if (!r.Read())
            return Emaitza.Txar("Erabiltzaile-izena edo pasahitza ez dira zuzenak");

        var hash = Segurtasuna.PasahitzaHash(pasahitza);
        if (r.GetString(1) != hash)
            return Emaitza.Txar("Erabiltzaile-izena edo pasahitza ez dira zuzenak");

        var saioa = new ErabiltzaileSaioa(r.GetInt32(0), erabiltzailea, r.GetString(2), r.GetString(3));
        var token = Saioak.Sortu(saioa);
        Console.WriteLine($"[SESION] {erabiltzailea} ({saioa.Rola}) - saioak: {Saioak.Guztira}");
        return Emaitza.Ondo(new { token, rola = saioa.Rola, izena = saioa.Izena });
    }

    // =====================================================
    //      KARTA / IZAKINAK (aldaketak admin soilik)
    // =====================================================

    private static Emaitza ProduktuakZerrenda()
    {
        using var konexioa = DatuBasea.Konektatu();
        konexioa.Open();
        using var agindua = konexioa.CreateCommand();
        agindua.CommandText = @"SELECT id, nombre, precio, stock, ingredientes, categoria, activo
                              FROM Producto ORDER BY categoria, nombre";
        var zerrenda = new List<object>();
        using var r = agindua.ExecuteReader();
        while (r.Read())
            zerrenda.Add(new { id = r.GetInt32(0), izena = r.GetString(1),
                prezioa = r.GetDouble(2), izakinak = r.GetInt32(3),
                osagaiak = r.IsDBNull(4) ? "" : r.GetString(4),
                kategoria = r.GetString(5), aktibo = r.GetInt32(6) });
        return Emaitza.Ondo(new { produktuak = zerrenda });
    }

    private static Emaitza Pro_Gehitu(JsonElement d)
    {
        var izena = Katea(d, "izena");
        var prezioa = ZenbakiD(d, "prezioa");
        var izakinak  = Zenbakia(d, "izakinak");
        if (izena == "" || prezioa <= 0)
            return Emaitza.Txar("Izena eta prezioa nahitaezkoak dira (prezioa > 0)");

        return Idazketa(konexioa =>
        {
            using var agindua = konexioa.CreateCommand();
            agindua.CommandText = @"
                INSERT INTO Producto (nombre, precio, stock, ingredientes, categoria)
                VALUES (@n, @p, @s, @i, @c)";
            agindua.Parameters.AddWithValue("@n", izena);
            agindua.Parameters.AddWithValue("@p", prezioa);
            agindua.Parameters.AddWithValue("@s", izakinak);
            agindua.Parameters.AddWithValue("@i", (object?)KateaAukerakoa(d, "osagaiak") ?? DBNull.Value);
            agindua.Parameters.AddWithValue("@c", Katea(d, "kategoria") is "" ? "general" : Katea(d, "kategoria"));
            var lerroak = agindua.ExecuteNonQuery();
            return lerroak == 0
                ? Emaitza.Txar("Izen hori duen produktua badago lehendik")
                : Emaitza.Ondo(new { mezua = "Producto creado" });
        });
    }

    private static Emaitza Pro_Aldatu(JsonElement d)
    {
        var id = Zenbakia(d, "id");
        if (id <= 0) return Emaitza.Txar("Produktuaren IDa falta da");

        return Idazketa(konexioa =>
        {
            using var agindua = konexioa.CreateCommand();
            agindua.CommandText = @"
                UPDATE Producto
                   SET nombre      = COALESCE(@n, nombre),
                       precio      = CASE WHEN @p2 > 0 THEN @p ELSE precio END,
                       stock       = CASE WHEN @tieneS = 1 THEN @s ELSE stock END,
                       ingredientes= COALESCE(@i, ingredientes),
                       categoria   = COALESCE(@c, categoria),
                       activo      = COALESCE(@a, activo)
                 WHERE id = @id";
            agindua.Parameters.AddWithValue("@n", (object?)KateaAukerakoa(d, "izena") ?? DBNull.Value);
            agindua.Parameters.AddWithValue("@p", ZenbakiD(d, "prezioa"));
            agindua.Parameters.AddWithValue("@p2", ZenbakiD(d, "prezioa"));
            agindua.Parameters.AddWithValue("@tieneS", Badu(d, "izakinak") ? 1 : 0);
            agindua.Parameters.AddWithValue("@s", Zenbakia(d, "izakinak"));
            agindua.Parameters.AddWithValue("@i", (object?)KateaAukerakoa(d, "osagaiak") ?? DBNull.Value);
            agindua.Parameters.AddWithValue("@c", (object?)KateaAukerakoa(d, "kategoria") ?? DBNull.Value);
            agindua.Parameters.AddWithValue("@a", Badu(d, "aktibo") ? (object)Zenbakia(d, "aktibo") : DBNull.Value);
            agindua.Parameters.AddWithValue("@id", id);
            return agindua.ExecuteNonQuery() == 0
                ? Emaitza.Txar("Ez da produktua aurkitu")
                : Emaitza.Ondo(new { mezua = "Producto actualizado" });
        });
    }

    private static Emaitza Pro_Ezabatu(JsonElement d)
    {
        var id = Zenbakia(d, "id");
        if (id <= 0) return Emaitza.Txar("Produktuaren IDa falta da");

        // Ezabaketa LOGIKOA: produktua eskaeren lerroetan agertzen bada,
        // desgaitu egiten dugu txartelaren historia ez apurtzeko.
        return Idazketa(konexioa =>
        {
            using var agindua = konexioa.CreateCommand();
            agindua.CommandText = @"
                UPDATE Producto SET activo = 0 WHERE id = @id";
            agindua.Parameters.AddWithValue("@id", id);
            return agindua.ExecuteNonQuery() == 0
                ? Emaitza.Txar("Ez da produktua aurkitu")
                : Emaitza.Ondo(new { mezua = "Producto eliminado (desactivado)" });
        });
    }

    private static Emaitza Izakinak_Aldatu(JsonElement d)
    {
        var id = Zenbakia(d, "id");
        var izakinak = Zenbakia(d, "izakinak");
        if (id <= 0 || izakinak < 0)
            return Emaitza.Txar("IDa ez da baliozkoa edo izakin-kopurua negatiboa da");

        return Idazketa(konexioa =>
        {
            using var agindua = konexioa.CreateCommand();
            agindua.CommandText = "UPDATE Producto SET stock = @s WHERE id = @id";
            agindua.Parameters.AddWithValue("@s", izakinak);
            agindua.Parameters.AddWithValue("@id", id);
            return agindua.ExecuteNonQuery() == 0
                ? Emaitza.Txar("Ez da produktua aurkitu")
                : Emaitza.Ondo(new { mezua = $"Stock actualizado a {izakinak}" });
        });
    }

    // =====================================================
    //    ERABILTZAILEAK (admin soilik, 1.2 eta 2. eskakizuna)
    // =====================================================

    private static Emaitza Erab_Sortu(JsonElement d)
    {
        var erabiltzailea = Katea(d, "erabiltzailea");
        var pasahitza  = Katea(d, "pasahitza");
        var izena = Katea(d, "izena");
        var rola   = Katea(d, "rola");
        if (erabiltzailea == "" || pasahitza == "" || izena == "")
            return Emaitza.Txar("Erabiltzaile-izena, pasahitza eta izena nahitaezkoak dira");
        if (rola != "admin" && rola != "usuario")
            return Emaitza.Txar("Aukeratutako rola ez da baliozkoa");

        return Idazketa(konexioa =>
        {
            using var agindua = konexioa.CreateCommand();
            agindua.CommandText = @"
                INSERT INTO Usuario (login, passHash, nombre, rol)
                VALUES (@l, @h, @n, @r)";
            agindua.Parameters.AddWithValue("@l", erabiltzailea);
            agindua.Parameters.AddWithValue("@h", Segurtasuna.PasahitzaHash(pasahitza));
            agindua.Parameters.AddWithValue("@n", izena);
            agindua.Parameters.AddWithValue("@r", rola);
            var lerroak = agindua.ExecuteNonQuery();
            return lerroak == 0
                ? Emaitza.Txar("Erabiltzaile-izen hori badago lehendik")
                : Emaitza.Ondo(new { mezua = $"Usuario {erabiltzailea} creado" });
        });
    }

    private static Emaitza Erab_Aldatu(JsonElement d)
    {
        var id = Zenbakia(d, "id");
        if (id <= 0) return Emaitza.Txar("Erabiltzailearen IDa falta da");

        return Idazketa(konexioa =>
        {
            using var agindua = konexioa.CreateCommand();
            agindua.CommandText = @"
                UPDATE Usuario
                   SET nombre = COALESCE(@n, nombre),
                       passHash = COALESCE(@h, passHash),
                       rol = COALESCE(@r, rol),
                       activo = COALESCE(@a, activo)
                 WHERE id = @id";
            agindua.Parameters.AddWithValue("@n", (object?)KateaAukerakoa(d, "izena") ?? DBNull.Value);
            agindua.Parameters.AddWithValue("@h", Katea(d, "pasahitza") == ""
                ? DBNull.Value : (object)Segurtasuna.PasahitzaHash(Katea(d, "pasahitza")));
            var rola = Katea(d, "rola");
            agindua.Parameters.AddWithValue("@r", (rola == "admin" || rola == "usuario")
                ? (object)rola : DBNull.Value);
            agindua.Parameters.AddWithValue("@a", Badu(d, "aktibo")
                ? (object)Zenbakia(d, "aktibo") : DBNull.Value);
            agindua.Parameters.AddWithValue("@id", id);
            return agindua.ExecuteNonQuery() == 0
                ? Emaitza.Txar("Ez da erabiltzailea aurkitu")
                : Emaitza.Ondo(new { mezua = "Usuario actualizado" });
        });
    }

    private static Emaitza Erab_Ezabatu(JsonElement d)
    {
        var id = Zenbakia(d, "id");
        if (id <= 0) return Emaitza.Txar("Erabiltzailearen IDa falta da");

        return Idazketa(konexioa =>
        {
            using var agindua = konexioa.CreateCommand();
            agindua.CommandText = "UPDATE Usuario SET activo = 0 WHERE id = @id";
            agindua.Parameters.AddWithValue("@id", id);
            return agindua.ExecuteNonQuery() == 0
                ? Emaitza.Txar("Ez da erabiltzailea aurkitu")
                : Emaitza.Ondo(new { mezua = "Usuario deshabilitado" });
        });
    }

    private static Emaitza Erab_Zerrenda()
    {
        using var konexioa = DatuBasea.Konektatu();
        konexioa.Open();
        using var agindua = konexioa.CreateCommand();
        agindua.CommandText = "SELECT id, login, nombre, rol, activo FROM Usuario ORDER BY id";
        var zerrenda = new List<object>();
        using var r = agindua.ExecuteReader();
        while (r.Read())
            zerrenda.Add(new { id = r.GetInt32(0), erabiltzailea = r.GetString(1),
                izena = r.GetString(2), rola = r.GetString(3), aktibo = r.GetInt32(4) });
        return Emaitza.Ondo(new { erabiltzaileak = zerrenda });
    }

    // =====================================================
    //    MESAS Y RESERVAS (requisito 1.5 y 4)
    // =====================================================

    private static Emaitza Mahaiak_Zerrenda()
    {
        using var konexioa = DatuBasea.Konektatu();
        konexioa.Open();
        using var agindua = konexioa.CreateCommand();
        // Egoera: eskaera irekia badago -> okupatuta; bestela gaurko/ data gabe erreserba badago
        // -> erreserbatuta; bestela librea. id, mahaiZenbakia eta edukiera mantentzen dira.
        agindua.CommandText = @"
            SELECT m.id, m.numMesa, m.capacidad,
                   (SELECT COUNT(*) FROM Reserva r
                     WHERE r.idMesa = m.id AND r.estado = 'activa'
                       AND (r.fecha IS NULL OR r.fecha = date('now'))) AS erreserbak,
                   (SELECT p.id FROM Pedido p
                     WHERE p.idMesa = m.id AND p.estado = 'abierta'
                     ORDER BY p.id DESC LIMIT 1) AS eskaeraId
              FROM Mesa m ORDER BY m.numMesa";
        var zerrenda = new List<object>();
        using var r = agindua.ExecuteReader();
        while (r.Read())
        {
            // 0=id, 1=numMesa, 2=capacidad, 3=erreserbak, 4=eskaeraId
            var erreserbak = Convert.ToInt32(r.GetValue(3));
            var eskaeraId = r.IsDBNull(4) ? (int?)null : Convert.ToInt32(r.GetValue(4));
            var egoera = eskaeraId != null ? "okupatuta"
                       : erreserbak > 0 ? "erreserbatuta"
                       : "librea";
            zerrenda.Add(new
            {
                id = r.GetInt32(0),
                mahaiZenbakia = r.GetInt32(1),
                edukiera = r.GetInt32(2),
                erreserbak,
                eskaeraId,
                egoera
            });
        }
        return Emaitza.Ondo(new { mahaiak = zerrenda });
    }

    private static Emaitza Erres_Sortu(ErabiltzaileSaioa saioa, JsonElement d)
    {
        var mahaiId = Zenbakia(d, "mahaiId");
        var txanda = Katea(d, "txanda");
        var pertsonak = Zenbakia(d, "pertsonaKopurua");
        var data = KateaAukerakoa(d, "data");           // hutsik etor daiteke: data gabeko erreserba
        var izena = Katea(d, "bezeroIzena");
        var oharrak = KateaAukerakoa(d, "oharrak");

        if (mahaiId <= 0 || pertsonak <= 0)
            return Emaitza.Txar("Mahaia falta da edo pertsona kopurua ez da baliozkoa");
        if (txanda != "bazkaria" && txanda != "afaria")
            return Emaitza.Txar("Txandak 'bazkaria' edo 'afaria' izan behar du");
        if (izena == "")
            return Emaitza.Txar("Bezeroaren izena falta da");
        DateTime? f = null;
        if (!string.IsNullOrEmpty(data))
        {
            if (!DateTime.TryParseExact(data, "yyyy-MM-dd", null,
                    System.Globalization.DateTimeStyles.None, out var parseatua))
                return Emaitza.Txar("Datak YYYY-MM-DD formatua izan behar du (edo hutsik egon)");
            f = parseatua;
        }

        return Idazketa(konexioa =>
        {
            using var transakzioa = konexioa.BeginTransaction();

            // edukiera
            using (var c = konexioa.CreateCommand())
            {
                c.Transaction = transakzioa;
                c.CommandText = "SELECT capacidad FROM Mesa WHERE id = @i";
                c.Parameters.AddWithValue("@i", mahaiId);
                var edukiera = c.ExecuteScalar();
                if (edukiera == null)
                {
                    transakzioa.Rollback();
                    return Emaitza.Txar("Mahaia ez da existitzen");
                }
                if (pertsonak > (long)edukiera)
                {
                    transakzioa.Rollback();
                    return Emaitza.Txar($"Mahaiak gehienez {edukiera} lagun onartzen ditu");
                }
            }

            // bikoiztua: mahia + data + txanda, data badago soilik
            if (f != null)
            {
                using var c2 = konexioa.CreateCommand();
                c2.Transaction = transakzioa;
                c2.CommandText = @"
                    SELECT 1 FROM Reserva
                     WHERE idMesa = @m AND fecha = @f AND turno = @t
                       AND estado = 'activa'";
                c2.Parameters.AddWithValue("@m", mahaiId);
                c2.Parameters.AddWithValue("@f", f!.Value.ToString("yyyy-MM-dd"));
                c2.Parameters.AddWithValue("@t", txanda);
                if (c2.ExecuteScalar() != null)
                {
                    transakzioa.Rollback();
                    return Emaitza.Txar($"Mahaia {txanda} txandarako erreserbatuta dago data horretan");
                }
            }

            using var txertaketa = konexioa.CreateCommand();
            txertaketa.Transaction = transakzioa;
            txertaketa.CommandText = @"
                INSERT INTO Reserva (idMesa, fecha, turno, numComensales,
                                     idUsuario, nombreCliente, observaciones, estado)
                VALUES (@m, @f, @t, @n, @u, @c, @o, 'activa')
                RETURNING id";
            txertaketa.Parameters.AddWithValue("@m", mahaiId);
            txertaketa.Parameters.AddWithValue("@f", f == null ? DBNull.Value : (object)f.Value.ToString("yyyy-MM-dd"));
            txertaketa.Parameters.AddWithValue("@t", txanda);
            txertaketa.Parameters.AddWithValue("@n", pertsonak);
            txertaketa.Parameters.AddWithValue("@u", saioa.ErabiltzaileId);
            txertaketa.Parameters.AddWithValue("@c", izena);
            txertaketa.Parameters.AddWithValue("@o", (object?)oharrak ?? DBNull.Value);
            var erreserbaId = Convert.ToInt32(txertaketa.ExecuteScalar());
            transakzioa.Commit();
            return Emaitza.Ondo(new { erreserbaId = erreserbaId,
                mezua = "Reserva creada", data = (object?)data ?? "datarik gabe", txanda });
        });
    }

    private static Emaitza Erres_Aldatu(ErabiltzaileSaioa saioa, JsonElement d)
    {
        var id = Zenbakia(d, "erreserbaId");
        if (id <= 0) return Emaitza.Txar("Erreserbaren IDa falta da");

        return Idazketa(konexioa =>
        {
            using var c = konexioa.CreateCommand();
            c.CommandText = @"SELECT idUsuario FROM Reserva WHERE id = @i AND estado = 'activa'";
            c.Parameters.AddWithValue("@i", id);
            var jabea = c.ExecuteScalar();
            if (jabea == null) return Emaitza.Txar("Ez da erreserba aurkitu, edo dagoeneko bertan behera utzi da");
            if (saioa.Rola != "admin" && (long)jabea != saioa.ErabiltzaileId)
                return Emaitza.Txar("Zure erreserbak bakarrik alda ditzakezu");

            using var u = konexioa.CreateCommand();
            u.CommandText = @"
                UPDATE Reserva
                   SET fecha         = COALESCE(@f, fecha),
                       turno         = COALESCE(@t, turno),
                       numComensales = COALESCE(@n, numComensales),
                       nombreCliente = COALESCE(@c, nombreCliente),
                       observaciones = COALESCE(@o, observaciones)
                 WHERE id = @id";
            var data = KateaAukerakoa(d, "data");
            u.Parameters.AddWithValue("@f", data != null && data != ""
                ? (object)data : DBNull.Value);
            var txanda = Katea(d, "txanda");
            u.Parameters.AddWithValue("@t", (txanda == "bazkaria" || txanda == "afaria")
                ? (object)txanda : DBNull.Value);
            var n = Zenbakia(d, "pertsonaKopurua");
            u.Parameters.AddWithValue("@n", n > 0 ? (object)n : DBNull.Value);
            u.Parameters.AddWithValue("@c", (object?)KateaAukerakoa(d, "bezeroIzena") ?? DBNull.Value);
            u.Parameters.AddWithValue("@o", (object?)KateaAukerakoa(d, "oharrak") ?? DBNull.Value);
            u.Parameters.AddWithValue("@id", id);
            u.ExecuteNonQuery();
            return Emaitza.Ondo(new { mezua = "Reserva modificada" });
        });
    }

    private static Emaitza Erres_Ezabatu(ErabiltzaileSaioa saioa, JsonElement d)
    {
        var id = Zenbakia(d, "erreserbaId");
        if (id <= 0) return Emaitza.Txar("Erreserbaren IDa falta da");

        return Idazketa(konexioa =>
        {
            using var c = konexioa.CreateCommand();
            c.CommandText = @"SELECT idUsuario FROM Reserva WHERE id = @i AND estado = 'activa'";
            c.Parameters.AddWithValue("@i", id);
            var jabea = c.ExecuteScalar();
            if (jabea == null) return Emaitza.Txar("Ez da erreserba aurkitu, edo dagoeneko bertan behera utzi da");
            if (saioa.Rola != "admin" && (long)jabea != saioa.ErabiltzaileId)
                return Emaitza.Txar("Zure erreserbak bakarrik ezaba ditzakezu");

            using var u = konexioa.CreateCommand();
            u.CommandText = "UPDATE Reserva SET estado = 'cancelada' WHERE id = @i";
            u.Parameters.AddWithValue("@i", id);
            u.ExecuteNonQuery();
            return Emaitza.Ondo(new { mezua = "Reserva cancelada" });
        });
    }

    private static Emaitza Erres_Zerrenda(JsonElement d)
    {
        using var konexioa = DatuBasea.Konektatu();
        konexioa.Open();
        using var agindua = konexioa.CreateCommand();
        var data = KateaAukerakoa(d, "data");
        var txanda = Katea(d, "txanda");
        agindua.CommandText = @"
            SELECT r.id, m.numMesa, r.fecha, r.turno, r.numComensales,
                   r.nombreCliente, r.observaciones, r.estado
              FROM Reserva r
              JOIN Mesa m ON m.id = r.idMesa
             WHERE r.estado = 'activa'
               AND (@f IS NULL OR r.fecha = @f)
               AND (@t IS NULL OR r.turno = @t)
             ORDER BY m.numMesa, r.turno";
        agindua.Parameters.AddWithValue("@f", string.IsNullOrEmpty(data)
            ? DBNull.Value : (object)data);
        agindua.Parameters.AddWithValue("@t", (txanda == "bazkaria" || txanda == "afaria")
            ? (object)txanda : DBNull.Value);
        var zerrenda = new List<object>();
        using var r = agindua.ExecuteReader();
        while (r.Read())
            zerrenda.Add(new { erreserbaId = r.GetInt32(0), mahaiZenbakia = r.GetInt32(1),
                data = r.IsDBNull(2) ? "datarik gabe" : r.GetString(2),
                txanda = r.GetString(3), pertsonaKopurua = r.GetInt32(4),
                bezeroIzena = r.GetString(5),
                oharrak = r.IsDBNull(6) ? "" : r.GetString(6),
                egoera = r.GetString(7) });
        return Emaitza.Ondo(new { erreserbak = zerrenda });
    }

    // =====================================================
    //    PEDIDOS / TICKET (requisito 4 y izakinak)
    // =====================================================

    private static Emaitza Eska_Irekia(ErabiltzaileSaioa saioa)
    {
        if (EskaeraIrekiaBadago(saioa.ErabiltzaileId, out var irekitaBaidu))
            return Emaitza.Txar($"Zure {irekitaBaidu} zenbakiko eskaera irekita dago");

        return Idazketa(konexioa =>
        {
            using var agindua = konexioa.CreateCommand();
            agindua.CommandText = @"
                INSERT INTO Pedido (idUsuario, fechaHora, estado, total)
                VALUES (@u, @f, 'abierta', 0)
                RETURNING id";
            agindua.Parameters.AddWithValue("@u", saioa.ErabiltzaileId);
            agindua.Parameters.AddWithValue("@f", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            var id = Convert.ToInt32(agindua.ExecuteScalar());
            return Emaitza.Ondo(new { eskaeraId = id });
        });
    }

    private static Emaitza Eska_LerroGehitu(ErabiltzaileSaioa saioa, JsonElement d)
    {
        var eskaeraId = Zenbakia(d, "eskaeraId");
        var produktuId = Zenbakia(d, "produktuId");
        var kopurua = Zenbakia(d, "kopurua");
        if (eskaeraId <= 0 || produktuId <= 0 || kopurua <= 0)
            return Emaitza.Txar("Eskaeraren IDa, produktuaren IDa eta kopurua (> 0) nahitaezkoak dira");

        return Idazketa(konexioa =>
        {
            using var transakzioa = konexioa.BeginTransaction();

            // Eskaera erabiltzailearena eta irekita egon behar da
            using (var c = konexioa.CreateCommand())
            {
                c.Transaction = transakzioa;
                c.CommandText = @"SELECT idUsuario FROM Pedido
                                   WHERE id = @p AND estado = 'abierta'";
                c.Parameters.AddWithValue("@p", eskaeraId);
                var jabea = c.ExecuteScalar();
                if (jabea == null)
                {
                    transakzioa.Rollback();
                    return Emaitza.Txar("Ez da eskaera aurkitu, edo dagoeneko kobratu da");
                }
                if (saioa.Rola != "admin" && (long)jabea != saioa.ErabiltzaileId)
                {
                    transakzioa.Rollback();
                    return Emaitza.Txar("Eskaera hori ez da zurea");
                }
            }

            // Produktuaren izakin nahikoak egiaztatu (eskaeran jada gehitutakoa batuta)
            double prezioa;
            using (var c2 = konexioa.CreateCommand())
            {
                c2.Transaction = transakzioa;
                c2.CommandText = @"
                    SELECT precio, stock, activo,
                           IFNULL((SELECT SUM(l.cantidad) FROM LineaPedido l
                                    WHERE l.idPedido = @p
                                      AND l.idProducto = pr.id), 0)
                      FROM Producto pr
                     WHERE pr.id = @pr";
                c2.Parameters.AddWithValue("@p", eskaeraId);
                c2.Parameters.AddWithValue("@pr", produktuId);
                using var r2 = c2.ExecuteReader();
                if (!r2.Read())
                {
                    transakzioa.Rollback();
                    return Emaitza.Txar("Ez da produktua aurkitu");
                }
                if (r2.GetInt32(2) != 1)
                {
                    transakzioa.Rollback();
                    return Emaitza.Txar("Produktua ez dago eskuragarri");
                }
                prezioa = r2.GetDouble(0);
                var izakinak = r2.GetInt32(1);
                var eskaeranBaidu = r2.GetInt64(3);
                if (eskaeranBaidu + kopurua > izakinak)
                {
                    transakzioa.Rollback();
                    return Emaitza.Txar($"Ez dago nahikoa izakin: {izakinak} unitate geratzen dira produktu horretatik");
                }
            }

            using (var txertaketa = konexioa.CreateCommand())
            {
                txertaketa.Transaction = transakzioa;
                txertaketa.CommandText = @"
                    INSERT INTO LineaPedido (idPedido, idProducto, cantidad, precioUnitario)
                    VALUES (@p, @pr, @c, @pu)";
                txertaketa.Parameters.AddWithValue("@p", eskaeraId);
                txertaketa.Parameters.AddWithValue("@pr", produktuId);
                txertaketa.Parameters.AddWithValue("@c", kopurua);
                txertaketa.Parameters.AddWithValue("@pu", prezioa);
                txertaketa.ExecuteNonQuery();
            }

            using (var eguneratzea = konexioa.CreateCommand())
            {
                eguneratzea.Transaction = transakzioa;
                eguneratzea.CommandText = @"
                    UPDATE Pedido
                       SET total = (SELECT IFNULL(SUM(precioUnitario * cantidad), 0)
                                      FROM LineaPedido WHERE idPedido = @p)
                     WHERE id = @p";
                eguneratzea.Parameters.AddWithValue("@p", eskaeraId);
                eguneratzea.ExecuteNonQuery();
            }

            transakzioa.Commit();
            return Emaitza.Ondo(new { mezua = "Linea anadida", prezioUnitarioa = prezioa });
        });
    }

    /// <summary>
    /// Saioa itxi/ireki ondoren (gailu berean nahiz beste batean) ordaindu gabeko
    /// eskaeraren osagai-zerrenda BDtik berreskuratzen du. Datuak eskaeraId badakar,
    /// eskaera hori; bestela erabiltzailearen azken eskaera irekia.
    /// Jabetza: sortzailea edo admin soilik.
    /// </summary>
    private static Emaitza Eska_Berreskuratu(ErabiltzaileSaioa saioa, JsonElement d)
    {
        var eskaeraId = Zenbakia(d, "eskaeraId");

        using var konexioa = DatuBasea.Konektatu();
        konexioa.Open();

        int? mahaiId = null;
        using (var c = konexioa.CreateCommand())
        {
            if (eskaeraId > 0)
            {
                c.CommandText = @"SELECT id, idUsuario, idMesa, estado FROM Pedido WHERE id = @p";
                c.Parameters.AddWithValue("@p", eskaeraId);
            }
            else
            {
                c.CommandText = @"SELECT id, idUsuario, idMesa, estado FROM Pedido
                                   WHERE idUsuario = @u AND estado = 'abierta'
                                   ORDER BY id DESC LIMIT 1";
                c.Parameters.AddWithValue("@u", saioa.ErabiltzaileId);
            }

            using var r = c.ExecuteReader();
            if (!r.Read())
            {
                if (eskaeraId > 0) return Emaitza.Txar("Ez da eskaera aurkitu");
                return Emaitza.Ondo(new
                {
                    eskaeraId = 0,
                    mahaiId = (int?)null,
                    mahaiZenbakia = (int?)null,
                    lerroak = Array.Empty<object>()
                });
            }

            if (saioa.Rola != "admin" && r.GetInt64(1) != saioa.ErabiltzaileId)
                return Emaitza.Txar("Eskaera hori ez da zurea");
            if (r.GetString(3) != "abierta")
                return Emaitza.Txar("Eskaera ez dago irekita (dagoeneko kobratu da)");

            eskaeraId = r.GetInt32(0);
            mahaiId = r.IsDBNull(2) ? (int?)null : Convert.ToInt32(r.GetValue(2));
        }

        int? mahaiZenbakia = null;
        if (mahaiId != null)
        {
            using var cm = konexioa.CreateCommand();
            cm.CommandText = "SELECT numMesa FROM Mesa WHERE id = @m";
            cm.Parameters.AddWithValue("@m", mahaiId.Value);
            var zenbakia = cm.ExecuteScalar();
            if (zenbakia != null) mahaiZenbakia = Convert.ToInt32(zenbakia);
        }

        var lerroak = new List<object>();
        using (var c2 = konexioa.CreateCommand())
        {
            c2.CommandText = @"
                SELECT l.idProducto, pr.nombre, l.cantidad, l.precioUnitario
                  FROM LineaPedido l JOIN Producto pr ON pr.id = l.idProducto
                 WHERE l.idPedido = @p ORDER BY l.id";
            c2.Parameters.AddWithValue("@p", eskaeraId);
            using var r2 = c2.ExecuteReader();
            while (r2.Read())
                lerroak.Add(new
                {
                    produktuId = r2.GetInt32(0),
                    izena = r2.GetString(1),
                    kopurua = r2.GetInt32(2),
                    prezioUnitarioa = Math.Round(r2.GetDouble(3), 2)
                });
        }

        return Emaitza.Ondo(new { eskaeraId, mahaiId, mahaiZenbakia, lerroak });
    }

    /// <summary>Eskaera irekia mahai bati lotzen dio (mapak okupatuta erakusteko).</summary>
    private static Emaitza Eska_MahaiEsleitu(ErabiltzaileSaioa saioa, JsonElement d)
    {
        var eskaeraId = Zenbakia(d, "eskaeraId");
        var mahaiId = Zenbakia(d, "mahaiId");
        if (eskaeraId <= 0 || mahaiId <= 0)
            return Emaitza.Txar("Eskaeraren IDa eta mahaiaren IDa nahitaezkoak dira");

        return Idazketa(konexioa =>
        {
            using var transakzioa = konexioa.BeginTransaction();

            using (var c = konexioa.CreateCommand())
            {
                c.Transaction = transakzioa;
                c.CommandText = "SELECT idUsuario, estado FROM Pedido WHERE id = @p";
                c.Parameters.AddWithValue("@p", eskaeraId);
                using var r = c.ExecuteReader();
                if (!r.Read())
                {
                    transakzioa.Rollback();
                    return Emaitza.Txar("Ez da eskaera aurkitu");
                }
                if (saioa.Rola != "admin" && r.GetInt64(0) != saioa.ErabiltzaileId)
                {
                    transakzioa.Rollback();
                    return Emaitza.Txar("Eskaera hori ez da zurea");
                }
                if (r.GetString(1) != "abierta")
                {
                    transakzioa.Rollback();
                    return Emaitza.Txar("Eskaera ez dago irekita");
                }
            }

            using (var c2 = konexioa.CreateCommand())
            {
                c2.Transaction = transakzioa;
                c2.CommandText = "SELECT COUNT(*) FROM Mesa WHERE id = @m";
                c2.Parameters.AddWithValue("@m", mahaiId);
                if (Convert.ToInt32(c2.ExecuteScalar()) == 0)
                {
                    transakzioa.Rollback();
                    return Emaitza.Txar("Mahaia ez da aurkitu");
                }
            }

            using (var u = konexioa.CreateCommand())
            {
                u.Transaction = transakzioa;
                u.CommandText = "UPDATE Pedido SET idMesa = @m WHERE id = @p";
                u.Parameters.AddWithValue("@m", mahaiId);
                u.Parameters.AddWithValue("@p", eskaeraId);
                u.ExecuteNonQuery();
            }

            transakzioa.Commit();
            return Emaitza.Ondo(new { mezua = "Mahaia esleituta", eskaeraId, mahaiId });
        });
    }

    // Erosketa baieztatu: egoera cobrada + IZAKIN-MURRIZKETA lock berean
    private static Emaitza Eska_Baieztatu(ErabiltzaileSaioa saioa, JsonElement d)
    {
        var eskaeraId = Zenbakia(d, "eskaeraId");
        if (eskaeraId <= 0) return Emaitza.Txar("Eskaeraren IDa falta da");

        return Idazketa(konexioa =>
        {
            using var transakzioa = konexioa.BeginTransaction();

            using (var c = konexioa.CreateCommand())
            {
                c.Transaction = transakzioa;
                c.CommandText = @"SELECT idUsuario, total, estado FROM Pedido WHERE id = @p";
                c.Parameters.AddWithValue("@p", eskaeraId);
                using var r = c.ExecuteReader();
                if (!r.Read())
                {
                    transakzioa.Rollback();
                    return Emaitza.Txar("Ez da eskaera aurkitu");
                }
                if (saioa.Rola != "admin" && (long)r.GetInt64(0) != saioa.ErabiltzaileId)
                {
                    transakzioa.Rollback();
                    return Emaitza.Txar("Eskaera hori ez da zurea");
                }
                if (r.GetString(2) != "abierta")
                {
                    transakzioa.Rollback();
                    return Emaitza.Txar("Eskaera ez dago irekita");
                }
            }

            // lerroak produktuz batuta
            var lerroak = new List<(int produktua, int kopurua)>();
            using (var c2 = konexioa.CreateCommand())
            {
                c2.Transaction = transakzioa;
                c2.CommandText = @"
                    SELECT idProducto, SUM(cantidad)
                      FROM LineaPedido WHERE idPedido = @p
                     GROUP BY idProducto";
                c2.Parameters.AddWithValue("@p", eskaeraId);
                using var r2 = c2.ExecuteReader();
                while (r2.Read()) lerroak.Add((r2.GetInt32(0), r2.GetInt32(1)));
            }
            if (lerroak.Count == 0)
            {
                transakzioa.Rollback();
                return Emaitza.Txar("Eskaera hutsik dago");
            }

            // izakin erabilgarriak aldi berean egiaztatu
            foreach (var (produktua, kopurua) in lerroak)
            {
                using var c3 = konexioa.CreateCommand();
                c3.Transaction = transakzioa;
                c3.CommandText = "SELECT stock FROM Producto WHERE id = @i";
                c3.Parameters.AddWithValue("@i", produktua);
                var izakinak = Convert.ToInt64(c3.ExecuteScalar());
                if (izakinak < kopurua)
                {
                    transakzioa.Rollback();
                    return Emaitza.Txar($"Ez dago nahikoa izakin {produktua} produktuan: {izakinak} unitate geratzen dira");
                }
            }

            // decremento de izakinak (requisito 4: actualizacion automatica)
            foreach (var (produktua, kopurua) in lerroak)
            {
                using var u = konexioa.CreateCommand();
                u.Transaction = transakzioa;
                u.CommandText = @"UPDATE Producto SET stock = stock - @c WHERE id = @i";
                u.Parameters.AddWithValue("@c", kopurua);
                u.Parameters.AddWithValue("@i", produktua);
                u.ExecuteNonQuery();
            }

            double guztira;
            using (var u2 = konexioa.CreateCommand())
            {
                u2.Transaction = transakzioa;
                u2.CommandText = @"UPDATE Pedido SET estado = 'cobrada' WHERE id = @p";
                u2.Parameters.AddWithValue("@p", eskaeraId);
                u2.ExecuteNonQuery();
                using var c4 = konexioa.CreateCommand();
                c4.Transaction = transakzioa;
                c4.CommandText = "SELECT total FROM Pedido WHERE id = @p";
                c4.Parameters.AddWithValue("@p", eskaeraId);
                guztira = Convert.ToDouble(c4.ExecuteScalar());
            }

            transakzioa.Commit();
            return Emaitza.Ondo(new
            {
                mezua = "Compra confirmada y stock actualizado",
                eskaeraId, guztira,
                kronologia = lerroak.Select(l => new { produktua = l.produktua, kopurua = l.kopurua })
            });
        });
    }

    private static Emaitza Eska_BertanBehera(ErabiltzaileSaioa saioa, JsonElement d)
    {
        var eskaeraId = Zenbakia(d, "eskaeraId");
        if (eskaeraId <= 0) return Emaitza.Txar("Eskaeraren IDa falta da");

        return Idazketa(konexioa =>
        {
            using var c = konexioa.CreateCommand();
            c.CommandText = @"SELECT idUsuario, estado FROM Pedido WHERE id = @p";
            c.Parameters.AddWithValue("@p", eskaeraId);
            using var r = c.ExecuteReader();
            if (!r.Read()) return Emaitza.Txar("Ez da eskaera aurkitu");
            if (saioa.Rola != "admin" && r.GetInt64(0) != saioa.ErabiltzaileId)
                return Emaitza.Txar("Eskaera hori ez da zurea");
            if (r.GetString(1) != "abierta") return Emaitza.Txar("Eskaera ez dago irekita");

            using var u = konexioa.CreateCommand();
            u.CommandText = "UPDATE Pedido SET estado = 'anulada' WHERE id = @p";
            u.Parameters.AddWithValue("@p", eskaeraId);
            u.ExecuteNonQuery();
            return Emaitza.Ondo(new { mezua = "Pedido anulado" });
        });
    }

    private static Emaitza Eska_Txartela(ErabiltzaileSaioa saioa, JsonElement d)
    {
        var eskaeraId = Zenbakia(d, "eskaeraId");
        if (eskaeraId <= 0) return Emaitza.Txar("Eskaeraren IDa falta da");

        using var konexioa = DatuBasea.Konektatu();
        konexioa.Open();
        string? bezeroa = null; string data = ""; string egoera = ""; double guztira = 0;
        using (var c = konexioa.CreateCommand())
        {
            c.CommandText = @"SELECT u.nombre, p.fechaHora, p.estado, p.total, p.idUsuario
                                FROM Pedido p JOIN Usuario u ON u.id = p.idUsuario
                               WHERE p.id = @p";
            c.Parameters.AddWithValue("@p", eskaeraId);
            using var r = c.ExecuteReader();
            if (!r.Read()) return Emaitza.Txar("Ez da eskaera aurkitu");
            // Eskaera irekiak (eta tiketak) sortzaileak edo admin-ek soilik ikus ditzakete
            if (saioa.Rola != "admin" && r.GetInt64(4) != saioa.ErabiltzaileId)
                return Emaitza.Txar("Eskaera hori ez da zurea");
            bezeroa = r.GetString(0); data = r.GetString(1);
            egoera = r.GetString(2); guztira = r.GetDouble(3);
        }

        var lerroak = new List<object>();
        using (var c2 = konexioa.CreateCommand())
        {
            c2.CommandText = @"
                SELECT pr.nombre, l.cantidad, l.precioUnitario
                  FROM LineaPedido l JOIN Producto pr ON pr.id = l.idProducto
                 WHERE l.idPedido = @p ORDER BY l.id";
            c2.Parameters.AddWithValue("@p", eskaeraId);
            using var r2 = c2.ExecuteReader();
            while (r2.Read())
                lerroak.Add(new { izena = r2.GetString(0), kopurua = r2.GetInt32(1),
                    prezioUnitarioa = Math.Round(r2.GetDouble(2), 2) });
        }

        return Emaitza.Ondo(new
        {
            txartela = new { eskaeraId, bezeroa, data, egoera, guztira,
                lerroak, beza = "%10 barne" }
        });
    }

    // =====================================================
    //    UTILIDADES
    // =====================================================

    private static bool EskaeraIrekiaBadago(int idUsuario, out int eskaeraId)
    {
        using var konexioa = DatuBasea.Konektatu();
        konexioa.Open();
        using var agindua = konexioa.CreateCommand();
        agindua.CommandText = @"SELECT id FROM Pedido
                             WHERE idUsuario = @u AND estado = 'abierta'";
        agindua.Parameters.AddWithValue("@u", idUsuario);
        var emaitza = agindua.ExecuteScalar();
        eskaeraId = emaitza == null ? 0 : Convert.ToInt32(emaitza);
        return emaitza != null;
    }

    // NEGOZIO-IDAZKETA GUZTIAK hemendik pasatzen dira: LOCK BAKARRA + transakzioa
    private static Emaitza Idazketa(Func<SqliteConnection, Emaitza> blokea)
    {
        lock (DatuBasea.IdazketaBlokeoa)
        {
            using var konexioa = DatuBasea.Konektatu();
            konexioa.Open();
            return blokea(konexioa);
        }
    }

}
