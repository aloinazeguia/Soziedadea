# TCP protokoloa — AppTpv (2. fasea)

## Konexioa
- `TcpListener` **5001 atakan**.
- Zerbitzariak gehienez **10 bezero aldi berean** onartzen ditu (enuntziatua).
  11. bezeroak `SESSION_LIMIT` errorea jasotzen du.
- **Bezero bakoitzeko hari bat** (irakurketa blokeatzailea); bezeroaren UI-a ez da
  blokeatzen zerbitzariak erantzuten duen bitartean.
- Markoa: **lerro bat = mezu bat** (`\n` bereizgailua). JSON UTF-8.

## Mezuaren formatua

Eskaera:

```json
{ "ekintza": "LOGIN", "token": "", "datuak": { "erabiltzailea": "admin", "pasahitza": "admin123" } }
```

Erantzuna:

```json
{ "arrakasta": true, "datuak": { "token": "a1b2...", "rola": "admin", "izena": "Administrador" } }
```

```json
{ "arrakasta": false, "errorea": "Erabiltzaile-izena edo pasahitza ez dira zuzenak" }
```

Gakoen mapa (protokoloa euskaratu da):

| Jatorrizkoa | Orain |
|---|---|
| `accion` | `ekintza` |
| `datos` | `datuak` |
| `ok` | `arrakasta` |
| `error` | `errorea` |
| `login` | `erabiltzailea` |
| `pass` | `pasahitza` |
| `nombre` | `izena` |
| `rol` | `rola` |
| `precio` / `stock` | `prezioa` / `izakinak` |
| `ingredientes` / `categoria` / `activo` | `osagaiak` / `kategoria` / `aktibo` |
| `numMesa` / `capacidad` | `mahaiZenbakia` / `edukiera` |
| `idMesa` / `idReserva` | `mahaiId` / `erreserbaId` |
| `fecha` / `turno` / `numComensales` | `data` / `txanda` / `pertsonaKopurua` |
| `nombreCliente` / `observaciones` / `estado` | `bezeroIzena` / `oharrak` / `egoera` |
| `idPedido` / `idProducto` / `cantidad` | `eskaeraId` / `produktuId` / `kopurua` |
| `precioUnitario` / `mensaje` | `prezioUnitarioa` / `mezua` |
| `productos` / `usuarios` / `mesas` / `reservas` | `produktuak` / `erabiltzaileak` / `mahaiak` / `erreserbak` |
| `total` / `ticket` / `lineas` / `iva` | `guztira` / `txartela` / `lerroak` / `beza` |

`token` eta ekintza-kodeak (`LOGIN`, `PRODUCTOS_LISTA`, ...) identifikatzaile
egonkorrak dira eta ez dira aldatu. DBko rol-balioak ere (`admin`, `usuario`)
berdin mantentzen dira, erabiltzaile-interfazeak "Administratzailea" /
"Erabiltzailea" erakusten dituen arren.

## Saioa eta tokena
- `LOGIN`ek `token` (GUID) bat itzultzen du. Ondoren, mezu guztiek `"token"`
  daramate eta zerbitzariak harekin lotutako rola egiaztatzen du.
- Administratzaileak ekintza gehigarriak ditu (`USUARIO_*`, `STOCK_*`);
  erabiltzaile arrunt batek deituz gero:
  `{ "arrakasta": false, "errorea": "Ekintza hau administratzaileentzat bakarrik dago baimenduta" }`.

## Ekintzak

| Ekintza | Rola | Datuak | Itzultzen du |
|---|---|---|---|
| `LOGIN` | – | erabiltzailea, pasahitza | token, rola, izena |
| `PRODUCTOS_LISTA` | biak | – | produktuak (izakinak/osagaiak barne) |
| `PRODUCTO_AGREGAR / _MODIFICAR / _BORRAR` | admin | produktuaren datuak | arrakasta |
| `STOCK_MODIFICAR` | admin | id, izakinak | arrakasta |
| `USUARIOS_LISTA` | admin | – | erabiltzaileak |
| `USUARIO_CREAR / _MODIFICAR / _BORRAR` | admin | erabiltzailea, pasahitza, izena, rola... | arrakasta |
| `MESAS_LISTA` | saioa | – | mahaiak |
| `RESERVA_CREAR` | saioa | mahaiId, data (aukerakoa), txanda, pertsonaKopurua | erreserbaId |
| `RESERVA_MODIFICAR / _BORRAR` | saioa | erreserbaId + aldaketak | arrakasta |
| `RESERVA_LISTA` | saioa | data, txanda (aukerakoak) | erreserbak |
| `PEDIDO_ABRIR` | saioa | – | eskaeraId |
| `PEDIDO_AGREGAR_LINEA` | saioa | eskaeraId, produktuId, kopurua | prezioUnitarioa; izakinak egiaztatzen ditu |
| `PEDIDO_CONFIRMAR` | saioa | eskaeraId | guztira + izakinak murriztu (lock berean) |
| `PEDIDO_CANCELAR` | saioa | eskaeraId | arrakasta |
| `TICKET_OBTENER` | saioa | eskaeraId | txartela (lerroak + guztira) |
| `LOGOUT` | saioa | – | saioa itxi (bereziki kudeatzen da) |

## Aldiberekotasuna
- **Lock global bakarra** zerbitzarian DB idazketa-sekuentzia guztien inguruan
  (eskaera sortu, lerroak, izakin-murrizketa, kontagailuak).
- SQLite WAL moduan, IRURKETAK lock gabe paraleloan egin daitezke.
- ≤10 bezero eta idazketa laburrekin ez da taula bakoitzeko lock-ik behar
  (enuntziatuak berak baimentzen du), eta horrela deadlock-ik ez da sortzen.
