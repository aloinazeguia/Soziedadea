# AppTpv — Administratzailearen gida

Gida hau `admin` rolarekin logeatzen den pertsonarentzat da.
Erabiltzaile arrunt batek ez ditu hemen azaldutako pestañak ikusiko.

Interfazeak: `BezeroaWPF/Ikuspegiak/IzakinIkuspegia.xaml` eta
`BezeroaWPF/Ikuspegiak/ErabiltzaileIkuspegia.xaml` (biak `LeihoNagusia.xaml`-eko
`TabControl`-ean daude).

## Saioa hasi

1. Exekutatu `AppTpv.exe` (zerbitzaria martxan egon behar da).
2. `SaioHasieraLeihoa` leihoan idatzi:
   - **Erabiltzailea:** `admin`
   - **Pasahitza:** `admin123`
3. **Sartu** botoia sakatu.

Goiko barran zure identitatea ikusiko duzu, adibidez:
`Administrador (admin) — rol: admin`.
**Erabiltzaileak** eta **Izakinak** pestañak ikusgai daude. Erabiltzaile
arruntek bezala, **Kontsumizioak**, **Erreserbak** eta **Mahaiak** pestañak
ere ikusten dituzu.

Administratzaile gisa, edozein eskaera irekiren osagai-zerrenda berreskura
dezakezu: **Kontsumizioak** pestañan, `PEDIDO_REKUPERATU` ekintzak
`datuak.eskaeraId` emanda, edo zure azken eskaera irekia ikusteko daturik gabe.
Erabiltzaile arruntek, aldiz, soilik sortu zuten eskaera ikus dezakete.

---

## 1. Produktu berriak gehitu (**Izakinak** pestaña)

Formularioa ezkerrean dago (`IzakinIkuspegia.xaml`):

| Kontrola | Betegarria |
|---|---|
| `TxtIzena` | Produktuaren izena (beharrezkoa, bakarra) |
| `TxtPrezioa` | Prezioa eurotan (beharrezkoa, > 0) |
| `TxtIzakinak` | Hasierako izakinak (≥ 0) |
| `TxtOsagaiak` | **Osagaiak**, komaz bereizita (adib. `ogia, haragitxoa, gazta atxurra`) |
| `TxtKategoria` | Kategoria; hutsik uzten bada `general` ezartzen da |

Urratsak:

1. Bete eremuak.
2. Sakatu **Sortu**.
3. Mezua `TxtAbisua`-n agertzen da (`Producto creado` — zerbitzariaren mezu gaztelaniaz,
   lokalizazio-plana `docs/hizkuntzak.md`-en dago — edo errorea).
4. Taula (`GridProduktuak`) automatikoki freskatzen da.

> Osagaiak (`ingredientes`) eremuak erronkaren **Ikus Osagaiak** eskakizunari
> erantzuten dio: produktuaren osagaiak gorde eta gero erakusten dira.

---

## 2. Biltegiko izakinak gehitu / aldatu / ezabatu

### Izakinak kopurua aldatu (modu azkarra)

1. Hautatu produktua `GridProduktuak` taulan — formularioa automatikoki
   betetzen da.
2. Idatzi nahi den **kopuru osoa** `TxtIzakinBerriak` eremuan.
3. Sakatu **Ezarri izakinak**.

⚠️ Ekintza honek **balio absolutua** ezartzen du, ez gehikuntza/galera:
`12` idatziz gero izakinak `12` izango dira, `+12` ez.

### Produktuaren gainerako datuak aldatu

1. Hautatu produktua taulan.
2. Aldatu nahi dituzun eremuak (izena, prezioa, stocka, osagaiak, kategoria).
3. Sakatu **Gorde aldaketak**.

`PRODUCTO_MODIFICAR` ekintzak bidalitako eremuak soilik aldatzen ditu;
hutsik daudenak bezala uzten dira.

### Produktua katalogotik kendu

1. Hautatu produktua taulan.
2. Sakatu **Kendu**.
3. Berretsi `MessageBox`-eko galdera.

Ezabaketa **logikoa** da: `activo = 0` ezartzen da. Produktua ez da agertzen
kontsumoen kategorien zerrendan, baina lehendik dauden ticketen historia
mantentzen da. Izakinak `0` jartzea beste aukera bat da: orduan txartelak
**AGORTUTA** etiketa erakusten du baina kategorian ikusgai jarraitzen du.

### Errore ohikoenak

- `Aukeratu zerrendako produktu bat.` → ez duzu fila bat hautatu.
- `Izena, prezioa (> 0) eta izakinak (>= 0) nahitaezkoak dira.` → formularioa osatu gabe dago.
- `Produktua ez dago eskuragarri` → produktua desgaitu egin da artean.

---

## 3. Erabiltzaileak sortu / aldatu / ezabatu (**Erabiltzaileak** pestaña)

Formularioa (`ErabiltzaileIkuspegia.xaml`):

| Kontrola | Betegarria |
|---|---|
| `TxtErabiltzailea` | Saio-hasierako izena (bakarrik sortzean) |
| `TxtPasahitza` | Pasahitza (sortzean beharrezkoa; aldatzean, hutsik utziz gero ez da aldatzen) |
| `TxtIzena` | Izena |
| `CboRola` | `admin` edo `usuario` |
| `GridErabiltzaileak` | Erabiltzaileen zerrenda |

### Sortu

1. Idatzi erabiltzaile-izena, pasahitza eta izena.
2. Aukeratu rola.
3. Sakatu **Sortu**.
4. `TxtAbisua`-n `<erabiltzailea> erabiltzailea sortu da.` agertzen da eta taula freskatzen da.

### Aldatu

1. Hautatu erabiltzailea `GridErabiltzaileak`-en — izena eta rola formularioan
   agertzen dira (pasahitza garbitu egiten da, segurtasunagatik).
2. Aldatu izena, rola eta/edo pasahitza.
3. Sakatu **Gorde aldaketak**.

### Ezabatu (desgaitu)

1. Hautatu erabiltzailea taulan.
2. Sakatu **Desgaitu**.
3. Berretsi galdera.

Erabiltzailea `activo = 0` bihurtzen da: ezin da gehiago logeatu, baina
erregistroa mantentzen da. Ezin duzu zeure burua desgaitu
(`Ezin duzu zeure burua desgaitu.`).

### Errore ohikoenak

- `Erabiltzaile-izena, pasahitza eta izena nahitaezkoak dira.` → falta den eremua bete.
- `Erabiltzaile-izen hori badago lehendik` → login-a erabilita dago.
- `Aukeratutako rola ez da baliozkoa` → rola baliozkoa aukeratu.

---

## Gogoratu

- Ekintza guztiak zerbitzarian egiaztatzen dira berriro: bezeroak pestañak
  ezkutatzea ez da segurtasun-neurri bakarra. Eskaera irekietan jabetza
  egiaztatzen da (`Eskaera hori ez da zurea`); admin-ek soilik du salbuespena.
- Saioa itxi gabe socket-a galtzen bada ere, zerbitzariak tokena garbitzen du
  konexioa ixten denean; ez dira saio zaharrak pilatzen.
- Taularen goiko **Eguneratu** botoiak datuak birkargatzen ditu (bestelako
  bezero batek aldaketak egin baditu ikusteko).
- `TxtAbisua` eremuak errore-mezuak eta berrespenak erakusten ditu.
