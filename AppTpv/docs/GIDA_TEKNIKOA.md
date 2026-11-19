# AppTpv — Gida teknikoa

Dokumentu honek sistemaren barne-funtzionamendua azaltzen du: arkitektura,
datu-basea, protokoloa, interfazeak eta konkurrentzia-kudeaketa.
Erabilera-gidak: [admin](GIDA_ADMIN.md) eta [erabiltzailea](GIDA_ERABILTZAILEA.md).
Defentsarako material espezifikoa: [zerbitzuak, prozesuak, hariak eta socketak](AURKEZPENA_ZERBITZUAK_PROZESUAK_HARIAK_SOCKETAK.md).

## 1. Arkitektura

```
   Windows (VM / PC)                    Zerbitzaria (macOS / Linux / Windows)
 ┌────────────────────────┐            ┌──────────────────────────────────┐
 │ ClienteWPF (WPF/XAML)  │  TCP 5001  │ ServidorTPV (.NET 10 konsola)    │
 │  VentanaLogin          │ ─────────> │  ServidorTCP (hari bana)         │
 │  VentanaPrincipal      │ <───────── │  ClienteConexion (JSON lerro bat)│
 │   ├ VistaConsumiciones │            │  GestorOperaciones (ekintzak)    │
 │   ├ VistaReservas      │            │  Sesiones (token GUID)           │
 │   ├ VistaUsuarios      │            │  BaseDatos (SQLite, lock global) │
 │   └ VistaStock         │            │        │                          │
 └────────────────────────┘            │        ▼  data/tpv.db             │
                                       └──────────────────────────────────┘
```

- **Garraioa:** `TcpListener` `0.0.0.0:5001` atakan (5000 macOS-ek erabiltzen duelako).
- **Mezuen formatua:** JSON UTF-8, lerro bana mezua (`\n` bereizle gisa).
- **Gehienez 10 bezero aldi berean.** 11.ak `SESSION_LIMIT` errorea jasotzen du.
- **Bezero bakoitzeko hari bat** (`ServidorTCP.cs`): zerbitzariak erantzuten
  ari den bitartean beste bezeroek eta UI-ak normal funtzionatzen dute.
- Bezeroak konexio bakarra mantentzen du (`Conexion.cs`), bidalketak `_candado`
  batekin serializatzen dira.

## 2. Fitxategien mapa

| Fitxategia | eginkizuna |
|---|---|
| `ServidorTPV/Program.cs` | BD hasieratu + TCP abiarazi; `parar` komandoa |
| `ServidorTPV/Red/ServidorTCP.cs` | Entzulea, 10 bezeroen mugra, hari bana |
| `ServidorTPV/Red/ClienteConexion.cs` | Lerroen irakurketa, JSON parsing, dispatch |
| `ServidorTPV/Servicios/GestorOperaciones.cs` | Ekintza guztien logika + rol guardak |
| `ServidorTPV/Servicios/Sesiones.cs` | Tokenen mapa konkurrentea |
| `ServidorTPV/Servicios/Resultado.cs` | `{ ok, datos, error }` emaitza |
| `ServidorTPV/Data/BaseDatos.cs` | Konexioa, eskema, haziak, `LockEscritura` |
| `ServidorTPV/Data/Esquema.sql` | Taulak, indizeak, WAL |
| `ServidorTPV/Data/Seguridad.cs` | SHA-256 + gatz finkoa |
| `ClienteWPF/Servicios/Conexion.cs` | Bezeroaren TCP konexioa |
| `ClienteWPF/Servicios/Sesion.cs` | Token/rol lokala |
| `ClienteWPF/Servicios/Modelos.cs` | JSON → objektuen mapaketa |
| `ClienteWPF/Ventanas/*.xaml` | Login, nagusia, kantitate leihoa |
| `ClienteWPF/Vistas/*.xaml` | 4 erabiltzaile-kontrol (ikus 5. atala) |

## 3. Datu-basea (SQLite)

`data/tpv.db` fitxategia `AppContext.BaseDirectory`en sortzen da.
`BaseDatos.Inicializar()`-ek `Esquema.sql` exekutatzen du (dena `IF NOT EXISTS`)
eta, fitxategia berria bada, haziak txertatzen ditu.

| Taula | Helburua | Zutabe garrantzitsuak |
|---|---|---|
| `Usuario` | Saioa eta rolak | `login` (UNIQUE), `passHash`, `nombre`, `rol` (`admin`/`usuario`), `activo` |
| `Mesa` | Erreserbetarako mahaiak | `numMesa`, `capacidad` |
| `Reserva` | Erreserbak | `idMesa`, `fecha` (NULL = gabe), `turno` (`bazkaria`/`afaria`), `numComensales`, `idUsuario`, `estado` (`activa`/`cancelada`) |
| `Producto` | Carta + biltegia | `nombre`, `precio`, `stock`, `ingredientes` (osagaiak), `categoria`, `activo` |
| `Pedido` | Komandaren burua | `idUsuario`, `fechaHora`, `estado` (`abierta`/`cobrada`/`anulada`), `total` |
| `LineaPedido` | Komandaren lerroak | `idPedido`, `idProducto`, `cantidad`, `precioUnitario` (historikoa) |

- `PRAGMA foreign_keys = ON` eta `PRAGMA journal_mode = WAL` (irakurketak
  paraleloan, idazketak serializatuta).
- **Ezabaketa logikoa:** `Usuario.activo = 0`, `Producto.activo = 0`,
  `Reserva.estado = 'cancelada'`. Horrela ticketen historia ez da apurtzen.
- Indizeak: `(fecha, turno)`, `(idMesa, fecha)`, `(idPedido)`.

### Haziak (lehen abiaraztea)

- `admin` / `admin123` — rola `admin`, izena `Administrador`.
- Mahaiak: 1 (4 plaza), 2 (4), 3 (6).
- Produktuak: Kola kontxa (2,50 € / 24), Kalimotxo (3,00 € / 40),
  Prentxikoa (3,00 € / 12), Sakramento burger (6,50 € / 8).
- Carta (17 edari, bakoitzeko 20 unitate):
  - `Kafeak`: Hutsa 1,70 € · Ebakia 1,75 € · kafesnea 1,80 €.
  - `Freskagarriak`: koka-kola, Limoia, laranja, Tonika, Nestea, Aquarius — 2,50 €.
  - `Garagardoa`: Zuritoa 1,80 € · Kaña 3,00 € · Botila 2,50 €.
  - `Ardo Beltzak`: Urtekoa 2,00 € · kriantzak 3,00 €.
  - `Ardo Txuriak`: Rueda 2,00 € · Berdejo 2,50 € · chardonay 3,00 €.

## 4. Protokoloa

Eskaera:

```json
{ "accion": "LOGIN", "token": "", "datos": { "login": "admin", "pass": "admin123" } }
```

Erantzuna:

```json
{ "ok": true, "datos": { "token": "...", "rol": "admin", "nombre": "Administrador" } }
```

Errorea:

```json
{ "ok": false, "error": "Credenciales invalidas" }
```

`LOGIN` ondoren, mezu guztiek tokena daramate; zerbitzariak rolari dagokion
baimena egiaztatzen du `RequiereAdmin` / `RequiereSesion` guardekin.

| Ekintza | Baimena | Bezeroan deitzen duen kontrola |
|---|---|---|
| `LOGIN` | – | `VentanaLogin.BtnEntrar_Click` |
| `LOGOUT` | saioa | `App.xaml.cs`, `VentanaPrincipal.BtnSalir_Click` |
| `PRODUCTOS_LISTA` | saioa | `VistaStock`, `VistaConsumiciones` |
| `PRODUCTO_AGREGAR` / `_MODIFICAR` / `_BORRAR` | admin | `VistaStock` |
| `STOCK_MODIFICAR` | admin | `VistaStock.BtnStock_Click` |
| `USUARIOS_LISTA` / `USUARIO_CREAR` / `_MODIFICAR` / `_BORRAR` | admin | `VistaUsuarios` |
| `MESAS_LISTA` | saioa | `VistaReservas.CargarMesas` |
| `RESERVA_CREAR` / `_MODIFICAR` / `_BORRAR` / `_LISTA` | saioa | `VistaReservas` |
| `PEDIDO_ABRIR` | saioa | `VistaConsumiciones.AsegurarPedidoAbierto` |
| `PEDIDO_AGREGAR_LINEA` | saioa | `VistaConsumiciones.BtnEnviar_Click` |
| `PEDIDO_CONFIRMAR` | saioa | `VistaConsumiciones.BtnCobrar_Click` |
| `TICKET_OBTENER` | saioa | `VistaConsumiciones` (SUBTOTAL / VER TICKET) |

`LOGOUT` ez dago `GestorOperaciones`-en: `ClienteConexion.cs`-ek kudeatzen du
zuzenean, tokena saioen mapatik kentzeko.

Xehetasunak: [PROTOCOLO.md](PROTOCOLO.md).

## 5. Interfazeen mapa (XAML)

Erronkak 5 interfaze eskatzen ditu; horiek dira, zehazki, zure XAML fitxategiak:

| # | Interfazea | XAML | Kontrol nagusiak |
|---|---|---|---|
| 1 | Login | `Ventanas/VentanaLogin.xaml` | `TxtLogin`, `TxtPass`, `BtnEntrar`, `TxtError` |
| 2 | Erabiltzaileen kudeaketa | `Vistas/VistaUsuarios.xaml` | `TxtLogin`, `TxtPass`, `TxtNombre`, `CboRol`, `GridUsuarios` |
| 3 | Biltegiaren kudeaketa | `Vistas/VistaStock.xaml` | `TxtNombre`, `TxtPrecio`, `TxtStock`, `TxtIngredientes`, `TxtCategoria`, `GridProductos`, `TxtNuevoStock` |
| 4 | Interfaze nagusia | `Vistas/VistaConsumiciones.xaml` | `ListaCategorias`, `ListaProductos`, `ListaPedido`, `BtnEnviar`, `BtnSubtotal`, `BtnCobrar`, `BtnTicket` |
| 5 | Erreserbak | `Vistas/VistaReservas.xaml` | `CboMesa`, `CboTurno`, `TxtFecha`, `TxtComensales`, `GridReservas` |

Osagarriak:

- `Ventanas/VentanaPrincipal.xaml` — 4 kontrolak ostatatzen dituen `TabControl`;
  `Sesion.EsAdmin` false denean **Usuarios** eta **Stock** pestañak
  `Visibility.Collapsed` bihurtzen dira.
- `Ventanas/VentanaCantidad.xaml` — kantitatea eskatzeko leiho laguntzailea.
  Gaur egun `VistaConsumiciones`-ek lerroaren inline editorea erabiltzen du;
  leiho hau ez dago askotan deituta (eskuragarri dago, erabiltzeko prest).

## 6. Segurtasuna

- Pasahitza ez da testu garbiz gordetzen: `SHA256(Sal + password)`, non
  `Sal = "AppTpv::ErronkaWIDZPP::2026"`.
- `LOGIN`-ek `activo = 1` duten erabiltzaileak soilik onartzen ditu.
- Tokena `Guid.NewGuid().ToString("N")` da; `Sesiones` klaseak
  `ConcurrentDictionary` batean gordetzen du, rola erantsita.
- Rol guardak zerbitzarian daude, bezeroan ez: bezeroak pestañak ezkutatzea
  erosotasuna baino ez da; zerbitzariak benetan ukatzen du baimenik gabeko
  ekintza (`Accion reservada al administrador`).

## 7. Konkurrentzia

Erronkak eskatzen duenez, **idazketa guztiak lock bakar batekin** egiten dira:

```csharp
lock (BaseDatos.LockEscritura)   // ServidorTPV/Data/BaseDatos.cs
{
    using var con = BaseDatos.Conectar();
    return bloque(con);   // transakzioa barne, behar denean
}
```

- Bi erabiltzailek aldi berean taula berean idazten saiatuz gero, bigarrena
  lehena amaitu arte itxaron egiten da.
- `Res_Crear`, `Ped_AgregarLinea` eta `Ped_Confirmar`-ek transakzioak
  erabiltzen dituzte (rollback egiten dute baliozkotzeak huts eginez gero).
- WAL moduari esker, irakurketak (`PRODUCTOS_LISTA`, `RESERVA_LISTA`,
  `TICKET_OBTENER`, ...) lock gabe egin daitezke.
- Defentsan justifikatzeko: ≤10 bezero eta idazketa laburrekin, taula bakoitzeko
  lock-ek deadlock arriskua handitzen dute; enunciatuak berak baimentzen du
  lock bakarra erabiltzea.

## 8. Stock eguneratze automatikoa

`BtnCobrar_Click` → `PEDIDO_CONFIRMAR` → `Ped_Confirmar` (osas 671-770):

1. Pedikoa irekita dagoela eta erabiltzailearena dela egiaztatu.
2. Lerroak produktuka batu (`SUM(cantidad)`).
3. Produktu bakoitzaren stocka egiaztatu; nahikoa ez bada, transakzioa
   bertan behera utzi eta `Stock insuficiente ... quedan N` errorea itzuli.
4. `UPDATE Producto SET stock = stock - @c` lerro bakoitzerako.
5. Pedikoa `cobrada` egoerara aldatu, `total` kalkulatu eta `commit`.
6. Bezeroak `CargarCarta()` berriro deitzen du; `AGOTADO` etiketa eguneratzen da.

Ticketa: `TICKET_OBTENER`-ek bezeroa, data, egoera, lerroak
(`nombre`, `cantidad`, `precioUnitario`) eta guztira itzultzen ditu;
`VistaConsumiciones` `MessageBox` batean erakusten du.

## 9. Erroreen kudeaketa

- Zerbitzariak `{ ok: false, error: "..." }` itzultzen du; bezeroak `TxtAviso`
  (admin/reserbak) edo `MessageBox` batean erakusten du.
- Konexioa galduta: `Conexion.Enviar`-ek `Cerrar()` egiten du eta
  `No se pudo conectar al servidor IP:ATAKA` mezua itzultzen du.
- Zerbitzariaren kontsolan `[ERROR] <AKCIOA>: <mezua>` agertzen da.

## 10. Probak eta hedapena

`docs/PLAN.md` fitxategian 40 urratseko proben zerrenda dago:
login, rol-guardak, erreserbak (data gabe/ekin, bikoiztuak, edukiera),
pedidoa, stock murrizketa (`Prentxikoa 12 → 2`), ticket-a, logout eta
10 bezeroen muga.

Windows VMrako argitalpena:

```bash
dotnet publish ClienteWPF/ClienteWPF.csproj -c Release \
  -r win-x64 --self-contained true -o ClienteWPF/publish_vm_x64
```

Kopiatu karpeta VM-ra, jarri `servidor.txt` (ostalariaren IP + `5001`) eta
exekutatu `AppTpv.exe`.

### Ezagutzen diren puntuak

- Zerbitzariaren `Cliente desconectado` log-mezua noizean behin desordenatuta
  agertzen da (kosmetikoa).
- `RESERVA_MODIFICAR`-ek ez du berriro egiaztatzen mahaiaren edukiera ez eta
  bikoiztutako erreserba; UI-tik erabiltzen den bidean ez da arazorik sortzen.
- `VentanaCantidad.xaml` leihoa ez dago gaur egun deituta (ikus 5. atala).
