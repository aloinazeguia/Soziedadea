# AppTpv — Administratzailearen gida

Gida hau `admin` rolarekin logeatzen den pertsonarentzat da.
Erabiltzaile arrunt batek ez ditu hemen azaldutako pestañak ikusiko.

Interfazeak: `ClienteWPF/Vistas/VistaStock.xaml` eta
`ClienteWPF/Vistas/VistaUsuarios.xaml` (biak `VentanaPrincipal.xaml`-eko
`TabControl`-ean daude).

## Saioa hasi

1. Exekutatu `AppTpv.exe` (zerbitzaria martxan egon behar da).
2. `VentanaLogin` leihoan idatzi:
   - **Usuario:** `admin`
   - **Contrasena:** `admin123`
3. **Entrar** botoia sakatu.

Goiko barran zure identitatea ikusiko duzu, adibidez:
`Administrador (admin) — rol: admin`.
**Usuarios** eta **Stock** pestañak ikusgai daude.

---

## 1. Produktu berriak gehitu (**Stock** pestaña)

Formularioa ezkerrean dago (`VistaStock.xaml`):

| Kontrola | Betegarria |
|---|---|
| `TxtNombre` | Produktuaren izena (beharrezkoa, bakarra) |
| `TxtPrecio` | Prezioa eurotan (beharrezkoa, > 0) |
| `TxtStock` | Hasierako stocka (≥ 0) |
| `TxtIngredientes` | **Osagaiak**, komaz bereizita (adib. `ogia, haragitxoa, gazta atxurra`) |
| `TxtCategoria` | Kategoria; hutsik uzten bada `general` ezartzen da |

Urratsak:

1. Bete eremuak.
2. Sakatu **Crear**.
3. Mezua `TxtAviso`-n agertzen da (`Producto creado.` edo errorea).
4. Taula (`GridProductos`) automatikoki freskatzen da.

> Osagaiak (`ingredientes`) eremuak erronkaren **Ikus Osagaiak** eskakizunari
> erantzuten dio: produktuaren osagaiak gorde eta gero erakusten dira.

---

## 2. Biltegiko stocka gehitu / aldatu / ezabatu

### Stock kopurua aldatu (modu azkarra)

1. Hautatu produktua `GridProductos` taulan — formularioa automatikoki
   betetzen da.
2. Idatzi nahi den **kopuru osoa** `TxtNuevoStock` eremuan.
3. Sakatu **Poner stock**.

⚠️ Ekintza honek **balio absolutua** ezartzen du, ez gehikuntza/galera:
`12` idatziz gero stocka `12` izango da, `+12` ez.

### Produktuaren gainerako datuak aldatu

1. Hautatu produktua taulan.
2. Aldatu nahi dituzun eremuak (izena, prezioa, stocka, osagaiak, kategoria).
3. Sakatu **Guardar cambios**.

`PRODUCTO_MODIFICAR` ekintzak bidalitako eremuak soilik aldatzen ditu;
hutsik daudenak bezala uzten dira.

### Produktua katalogotik kendu

1. Hautatu produktua taulan.
2. Sakatu **Quitar**.
3. Berretsi `MessageBox`-eko galdera.

Ezabaketa **logikoa** da: `activo = 0` ezartzen da. Produktua ez da agertzen
kontsumoen kategorien zerrendan, baina lehendik dauden ticketen historia
mantentzen da. Stocka `0` jartzea beste aukera bat da: orduan txartelak
**AGOTADO** etiketa erakusten du baina kategorian ikusgai jarraitzen du.

### Errore ohikoenak

- `Falta el id del producto` → ez duzu fila bat hautatu.
- `Nombre y precio son obligatorios (precio > 0)` → formularioa osatu gabe dago.
- `Producto no encontrado` → produktua ezabatu egin da artean.

---

## 3. Erabiltzaileak sortu / aldatu / ezabatu (**Usuarios** pestaña)

Formularioa (`VistaUsuarios.xaml`):

| Kontrola | Betegarria |
|---|---|
| `TxtLogin` | Saio-hasierako izena (bakarrik sortzean) |
| `TxtPass` | Pasahitza (sortzean beharrezkoa; aldatzean, hutsik utziz gero ez da aldatzen) |
| `TxtNombre` | Izena |
| `CboRol` | `admin` edo `usuario` |
| `GridUsuarios` | Erabiltzaileen zerrenda |

### Sortu

1. Idatzi login, pasahitza eta izena.
2. Aukeratu rola.
3. Sakatu **Crear**.
4. `TxtAviso`-n `Usuario <login> creado.` agertzen da eta taula freskatzen da.

### Aldatu

1. Hautatu erabiltzailea `GridUsuarios`-en — izena eta rola formularioan
   agertzen dira (pasahitza garbitu egiten da, segurtasunagatik).
2. Aldatu izena, rola eta/edo pasahitza.
3. Sakatu **Guardar cambios**.

### Ezabatu (desgaitu)

1. Hautatu erabiltzailea taulan.
2. Sakatu **Deshabilitar**.
3. Berretsi galdera.

Erabiltzailea `activo = 0` bihurtzen da: ezin da gehiago logeatu, baina
erregistroa mantentzen da. Ezin duzu zeure burua desgaitu
(`No puedes deshabilitarte a ti mismo.`).

### Errore ohikoenak

- `Login, contrasena y nombre son obligatorios.` → falta den eremua bete.
- `Ya existe ...` → login-a erabilita dago.
- `El rol debe ser 'admin' o 'usuario'` → rola baliozkoa aukeratu.

---

## Gogoratu

- Ekintza guztiak zerbitzarian egiaztatzen dira berriro: bezeroak pestañak
  ezkutatzea ez da segurtasun-neurri bakarra.
- Taularen goiko **Refrescar** botoiak datuak birkargatzen ditu (bestelako
  bezero batek aldaketak egin baditu ikusteko).
- `TxtAviso` eremuak errore-mezuak eta berrespenak erakusten ditu.
