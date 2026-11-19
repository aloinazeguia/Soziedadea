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
- [x] Haziak: 17 erabiltzaile (`admin` + 15 berri, pasahitza `1234`), 3 mahai (4/4/6 plaza), 21 produktu
- [x] Egiaztatuta: `dotnet run` → `BD OK -> erabiltzaileak=17 mahaiak=3 produktuak=21`

## 2. Fasea — Osatuta ✅ (2026/02/11)

### Zer dago eginda

- [x] `Sarea/TCPZerbitzaria.cs`: TcpListener **5001** atakan (5000 macOS-eko ControlCenter/AirPlay-ek okupatzen du),
      gehienez 10 bezero aldi berean (11.a `{"errorea":"SESSION_LIMIT"}` mezuarekin baztertzen da), bezero bakoitzeko hari bat
- [x] `Sarea/BezeroKonexioa.cs`: JSON framing-a (lerro bat = mezu bat), token/ekintzaren araberako banaketa, LOGOUT
- [x] `Zerbitzuak/Saioak.cs`: GUID tokena LOGIN ondoren, mapa konkurrentea, saio-kontagailua
- [x] `Zerbitzuak/EragiketaKudeatzailea.cs`: protokoloko **20 ekintza** rol-guardekin
      (`AdminEskatzenDu` / `SaioaEskatzenDu`), idazketa-lock globala + transakzioak
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

## Hurrengo geltokia — 3. Fasea

`BezeroaWPF`: enuntziatuko 5 interfazeak `docs/PROTOCOLO.md` kontratuaren gainean, Windows VM-an probatuta
(UTM); binarioak pasatzeko UTM karpeta partekatuak. Zerbitzariak dagoeneko betetzen du kontratua.
