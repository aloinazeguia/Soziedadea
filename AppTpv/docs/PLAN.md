# PLANA — AppTpv (WID+ZPP Erronka)

## 1. Fasea — Osatuta ✅ (2026/02/11)

- [x] `AppTpv.slnx` soluzioa `ZerbitzariaTPV` proiektuarekin (kontsola, net10.0)
- [x] `Microsoft.Data.Sqlite` paketea integratuta
- [x] SQLite eskema osoa (`Datuak/Eskema.sql`), erronkaren baldintza GUZTIAK estaltzen dituena:
  - Usuario/rol (admin | usuario) + passHash
  - Mesa, Reserva (`fecha` NULL = data gabe, `turno` bazkaria/afaria)
  - Producto **stock** eta **ingredientes** (osagaiak) eremuekin
  - Pedido/LineaPedido (ticket-a + kobratzean stocka murriztu)
- [x] `DatuBasea.Hasieratu()`-ek BD bera sortzen du lehen abiaraztean
- [x] Idazketen lock globala (`IdazketaBlokeoa`) prest (enuntziatuaren konkurrentzia-eskakizuna)
- [x] Haziak: 17 erabiltzaile (`admin` + 15 berri, pasahitza `1234`), 7 mahai (4 laguneko 2, 6 laguneko 2, 10 laguneko 3), 21 produktu
- [x] Egiaztatuta: `dotnet run` → `BD OK -> erabiltzaileak=17 mahaiak=7 produktuak=21`

## 2. Fasea — Osatuta ✅ (2026/02/11)

### Zer dago eginda

- [x] `Sarea/TCPZerbitzaria.cs`: TcpListener **5001** atakan (5000 macOS-eko ControlCenter/AirPlay-ek okupatzen du),
      gehienez 10 bezero aldi berean (11.a `{"errorea":"SESSION_LIMIT"}` mezuarekin baztertzen da), bezero bakoitzeko hari bat
- [x] `Sarea/BezeroKonexioa.cs`: JSON framing-a (lerro bat = mezu bat), token/ekintzaren araberako banaketa, LOGOUT
- [x] `Zerbitzuak/Saioak.cs`: GUID tokena LOGIN ondoren, mapa konkurrentea, saio-kontagailua
- [x] `Zerbitzuak/EragiketaKudeatzailea.cs`: protokoloko **20 ekintza** rol-guardekin
      (`AdminEskatzenDu` / `SaioaEskatzenDu`), idazketa-lock globala + transakzioak
      (gerora 2 ekintza berri gehitu dira: `PEDIDO_REKUPERATU` eta `PEDIDO_MAHAIA_ESLEITU`)
- [x] `Programa.cs`-ek BD + TCP abiarazten ditu; `parar` komandoa ixteko

### Probekin zuzendutako akatsak

- `Eskema.sql`-eko FK okerrak (`Mesa(idMesa)`/`Usuario(idUsuario)` → `...id`):
  `foreign key mismatch` eragiten zuten Reserva eta Pedido sortzean
- `INSERT ... ExecuteScalar()`-ek 0 itzultzen zuen → `RETURNING id` erabiltzen da `Erres_Sortu` eta `Ped_Abrir`-en

### Proben frogak (node TCP bezeroa, 40 urratseko suitea)

- LOGIN zuzena/okerra, USUARIO_CREAR (+bikoiztua, +rol okerra), USUARIOS_LISTA
- Rol-guardak: "Ekintza hau administratzaileentzat bakarrik dago baimenduta" erabiltzaile arruntarentzat
- Tokenik gabeko ekintzak: "Ez dago saio aktiborik (tokena falta da)"
- MESAS_LISTA / PRODUCTOS_LISTA erabiltzaile arruntarekin
- RESERVA_CREAR: data gabe ✅, datarekin ✅, bikoiztua baztertuta ✅, edukiera baztertuta
  ("Mahaiak gehienez 4 lagun onartzen ditu"), txanda okerra, data-formatu okerra
- RESERVA_LISTA (guztira eta dataren arabera iragazita), MODIFICAR, BORRAR (cancelada), beste batenak/id gabe
- PEDIDO_ABRIR, AGREGAR_LINEA (+izakin nahiko ez: "N unitate geratzen dira"), produktu inexistentea
- PEDIDO_CONFIRMAR: guztira 30 € ✅ eta **izakin-murrizketa egiaztatuta**
- Kobratu ondoren AGREGAR baztertuta, bigarren eskaera, "Zure 2 zenbakiko eskaera irekita dago", cancelar
- TICKET_OBTENER: bezeroa, data, `cobrada` egoera, lerroak, guztira ✅
- LOGOUT eta logout ondorengo ekintza baztertuta
- 10 bezeroen muga: 11 koneksioko script-a → 11.a `SESSION_LIMIT` erantzunarekin baztertuta

### Probekin erabilitako BD fitxategia

```
AppTpv/ZerbitzariaTPV/bin/Debug/net10.0/data/tpv.db   (ezabatuz gero haziekin birsortzen da)
```

### Ezagutzen diren zain egongoak (ez blokeatzaileak)

- "Bezeroa deskonektatuta" log-mezua batzuetan desordenatuta agertzen da (kontagailua hariren artean irakurtzen da): kosmetikoa
- RESERVA_MODIFICAR-ek ez du edukiera/bikoiztua berriro baliozkotzen mahia/data aldatzean: UI-ak eskatuz gero landu
- BDren babeskopiak eta kredentzialen dokumentua: azken entregarako

## 3. Fasea — Osatuta ✅ (2026/11/19)

Eskaeraren iraunkortasuna eta mahaien mapa:

- [x] `Pedido.idMesa` zutabe nullable + FK `Mesa(id)` (`Eskema.sql`) eta
      `DatuBasea.Hasieratu()`-eko migrazio idempotentea (datuak galdu gabe)
- [x] `PEDIDO_REKUPERATU`: ordaindu gabeko eskaera eta haren osagai-zerrenda
      BDtik itzultzen ditu. `KontsumizioIkuspegia.Loaded`-en deitzen da: saioa
      itxi/ireki ondoren —gailu berean nahiz bestean— osagaiak berriro agertzen dira
- [x] `PEDIDO_MAHAIA_ESLEITU` + `CboMahaiak`/`BtnMahiaEsleitu` kontsumizio-fitxan
- [x] `MahaiMapaIkuspegia` 6. interfazea `LeihoNagusia`-n fitxa gisa (7 mahai,
      `librea`/`erreserbatuta`/`okupatuta` + eskaera zenbakia, **Freskatu** botoia)
- [x] `MESAS_LISTA` luzatu da: `erreserbak`, `eskaeraId` eta `egoera` gako berriak,
      lehenengo hiru gakoak (`id`, `mahaiZenbakia`, `edukiera`) mantenduz
- [x] Jabetza-guardak: eskaera irekiak sortzaileak/admin-ek soilik ikusi/aldatu
      (`Eskaera hori ez da zurea`), `TICKET_OBTENER` barne
- [x] `BezeroKonexioa.Artatu()`-ren `try/finally`: socket-a ixten denean tokenak
      garbitu, LOGOUT esplizitua ez badago ere (10 bezeroen muga aldatu gabe)
- [x] Dokumentazio osoa eguneratu: `PROTOCOLO.md`, gidak, `README.md`, `PLAN.md`
      eta `Proiektua.pdf`

### Proben frogak (TCP smoke testak, 2026/11/19)

- Eskaera ireki + 2 lerro + 5. mahaiari esleitu → `MESAS_LISTA`-k `okupatuta` + eskaera zenbakia
- Socket-a **LOGOUT gabe** itxi → berriro LOGIN → `PEDIDO_REKUPERATU`: eskaera bera,
  mahia eta 2 osagai (izena, kopurua, prezioa) itzuli dira
- Bestelako erabiltzaileari berreskuratzea eta tiketa ukatuta; admin-ek bai
- `PEDIDO_CONFIRMAR` ondoren mahia automatikoki `librea`
- `[SESION] Konexioa itxita: 1 token garbituta` log-mezua egiaztatuta
- `dotnet build AppTpv.slnx --no-incremental` → 0 abisu, 0 errore
- Proben ondoren BD jatorrizko egoerara itzuli da (3 eskaera: 1 `cobrada`, 2 `anulada`;
  0 eskaera ireki, 0 `idMesa` esleituta, 7 mahai)

## Hurrengo geltokia

Ez dago zeregin irekirik. Defentsan erakusteko, `docs/AURKEZPENA_...md` eta
`docs/Proiektua.pdf` erabil daitezke.
