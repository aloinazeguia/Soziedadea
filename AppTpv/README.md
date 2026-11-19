# AppTpv — Elkarte gastronomikorako TPV sistema

WID + ZPP **1. Ebaluaketako Erronka**rako garatutako salmenta-puntu (TPV) sistema.
Bezero-zerbitzari arkitektura, TCP konexioa eta SQLite datu-basea erabiltzen ditu.
Bezeroaren interfazeak WPF/XAML-ekin daude eginda (`BezeroaWPF` proiektua).

## Egitura

| Karpeta / fitxategia | Edukia |
|---|---|
| `AppTpv.slnx` | Soluzioa: bi proiektu |
| `ZerbitzariaTPV/` | Zerbitzaria: TCP, saioak, negozio-logika, SQLite |
| `BezeroaWPF/` | Bezeroa: 5 interfazeak XAML-en (`Leihoak/`, `Ikuspegiak/`) |
| `docs/PROTOCOLO.md` | TCP protokoloa (gako euskaldunak) |
| `docs/PLAN.md` | Garapen-plana eta proben frogak |
| `docs/GIDA_TEKNIKOA.md` | **Gida teknikoa** |
| `docs/GIDA_ADMIN.md` | **Administratzailearen gida** |
| `docs/GIDA_ERABILTZAILEA.md` | **Erabiltzailearen gida** |

## Beharrizanak

- **.NET 10 SDK** (`dotnet --version` → `10.0.x`).
- Zerbitzaria edozein plataformatan exekuta daiteke (macOS, Linux, Windows).
- Bezeroa **Windows**-en exekutatzeko diseinatuta dago (WPF). macOS-etik konpila
  eta argitara daiteke, `EnableWindowsTargeting` propietateari esker.

## Martxan jartzea

### 1. Zerbitzaria abiarazi

```bash
cd AppTpv
dotnet run --project ZerbitzariaTPV/ZerbitzariaTPV.csproj
```

Kontsolan honelako mezuak ikusi behar dira:

```
BD OK -> erabiltzaileak=17 mahaiak=3 produktuak=21
[TCP] 5001 atakan entzuten (gehienez 10 bezero)
Zerbitzaria martxan. Idatzi 'parar' ixteko.
```

Gelditzeko: idatzi `parar` + Enter, edo `Ctrl+C`.

### 2. Bezeroa konpilatu / argitaratu

```bash
# Soluzio osoa konpilatu (egiaztatuta: 0 errore, 0 abisu)
dotnet build AppTpv.slnx -c Release

# Windows VM-rako paketea sortu (self-contained, x64)
dotnet publish BezeroaWPF/BezeroaWPF.csproj -c Release \
  -r win-x64 --self-contained true -o BezeroaWPF/publish_vm_x64
```

`BezeroaWPF/publish_vm_x64/` karpeta osoa Windowsera kopiatu eta `AppTpv.exe`
exekutatu. (Dagoeneko badago `publish_vm/` eta `publish_vm_x64/` karpetak.)

### 3. Zerbitzariaren helbidea konfiguratu

Bezeroak lehenespenez `127.0.0.1:5001` erabiltzen du. Beste makina batean
(adibidez, UTM Windows VM batean ostalari macOS-a dela) `servidor.txt` fitxategia
jarri `AppTpv.exe`-rekin batera, lehenengo lerroan `IP:ATAKA`:

```
192.168.64.1:5001
```

### 4. Saioa hasi

`SaioHasieraLeihoa` leihoan:

- **Erabiltzailea:** `admin`
- **Pasahitza:** `admin123`

Horrez gain, 15 erabiltzaile sortu berri daude (`pasahitza: 1234`), adibidez
`ane.azkue`. Erregistro horien rola datu-basean `usuario` da; interfazeak
`Erabiltzailea` erakusten du.

Bezeroak `LeihoNagusia` leihoa irekitzen du; bertan rolaren arabera
pestaña batzuk ikusgai egongo dira.

## Datu-basea

- Motorra: **SQLite** (`Microsoft.Data.Sqlite`), fitxategia:
  - Debug: `ZerbitzariaTPV/bin/Debug/net10.0/data/tpv.db`
  - Release: `ZerbitzariaTPV/bin/Release/net10.0/data/tpv.db`
- Lehen abiaraztean `ZerbitzariaTPV/Datuak/Eskema.sql`-etik sortzen da automatikoki.
- Hasierako datuak: 17 erabiltzaile (`admin` + 15 erabiltzaile berri, pasahitza
  `1234`), 3 mahai (4/4/6 plaza) eta 21 produktu (kafeak, freskagarriak,
  garagardoa eta ardoak; lehendik zeuden 4 produktuak ere mantendu dira).
- SQLite fitxategiak ez du berezko erabiltzaile/pasahitzik; sarbidea aplikazioak
  kontrolatzen du. Pasahitzak **SHA-256 + gatz finkoa** (`AppTpv::ErronkaWIDZPP::2026`)
  gordetzen dira `Usuario.passHash` zutabean.

**Backupa:** zerbitzaria geldituta, kopiatu `data/tpv.db` fitxategia. Zerbitzaria
martxan badago, erabili SQLite-ren backup mekanismoa (WAL modua dago aktibatuta).

## Erronkaren baldintzak non dauden

| Erronkaren eskakizuna | Non |
|---|---|
| 1.1 Login interfazea | `BezeroaWPF/Leihoak/SaioHasieraLeihoa.xaml` |
| 1.2 Erabiltzaileen CRUD | `BezeroaWPF/Ikuspegiak/ErabiltzaileIkuspegia.xaml` |
| 1.3 Stockaren CRUD | `BezeroaWPF/Ikuspegiak/IzakinIkuspegia.xaml` |
| 1.4 Interfaze nagusia | `BezeroaWPF/Ikuspegiak/KontsumizioIkuspegia.xaml` |
| 1.5 Mahai-erreserbak (bazkaria/afaria) | `BezeroaWPF/Ikuspegiak/ErreserbaIkuspegia.xaml` |
| 2. Rolak (admin / erabiltzailea) | `ZerbitzariaTPV/Zerbitzuak/EragiketaKudeatzailea.cs` |
| 3. TCP, 10 bezero, hari bana | `ZerbitzariaTPV/Sarea/TCPZerbitzaria.cs` |
| 3. Idazketen lock bakarra | `ZerbitzariaTPV/Datuak/DatuBasea.cs` (`LockEscritura`) |
| 4. Ikus Osagaiak erreserba/kontsumoetan | XAML-eko `ComboBox`, `ListBox`, `DataGrid`, txartel-botoiak |
| 4. Ticketa + stock eguneratzea | `KontsumizioIkuspegia.xaml.cs` + `Ped_Confirmar` |

## Dokumentazio gehiago

- [Gida teknikoa](docs/GIDA_TEKNIKOA.md)
- [Administratzailearen gida](docs/GIDA_ADMIN.md)
- [Erabiltzailearen gida](docs/GIDA_ERABILTZAILEA.md)
- [Aurkezpena: zerbitzuak, prozesuak, hariak eta socketak](docs/AURKEZPENA_ZERBITZUAK_PROZESUAK_HARIAK_SOCKETAK.md)
- [Hizkuntza-planaren aurkezpena](docs/hizkuntzak.md)
