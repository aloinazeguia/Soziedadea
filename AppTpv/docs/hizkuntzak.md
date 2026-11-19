# AppTpv — Hizkuntzak (EU / ES)

Dokumentu honek aplikazioa **euskara (eu)** eta **gaztelania (es)** artean
aldatzeko behar den lana deskribatzen du. Orain UI-a euskaraz dago tinkatuta;
helburua da erabiltzaileak hizkuntza saioan zehar alda dezakeela, aplikazioa
berrabiarazi gabe.

> **Oharra:** dokumentu hau plan bat da. Oraindik ez da inplementatu.
> Protokoloa, datu-basea eta fitxategi-izenak ez dira aldatu behar.

---

## 1. Egoera actuala

| Geruza | Orain | Lokalizatu daiteke? |
|---|---|---|
| XAML (leiho, ikuspegi, kontrolak) | Testuak `Content`, `Text`, `Header`, `Title` propertietan tinkatuta, euskaraz | **Bai** |
| Kodea (`.xaml.cs`) | `MessageBox.Show`, `TxtAbisua`, `TxtErrorea` mezuak | **Bai** |
| Zerbitzaria (`EragiketaKudeatzailea.cs`) | Mezu batzuk euskaraz eta beste batzuk gaztelaniaz (`Producto creado`, `Reserva creada`, ...) | **Bai** (ikus 5. atala) |
| TCP protokoloa | `ekintza`, `datuak`, `arrakasta`, `errorea`, `LOGIN`, `PEDIDO_ABRIR`, `token` | **Ez** — identifikatzaileak dira |
| Datu-basea | Taulen/zutabeen izenak; `admin`/`usuario`, `activa`/`cancelada`, `abierta`/`cobrada` | **Ez** — balio egonkorrak dira |
| Produktuen kategoriak | DBko datuak (`Kafeak`, `Ardo Beltzak`, ...) | Aukeran: itzulpen-mapa gehitu (ikus 6. atala) |

---

## 2. Zer lokalizatu eta zer mantendu

| Lokalizatu behar da | Mantendu behar da |
|---|---|
| Botoien eta etiketen testuak (`Sartu`, `Amaitu saioa`, `Sortu`, ...) | JSON gakoak (`ekintza`, `datuak`, ...) |
| Errore-mezuak eta berrespenak | Ekintza-kodeak (`LOGIN`, `RESERVA_CREAR`, ...) |
| Leihoen izenburuak, pestañak, zutabeen goiburuak | `token`, `SESSION_LIMIT` |
| `MessageBox` testuak | Rol-balioak: `admin`, `usuario` |
| Kategoria-izenak (DB datuak badira, mapa baten bidez) | SQL taulen/zutabeen izenak, fitxategi-izenak |

**Arau nagusia:** protokoloa eta datu-basea hizkuntzarekiko independenteak dira.
Hizkuntza aldaketa **bezeroan** gertatzen da; zerbitzariak ez du hizkuntza
jakin beharrik, 5. atalean bestela erabakiz gero.

---

## 3. Proposatutako arkitektura (gomendatua): `ResourceDictionary`

WPF-en modu sinpleena `DynamicResource` erabiltzea da: hizkuntza aldatzean
erreferentziak automatikoki eguneratzen dira, leihoak berriro sortu gabe.

### Fitxategi berriak

```
AppTpv/BezeroaWPF/Baliabideak/HizkuntzakEU.xaml
AppTpv/BezeroaWPF/Baliabideak/HizkuntzakES.xaml
AppTpv/BezeroaWPF/Zerbitzuak/HizkuntzaKudeatzailea.cs
```

### Hizkuntza-dictionaryen adibidea

`HizkuntzakEU.xaml`:

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:sys="clr-namespace:System;assembly=mscorlib">
    <sys:String x:Key="Sartu">Sartu</sys:String>
    <sys:String x:Key="AmaituSaioa">Amaitu saioa</sys:String>
    <sys:String x:Key="Sortu">Sortu</sys:String>
    <sys:String x:Key="GordeAldaketak">Gorde aldaketak</sys:String>
    <sys:String x:Key="Eguneratu">Eguneratu</sys:String>
    <sys:String x:Key="Erabiltzailea">Erabiltzailea</sys:String>
    <sys:String x:Key="Pasahitza">Pasahitza</sys:String>
    <sys:String x:Key="ErroreaSaioa">Erabiltzaile-izena edo pasahitza ez dira zuzenak</sys:String>
    <sys:String x:Key="Agortuta">AGORTUTA</sys:String>
    <sys:String x:Key="Kobratu">KOBRATU</sys:String>
    <sys:String x:Key="IkusiTxartela">IKUSI TIKETA</sys:String>
</ResourceDictionary>
```

`HizkuntzakES.xaml`: gako berberak, balioak gaztelaniaz (adib. `Entrar`,
`Cerrar sesión`, `Crear`, `Guardar cambios`, `Actualizar`, `Usuario`,
`Contraseña`, `Usuario o contraseña incorrectos`, `AGOTADO`, `COBRAR`,
`VER TICKET`).

### XAML erabilera

```xml
<Button x:Name="BtnSartu" Content="{DynamicResource Sartu}"
        Click="BtnSartu_Click" IsDefault="True"/>
<TextBlock Text="{DynamicResource Erabiltzailea}"/>
```

### Aldaketa kodean

```csharp
public static class HizkuntzaKudeatzailea
{
    public static string Hizkuntza { get; private set; } = "eu";

    public static void Ezarri(string kodea)   // "eu" | "es"
    {
        Hizkuntza = kodea == "es" ? "es" : "eu";
        var hiztegia = new ResourceDictionary
        {
            Source = new Uri(Hizkuntza == "es"
                ? "Baliabideak/HizkuntzakES.xaml"
                : "Baliabideak/HizkuntzakEU.xaml", UriKind.Relative)
        };
        var bilduma = Application.Current.Resources.MergedDictionaries;
        bilduma.Clear();
        bilduma.Add(hiztegia);
        Properties.Settings.Default.Hizkuntza = Hizkuntza;
        Properties.Settings.Default.Save();
    }

    public static string Testua(string gakoa) =>
        Application.Current.Resources[gakoa] as string ?? gakoa;
}
```

Kodeko mezuak `Testua("ErroreaSaioa")` bidez eskuratzen dira.

### Non kargatu

- `App.xaml.cs`: `Startup` gertaeran `HizkuntzaKudeatzailea.Ezarri(gordetakoa)`.
- `LeihoNagusia.xaml`: goiko barran hizkuntza aldatzeko `ComboBox` edo
  `Menu` (`EU` / `ES`); hautatzean `Ezarri()` deitu.
- `hizkuntza.txt` fitxategia `servidor.txt`-en antzera erabil daiteke,
  lehenespenez `eu` izanik.

---

## 4. Aldatu beharreko fitxategien zerrenda

**Berriak:** 3 fitxategi (3. atalekoak).

**Aldatu beharrekoak:**

| Fitxategia | Zer aldatu |
|---|---|
| `App.xaml` / `App.xaml.cs` | Hizkuntza-dictionarya kargatu abiaraztean |
| `Leihoak/SaioHasieraLeihoa.xaml(.cs)` | Etiketak, `BtnSartu`, errore-mezua |
| `Leihoak/LeihoNagusia.xaml(.cs)` | Pestañak, `Amaitu saioa`, hizkuntza-hautatzailea |
| `Leihoak/KopuruLeihoa.xaml(.cs)` | Izenburua, `Onartu`/`Utzi`, `MessageBox` |
| `Ikuspegiak/KontsumizioIkuspegia.xaml(.cs)` | Kategoriak, botoiak, `AGORTUTA`, `KOBRATU`, `IKUSI TIKETA`, txartela |
| `Ikuspegiak/ErreserbaIkuspegia.xaml(.cs)` | Formularioa, botoiak, zutabe-goiburuak, `TxtAbisua` |
| `Ikuspegiak/IzakinIkuspegia.xaml(.cs)` | Formularioa, botoiak, erroreak |
| `Ikuspegiak/ErabiltzaileIkuspegia.xaml(.cs)` | Formularioa, rola: `Administratzailea`/`Erabiltzailea` → `Administrador`/`Usuario` |
| `Zerbitzuak/Ereduak.cs` | `RolErakutsi`/`AktiboErakutsi` propietateak hizkuntzaren arabera |

Gutxi gorabehera **120–150 testu-kate** daude bi hizkuntzetan idazteko.

---

## 5. Zerbitzariaren mezuak: bi aukera

Gaur egun zerbitzariak mezu mistoak bidaltzen ditu (`errorea` / `mezua` gakoen
bidez). Hizkuntza aldatzeko bi bide daude:

### A) Bezeroan mapatu (gomendatua)

Zerbitzariak mezu-**kode** egonkorrak bidaltzen ditu, adibidez
`ERABILTZALEA_OKERRA`, `IZAKIN_EZ_NAHIKO`, `ERRESERBA_SORTUTA`, eta
bezeroak EU/ES testuak aukeratzen ditu. `errorea`/`mezua` gakoen bidez
gizaki-irakur daitezkeen testuak bidaltzen jarrai daitezke, *fallback* gisa.

- **Abantaila:** protokoloa egonkorra da; zerbitzaria ez da aldatzen ehunka mezutarako.
- **Eragozpena:** kode berriak gehitu behar dira mezu bakoitzeko.

### B) Zerbitzarian `.resx` fitxategiak

`ZerbitzariaTPV/Baliabideak/MezuakEU.resx` eta `MezuakES.resx`, eskaeran
`"hizkuntza": "es"` eremua gehituta; `EragiketaKudeatzailea`-k
`CultureInfo` bidez formatuatzen ditu mezuak.

- **Abantaila:** mezuak leku bakarrean.
- **Eragozpena:** protokoloa aldatu behar da (`docs/PROTOCOLO.md` eguneratu),
  eta zerbitzariak hizkuntza-ezarpena bezeroarekin partekatu behar du.

Gomendioa: **A**, geroago B behar izanez gero gehitu daiteke.

---

## 6. Produktuen kategoriak

Kategoriak (`Kafeak`, `Freskagarriak`, `Garagardoa`, `Ardo Beltzak`,
`Ardo Txuriak`) datu-basean gordetzen dira, beraz ez dira UI-testu arruntak.
Bi hizkuntzetan erakusteko:

1. **Mapa sinplea** bezeroan: `KategoriaItzulpenak["Ardo Beltzak"] = "Vinos tintos"`;
   DBko balioa aldatu gabe erakusten da itzulpena.
2. Edo DBko `Categoria` taulari `izenaEu`/`izenaEs` zutabeak gehitu (eskema-aldaketa;
   ez da gomendatzen oraingo fasean).

---

## 7. Probak

- [ ] `EU` → `ES` aldatzean, 5 interfazeetako testuak berehala eguneratzen dira.
- [ ] Hizkuntza hautaketa gorde egiten da (`hizkuntza.txt` / `Settings`) eta
      hurrengo abiaraztean mantentzen da.
- [ ] `RolErakutsi`: `Administratzailea`/`Erabiltzailea` ↔ `Administrador`/`Usuario`,
      baina DBko `admin`/`usuario` balioak ukitu gabe.
- [ ] Errore-mezuak (`Login`, izakinak, edukiera) bi hizkuntzetan agertzen dira.
- [ ] TCP smoke-testak gako berriekin berdin functionatzen du:
      `ekintza`, `datuak`, `arrakasta`, `errorea` aldatu gabe.
- [ ] Txartelaren inprimaketa/ikuspegia bi hizkuntzetan zuzen ikusten da.

---

## 8. Resumen en español

El objetivo es permitir cambiar la interfaz entre **euskera** y **español**
sin reiniciar la aplicación. La forma recomendada es usar `ResourceDictionary`
por idioma (`HizkuntzakEU.xaml` / `HizkuntzakES.xaml`) con `DynamicResource`
en el XAML y un servicio `HizkuntzaKudeatzailea` que guarda la preferencia.
Hay que traducir unos 120–150 textos de ventanas, vistas y mensajes.

**No se deben traducir** las claves JSON del protocolo (`ekintza`, `datuak`,
`arrakasta`, `errorea`), los códigos de acción (`LOGIN`, `PEDIDO_ABRIR`, ...),
el token, los nombres de tablas/columnas ni los valores de la base de datos
(`admin`, `usuario`, `activa`, `cobrada`). Las categorías de productos son
datos de la BD: se pueden mostrar traducidos mediante un mapa en el cliente
sin modificar la base de datos.

Para los mensajes del servidor se recomienda devolver códigos estables y
mapearlos en el cliente (opción A); la alternativa es añadir recursos `.resx`
en el servidor y un campo `hizkuntza` en la petición (opción B).

Tras implementarlo, hay que verificar que el cambio de idioma actualiza las
cinco interfaces al instante, que la preferencia persiste y que las pruebas
TCP siguen funcionando con las mismas claves.
