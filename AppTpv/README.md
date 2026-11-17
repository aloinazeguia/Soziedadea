# AppTpv — Elkarte gastronomikorako TPV sistema

WID + ZPP **1. Ebaluaketako Erronka**rako garatutako salmenta-puntu (TPV) sistema.
Bezero-zerbitzari arkitektura, TCP konexioa eta SQLite datu-basea erabiltzen ditu.
Bezeroaren interfazeak WPF/XAML-ekin daude eginda (`ClienteWPF` proiektua).

## Egitura

| Karpeta / fitxategia | Edukia |
|---|---|
| `AppTpv.slnx` | Soluzioa: bi proiektu |
| `ServidorTPV/` | Zerbitzaria: TCP, saioak, negozio-logika, SQLite |
| `ClienteWPF/` | Bezeroa: 5 interfazeak XAML-en (`Ventanas/`, `Vistas/`) |
| `docs/PROTOCOLO.md` | TCP protokoloa (gaztelaniaz) |
| `docs/PLAN.md` | Garapen-plana eta proben frogak (gaztelaniaz) |
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
dotnet run --project ServidorTPV/ServidorTPV.csproj
```

Kontsolan honelako mezuak ikusi behar dira:

```
BD OK -> usuarios=1 mesas=3 productos=4
[TCP] Escuchando en el puerto 5001 (max 10 clientes)
Server running. Escribe 'parar' para cerrar.
```

Gelditzeko: idatzi `parar` + Enter, edo `Ctrl+C`.

### 2. Bezeroa konpilatu / argitaratu

```bash
# Soluzio osoa konpilatu (egiaztatuta: 0 errore, 0 abisu)
dotnet build AppTpv.slnx -c Release

# Windows VM-rako paketea sortu (self-contained, x64)
dotnet publish ClienteWPF/ClienteWPF.csproj -c Release \
  -r win-x64 --self-contained true -o ClienteWPF/publish_vm_x64
```

`ClienteWPF/publish_vm_x64/` karpeta osoa Windowsera kopiatu eta `AppTpv.exe`
exekutatu. (Dagoeneko badago `publish_vm/` eta `publish_vm_x64/` karpetak.)

### 3. Zerbitzariaren helbidea konfiguratu

Bezeroak lehenespenez `127.0.0.1:5001` erabiltzen du. Beste makina batean
(adibidez, UTM Windows VM batean ostalari macOS-a dela) `servidor.txt` fitxategia
jarri `AppTpv.exe`-rekin batera, lehenengo lerroan `IP:ATAKA`:

```
192.168.64.1:5001
```

### 4. Saioa hasi

`VentanaLogin` leihoan:

- **Usuario:** `admin`
- **Contrasena:** `admin123`

Bezeroak `VentanaPrincipal` leihoa irekitzen du; bertan rolaren arabera
pestaña batzuk ikusgai egongo dira.

## Datu-basea

- Motorra: **SQLite** (`Microsoft.Data.Sqlite`), fitxategia:
  - Debug: `ServidorTPV/bin/Debug/net10.0/data/tpv.db`
  - Release: `ServidorTPV/bin/Release/net10.0/data/tpv.db`
- Lehen abiaraztean `ServidorTPV/Data/Esquema.sql`-etik sortzen da automatikoki.
- Hasierako datuak: `admin` erabiltzailea, 3 mahai (4/4/6 plaza) eta 4 produktu
  stockarekin.
- SQLite fitxategiak ez du berezko erabiltzaile/pasahitzik; sarbidea aplikazioak
  kontrolatzen du. Pasahitzak **SHA-256 + gatz finkoa** (`AppTpv::ErronkaWIDZPP::2026`)
  gordetzen dira `Usuario.passHash` zutabean.

**Backupa:** zerbitzaria geldituta, kopiatu `data/tpv.db` fitxategia. Zerbitzaria
martxan badago, erabili SQLite-ren backup mekanismoa (WAL modua dago aktibatuta).

## Erronkaren baldintzak non dauden

| Erronkaren eskakizuna | Non |
|---|---|
| 1.1 Login interfazea | `ClienteWPF/Ventanas/VentanaLogin.xaml` |
| 1.2 Erabiltzaileen CRUD | `ClienteWPF/Vistas/VistaUsuarios.xaml` |
| 1.3 Stockaren CRUD | `ClienteWPF/Vistas/VistaStock.xaml` |
| 1.4 Interfaze nagusia | `ClienteWPF/Vistas/VistaConsumiciones.xaml` |
| 1.5 Mahai-erreserbak (bazkaria/afaria) | `ClienteWPF/Vistas/VistaReservas.xaml` |
| 2. Rolak (admin / erabiltzailea) | `ServidorTPV/Servicios/GestorOperaciones.cs` |
| 3. TCP, 10 bezero, hari bana | `ServidorTPV/Red/ServidorTCP.cs` |
| 3. Idazketen lock bakarra | `ServidorTPV/Data/BaseDatos.cs` (`LockEscritura`) |
| 4. Ikus Osagaiak erreserba/kontsumoetan | XAML-eko `ComboBox`, `ListBox`, `DataGrid`, txartel-botoiak |
| 4. Ticketa + stock eguneratzea | `VistaConsumiciones.xaml.cs` + `Ped_Confirmar` |

## Dokumentazio gehiago

- [Gida teknikoa](docs/GIDA_TEKNIKOA.md)
- [Administratzailearen gida](docs/GIDA_ADMIN.md)
- [Erabiltzailearen gida](docs/GIDA_ERABILTZAILEA.md)
- [Aurkezpena: zerbitzuak, prozesuak, hariak eta socketak](docs/AURKEZPENA_ZERBITZUAK_PROZESUAK_HARIAK_SOCKETAK.md)
