# AppTpv — Gida teknikoa

Dokumentu honek sistemaren barne-funtzionamendua azaltzen du: arkitektura,
datu-basea, protokoloa, interfazeak eta konkurrentzia-kudeaketa.
Erabilera-gidak: [admin](GIDA_ADMIN.md) eta [erabiltzailea](GIDA_ERABILTZAILEA.md).
Defentsarako material espezifikoa: [zerbitzuak, prozesuak, hariak eta socketak](AURKEZPENA_ZERBITZUAK_PROZESUAK_HARIAK_SOCKETAK.md).
Hizkuntzen (EU/ES) aldaketarako plana: [hizkuntzak](hizkuntzak.md).

## 1. Arkitektura

```
  BezeroaWPF (WPF/XAML)                    ZerbitzariaTPV (.NET 10 kontsola)
  ├─ SaioHasieraLeihoa       TCP 5001      ├─ TCPZerbitzaria (hari bana)
  ├─ LeihoNagusia            ────────>     ├─ BezeroKonexioa (JSON lerro bat)
  ├─ KontsumizioIkuspegia    <────────     ├─ EragiketaKudeatzailea (ekintzak)
  ├─ ErreserbaIkuspegia                    ├─ Saioak (token GUID)
  ├─ ErabiltzaileIkuspegia                 └─ DatuBasea (SQLite, lock global)
  └─ IzakinIkuspegia                                │
                                                    ▼  data/tpv.db
```

- **Garraioa:** `TcpListener` `0.0.0.0:5001` atakan (5000 macOS-ek erabiltzen duelako).
- **Mezuen formatua:** JSON UTF-8, lerro bana mezua (`\n` bereizle gisa).
- **Gehienez 10 bezero aldi berean.** 11.ak `SESSION_LIMIT` errorea jasotzen du.
- **Bezero bakoitzeko hari bat** (`TCPZerbitzaria.cs`): zerbitzariak erantzuten
  ari den bitartean beste bezeroek eta UI-ak normal funtzionatzen dute.
- Bezeroak konexio bakarra mantentzen du (`Konexioa.cs`), bidalketak `_blokeoa`
  batekin serializatzen dira.

## 2. Fitxategien mapa

| Fitxategia | eginkizuna |
|---|---|
| `ZerbitzariaTPV/Programa.cs` | BD hasieratu + TCP abiarazi; `parar` komandoa |
| `ZerbitzariaTPV/Sarea/TCPZerbitzaria.cs` | Entzulea, 10 bezeroen mugra, hari bana |
| `ZerbitzariaTPV/Sarea/BezeroKonexioa.cs` | Lerroen irakurketa, JSON parsing, dispatch |
| `ZerbitzariaTPV/Zerbitzuak/EragiketaKudeatzailea.cs` | Ekintza guztien logika + rol guardak |
| `ZerbitzariaTPV/Zerbitzuak/Saioak.cs` | Tokenen mapa konkurrentea |
| `ZerbitzariaTPV/Zerbitzuak/Emaitza.cs` | `{ arrakasta, datuak, errorea }` emaitza |
| `ZerbitzariaTPV/Datuak/DatuBasea.cs` | Konexioa, eskema, haziak, `Pedido.idMesa` migrazioa, `IdazketaBlokeoa` |
| `ZerbitzariaTPV/Datuak/Eskema.sql` | Taulak, indizeak (`idx_pedido_mesa`), WAL |
| `ZerbitzariaTPV/Datuak/Segurtasuna.cs` | SHA-256 + gatz finkoa |
| `BezeroaWPF/Zerbitzuak/Konexioa.cs` | Bezeroaren TCP konexioa |
| `BezeroaWPF/Zerbitzuak/Saioa.cs` | Token/rol lokala |
| `BezeroaWPF/Zerbitzuak/Ereduak.cs` | JSON → objektuen mapaketa |
| `BezeroaWPF/Leihoak/*.xaml` | Login, nagusia, kantitate leihoa |
| `BezeroaWPF/Ikuspegiak/*.xaml` | 6 erabiltzaile-kontrol, mahaien mapa barne (ikus 5. atala) |

## 3. Datu-basea (SQLite)

`data/tpv.db` fitxategia `AppContext.BaseDirectory`en sortzen da.
`DatuBasea.Hasieratu()`-ek `Eskema.sql` exekutatzen du (dena `IF NOT EXISTS`)
eta, fitxategia berria bada, haziak txertatzen ditu.

| Taula | Helburua | Zutabe garrantzitsuak |
|---|---|---|
| `Usuario` | Saioa eta rolak | `login` (UNIQUE), `passHash`, `nombre`, `rol` (`admin`/`usuario`), `activo` |
| `Mesa` | Erreserbetarako mahaiak | `numMesa`, `capacidad` |
| `Reserva` | Erreserbak | `idMesa`, `fecha` (NULL = gabe), `turno` (`bazkaria`/`afaria`), `numComensales`, `idUsuario`, `estado` (`activa`/`cancelada`) |
| `Producto` | Carta + biltegia | `nombre`, `precio`, `stock`, `ingredientes` (osagaiak), `categoria`, `activo` |
| `Pedido` | Komandaren burua | `idUsuario`, `idMesa` (NULL = mahairik gabe), `fechaHora`, `estado` (`abierta`/`cobrada`/`anulada`), `total` |
| `LineaPedido` | Komandaren lerroak | `idPedido`, `idProducto`, `cantidad`, `precioUnitario` (historikoa) |

- `PRAGMA foreign_keys = ON` eta `PRAGMA journal_mode = WAL` (irakurketak
  paraleloan, idazketak serializatuta).
- **Ezabaketa logikoa:** `Usuario.activo = 0`, `Producto.activo = 0`,
  `Reserva.estado = 'cancelada'`. Horrela ticketen historia ez da apurtzen.
- Indizeak: `(fecha, turno)`, `(idMesa, fecha)`, `(idPedido)`, `(idMesa, estado)`.

`DatuBasea.Hasieratu()`-ek **migrazio idempotentea** exekutatzen du eskema baino
lehen: lehendik dauden DBetan `Pedido.idMesa` zutabea gehitzen du
(`ALTER TABLE ... ADD COLUMN`, NULL onargarria → ez da daturik galtzen).
`Eskema.sql`-en zutabea eta `idx_pedido_mesa` indizea jada daude DB berrietan.

**Eskaeren iraunkortasuna:** `PEDIDO_AGREGAR_LINEA`-k `LineaPedido` erregistroak
idazten ditu komandan; bezeroak `PEDIDO_REKUPERATU` erabiltzen du saioa irekitzean
DBtik berreskuratzeko. Hori dela eta, saioa itxi/ireki arren —gailu berean nahiz
bestean— ordaindu gabeko eskaeraren osagai-zerrenda agertzen da berriro.

### Haziak (lehen abiaraztea)

- `admin` / `admin123` — rola `admin`, izena `Administrador`.
- 15 erabiltzaile (`1234` pasahitza, rola `usuario` → UI-an "Erabiltzailea"):
  `ane.azkue`, `iker.etxeberria`, `nerea.garai`, `jon.aranburu`,
  `maddi.oiartzabal`, `gorka.larralde`, `uxue.mendoza`, `asier.bengoa`,
  `leire.sarobe`, `eneko.iturbe`, `amets.salazar`, `ihintza.barandiaran`,
  `hodei.mintegi`, `eider.zabaleta`, `unai.berasategi`.
- Mahaiak: 1–2 (4 plaza), 3–4 (6 plaza), 5–7 (10 plaza) — **7 mahai** guztira.
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
{ "ekintza": "LOGIN", "token": "", "datuak": { "erabiltzailea": "admin", "pasahitza": "admin123" } }
```

Erantzuna:

```json
{ "arrakasta": true, "datuak": { "token": "...", "rola": "admin", "izena": "Administrador" } }
```

Errorea:

```json
{ "arrakasta": false, "errorea": "Erabiltzaile-izena edo pasahitza ez dira zuzenak" }
```

`LOGIN` ondoren, mezu guztiek tokena daramate; zerbitzariak rolari dagokion
baimena egiaztatzen du `AdminEskatzenDu` / `SaioaEskatzenDu` guardekin.

| Ekintza | Baimena | Bezeroan deitzen duen kontrola |
|---|---|---|
| `LOGIN` | – | `SaioHasieraLeihoa.BtnSartu_Click` |
| `LOGOUT` | saioa | `App.xaml.cs`, `LeihoNagusia.BtnIrten_Click` |
| `PRODUCTOS_LISTA` | saioa | `IzakinIkuspegia`, `KontsumizioIkuspegia` |
| `PRODUCTO_AGREGAR` / `_MODIFICAR` / `_BORRAR` | admin | `IzakinIkuspegia` |
| `STOCK_MODIFICAR` | admin | `IzakinIkuspegia.BtnIzakinak_Click` |
| `USUARIOS_LISTA` / `USUARIO_CREAR` / `_MODIFICAR` / `_BORRAR` | admin | `ErabiltzaileIkuspegia` |
| `MESAS_LISTA` | saioa | `ErreserbaIkuspegia`, `KontsumizioIkuspegia`, `MahaiMapaIkuspegia` |
| `RESERVA_CREAR` / `_MODIFICAR` / `_BORRAR` / `_LISTA` | saioa | `ErreserbaIkuspegia` |
| `PEDIDO_ABRIR` | saioa | `KontsumizioIkuspegia.EskaeraIrekiaBermatu` |
| `PEDIDO_AGREGAR_LINEA` | saioa (jabea/admin) | `KontsumizioIkuspegia.BtnBidali_Click` |
| `PEDIDO_REKUPERATU` | saioa (jabea/admin) | `KontsumizioIkuspegia.EskaeraBerreskuratu` (Loaded) |
| `PEDIDO_MAHAIA_ESLEITU` | saioa (jabea/admin) | `KontsumizioIkuspegia.BtnMahiaEsleitu_Click` |
| `PEDIDO_CONFIRMAR` | saioa (jabea/admin) | `KontsumizioIkuspegia.BtnKobratu_Click` |
| `PEDIDO_CANCELAR` | saioa (jabea/admin) | `KontsumizioIkuspegia` |
| `TICKET_OBTENER` | saioa (jabea/admin) | `KontsumizioIkuspegia` (AZPITOTALA / IKUSI TIKETA) |

Guztira **22 ekintza + `LOGOUT`**. `LOGOUT` ez dago `EragiketaKudeatzailea`-en:
`BezeroKonexioa.cs`-ek kudeatzen du zuzenean, tokena saioen mapatik kentzeko.

`BezeroKonexioa.Artatu()`-ren `try/finally` blokeak socket-a ixten denean
konexio horretan sortutako token guztiak ezabatzen ditu, LOGOUT esplizitua ez badago
ere (`[SESION] Konexioa itxita: N token garbituta`). 10 bezeroen muga aldatu gabe dago.

Xehetasunak: [PROTOCOLO.md](PROTOCOLO.md).

## 5. Interfazeen mapa (XAML)

Erronkak 5 interfaze eskatzen ditu; horiei **mahaien mapa** (6. fitxa) gehitu zaie:

| # | Interfazea | XAML | Kontrol nagusiak |
|---|---|---|---|
| 1 | Login | `Leihoak/SaioHasieraLeihoa.xaml` | `TxtErabiltzailea`, `TxtPasahitza`, `BtnSartu`, `TxtErrorea` |
| 2 | Erabiltzaileen kudeaketa | `Ikuspegiak/ErabiltzaileIkuspegia.xaml` | `TxtErabiltzailea`, `TxtPasahitza`, `TxtIzena`, `CboRola`, `GridErabiltzaileak` |
| 3 | Biltegiaren kudeaketa | `Ikuspegiak/IzakinIkuspegia.xaml` | `TxtIzena`, `TxtPrezioa`, `TxtIzakinak`, `TxtOsagaiak`, `TxtKategoria`, `GridProduktuak`, `TxtIzakinBerriak` |
| 4 | Interfaze nagusia | `Ikuspegiak/KontsumizioIkuspegia.xaml` | `ListaKategoriak`, `ListaProduktuak`, `ListaEskaera`, `CboMahaiak`, `BtnMahiaEsleitu`, `BtnBidali`, `BtnAzpitotala`, `BtnKobratu`, `BtnTxartela` |
| 5 | Erreserbak | `Ikuspegiak/ErreserbaIkuspegia.xaml` | `CboMahia`, `CboTxanda`, `TxtData`, `TxtPertsonak`, `GridErreserbak` |
| 6 | Mahaien mapa | `Ikuspegiak/MahaiMapaIkuspegia.xaml` | `ListaMahaiak`, `BtnFreskatu`, `TxtMapaInfo` — 7 mahai, egoera bisualarekin |

`KontsumizioIkuspegia.Loaded`-ek `MenuaKargatu()`, `MahaiakKargatu()` eta
`EskaeraBerreskuratu()` deitzen ditu: saioa irekitzean DBn gordetako eskaera irekia
eta haren osagaiak berreraikitzen dira.

Osagarriak:

- `Leihoak/LeihoNagusia.xaml` — 5 fitxa dituen `TabControl`
  (Kontsumizioak, Erreserbak, **Mahaiak**, Erabiltzaileak, Izakinak);
  `Saioa.AdminDa` false denean **Erabiltzaileak** eta **Izakinak** pestañak
  `Visibility.Collapsed` bihurtzen dira. Mahaien mapa erabiltzaile guztientzat ikusgai dago.
- `Leihoak/KopuruLeihoa.xaml` — kantitatea eskatzeko leiho laguntzailea.
  Gaur egun `KontsumizioIkuspegia`-ek lerroaren inline editorea erabiltzen du;
  leiho hau ez dago askotan deituta (eskuragarri dago, erabiltzeko prest).

## 6. Segurtasuna

- Pasahitza ez da testu garbiz gordetzen: `SHA256(Sal + pasahitza)`, non
  `Sal = "AppTpv::ErronkaWIDZPP::2026"`.
- `LOGIN`-ek `activo = 1` duten erabiltzaileak soilik onartzen ditu.
- Tokena `Guid.NewGuid().ToString("N")` da; `Saioak` klaseak
  `ConcurrentDictionary` batean gordetzen du, rola erantsita.
- Rol guardak zerbitzarian daude, bezeroan ez: bezeroak pestañak ezkutatzea
  erosotasuna baino ez da; zerbitzariak benetan ukatzen du baimenik gabeko
  ekintza (`Ekintza hau administratzaileentzat bakarrik dago baimenduta`).
- Eskaera irekien **jabetza-guardak**: `PEDIDO_REKUPERATU`,
  `PEDIDO_AGREGAR_LINEA`, `PEDIDO_CONFIRMAR`, `PEDIDO_CANCELAR`,
  `PEDIDO_MAHAIA_ESLEITU` eta `TICKET_OBTENER` ekintzetan sortzailea edo admin
  soilik aritu daiteke. Bestela: `Eskaera hori ez da zurea`.
- Tokenen garbiketa: `LOGOUT` esplizitua ez badago ere, socket-a ixten denean
  `BezeroKonexioa.Artatu()`-ren `finally` blokeak konexio horretako tokenak
  ezabatzen ditu, saio zaharrak pilatu ez daitezen.

## 7. Konkurrentzia

Erronkak eskatzen duenez, **idazketa guztiak lock bakar batekin** egiten dira:

```csharp
lock (DatuBasea.IdazketaBlokeoa)   // ZerbitzariaTPV/Datuak/DatuBasea.cs
{
    using var con = DatuBasea.Konektatu();
    return bloque(con);   // transakzioa barne, behar denean
}
```

- Bi erabiltzailek aldi berean taula berean idazten saiatuz gero, bigarrena
  lehena amaitu arte itxaron egiten da.
- `Erres_Sortu`, `Eska_LerroGehitu`, `Eska_MahaiEsleitu` eta `Eska_Baieztatu`-ek
  transakzioak erabiltzen dituzte (rollback egiten dute baliozkotzeak huts eginez gero).
- WAL moduari esker, irakurketak (`PRODUCTOS_LISTA`, `RESERVA_LISTA`,
  `TICKET_OBTENER`, ...) lock gabe egin daitezke.
- Defentsan justifikatzeko: ≤10 bezero eta idazketa laburrekin, taula bakoitzeko
  lock-ek deadlock arriskua handitzen dute; enunciatuak berak baimentzen du
  lock bakarra erabiltzea.

## 8. Izakinak eguneratze automatikoa

`BtnKobratu_Click` → `PEDIDO_CONFIRMAR` → `Eska_Baieztatu`:

1. Eskaera irekita dagoela eta erabiltzailearena dela egiaztatu.
2. Lerroak produktuka batu (`SUM(cantidad)`).
3. Produktu bakoitzaren stocka egiaztatu; nahikoa ez bada, transakzioa
   bertan behera utzi eta `Ez dago nahikoa izakin ... unitate geratzen dira` errorea itzuli.
4. `UPDATE Producto SET stock = stock - @c` lerro bakoitzerako.
5. Eskaera `cobrada` egoerara aldatu, `total` kalkulatu eta `commit`.
6. Mahia automatikoki libre bihurtzen da (`MESAS_LISTA`-k eskaera irekiak soilik
   kontatzen dituelako okupatuta gisa).
7. Bezeroak `MenuaKargatu()` berriro deitzen du; `AGORTUTA` etiketa eguneratzen da.

Ticketa: `TICKET_OBTENER`-ek bezeroa, data, egoera, lerroak
(`izena`, `kopurua`, `prezioUnitarioa`) eta guztira itzultzen ditu;
`KontsumizioIkuspegia` `MessageBox` batean erakusten du.

## 9. Erroreen kudeaketa

- Zerbitzariak `{ arrakasta: false, errorea: "..." }` itzultzen du; bezeroak `TxtAbisua`
  (admin/reserbak) edo `MessageBox` batean erakusten du.
- Konexioa galduta: `Konexioa.Bidali`-ek `Itxi()` egiten du eta
  `Ezin izan da zerbitzariarekin konektatu. Egiaztatu konexioa eta saiatu berriro.` mezua itzultzen du.
- Zerbitzariaren kontsolan `[ERROR] <AKCIOA>: <mezua>` agertzen da.

## 10. Probak eta hedapena

`docs/PLAN.md` fitxategian proben zerrenda dago:
login, rol-guardak, erreserbak (data gabe/ekin, bikoiztuak, edukiera),
eskaera, izakin-murrizketa, ticket-a, logout,
**eskaeraren berreskuratzea (saioa itxi/ireki, gailu berean eta bestean),
jabetza-guardak, mahaiaren esleipena, mahaien mapa** eta 10 bezeroen muga.

TCP smoke testak zuzenean zerbitzariaren aurka exekutatu dira: eskaera ireki +
2 lerro + mahia esleitu → socket-a LOGOUT gabe itxi → berriro LOGIN →
`PEDIDO_REKUPERATU` eskaera bera, mahia eta 2 osagai itzuli ditu; bestelako
erabiltzaileari ukatuta, admin-ek bai; kobratu ondoren mahia librea.

Windows VMrako argitalpena:

```bash
dotnet publish BezeroaWPF/BezeroaWPF.csproj -c Release \
  -r win-x64 --self-contained true -o BezeroaWPF/publish_vm_x64
```

Kopiatu karpeta VM-ra, jarri `servidor.txt` (ostalariaren IP + `5001`) eta
exekutatu `AppTpv.exe`.

### Ezagutzen diren puntuak

- Zerbitzariaren `Cliente desconectado` log-mezua noizean behin desordenatuta
  agertzen da (kosmetikoa).
- `RESERVA_MODIFICAR`-ek ez du berriro egiaztatzen mahaiaren edukiera ez eta
  bikoiztutako erreserba; UI-tik erabiltzen den bidean ez da arazorik sortzen.
- `KopuruLeihoa.xaml` leihoa ez dago gaur egun deituta (ikus 5. atala).
