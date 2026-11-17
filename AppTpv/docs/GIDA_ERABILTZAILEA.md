# AppTpv — Erabiltzailearen gida

Gida hau `usuario` rol arruntarekin logeatzen den pertsonarentzat da.
Bi pestaña baino ez ditu ikusiko: **Consumiciones** eta **Reservas**.
Administrazio-pestañak (**Usuarios**, **Stock**) ezkutatuta egongo dira.

Interfazeak: `ClienteWPF/Ventanas/VentanaLogin.xaml`,
`Ventanas/VentanaPrincipal.xaml`, `Vistas/VistaConsumiciones.xaml` eta
`Vistas/VistaReservas.xaml`.

## Saioa hasi eta amaitu

1. `VentanaLogin` leihoan, idatzi zure erabiltzaile-izena eta pasahitza.
   (Proban, administratzaileak sortutako edozein erabiltzaile erabil daiteke;
   hasierako kontua `admin` / `admin123` da.)
2. Sakatu **Entrar**. Kredentzialak okerrak badira, `TxtError`-en
   `Credenciales invalidas` agertzen da.
3. Amaitzeko, sakatu goiko eskuineko **Cerrar sesion** (`BtnSalir`).
   Saioa zerbitzarian ere itxi egiten da.

---

## 1. Mahai-erreserbak (**Reservas** pestaña)

Erreserbetarako **Ikus Osagaiak** (WPF kontrolak) erabiltzen dira:
`ComboBox` (mahaiak eta txanda), `TextBox` (data, komensalak, izena, oharrak)
eta `DataGrid` (erreserben zerrenda). Ez dago komando-testurik idatzi beharrik.

### Erreserba berria sortu

Formularioa ezkerrean dago (`VistaReservas.xaml`):

| Kontrola | Zer egiten duen |
|---|---|
| `CboMesa` | Mahai zerrenda: `Mesa 1 (capacidad 4)` moduko testua erakusten du |
| `CboTurno` | **bazkaria** edo **afaria** aukeratu |
| `TxtComensales` | Jayen kopurua (lehenespenez `2`) |
| `TxtFecha` | `YYYY-MM-DD` formatuan; **hutsik uzten bada, data gabeko erreserba** da |
| `TxtCliente` | Erreserba egiten duen bezeroaren izena |
| `TxtObservaciones` | Oharrak (aukerakoa) |

Urratsak:

1. Aukeratu mahia eta txanda.
2. Idatzi komensal-kopurua, bezeroaren izena eta (aukerakoa) data.
3. Sakatu **Crear**.
4. `TxtAviso`-n `Reserva creada.` agertzen da; eskuineko `GridReservas`
   taula eguneratzen da.

Zerbitzariak automatikoki egiaztatzen ditu:

- komensalak mahiaren edukiera baino gehiago ez direla
  (`La mesa admite como maximo 4 comensales`);
- mahia ez dagoela jada erreserbatuta **data + txanda** berean;
- data behar bezala idatzita dagoela (`YYYY-MM-DD`).

### Erreserba aldatu

1. Hautatu fila `GridReservas`-en — formularioa automatikoki betetzen da.
2. Aldatu nahi dituzun eremuak.
3. Sakatu **Modificar**.

### Erreserba bertan behera utzi

1. Hautatu fila.
2. Sakatu **Cancelar reserva** eta berretsi.

Erreserba ez da fisikoki ezabatzen: `estado = 'cancelada'` bihurtzen da.

### Zerrenda iragazi

`TxtFiltroFecha` eremuan data idatzi eta **Refrescar** sakatu: data horretako
erreserbak soilik erakusten dira. Hutsik uzten bada, guztiak erakusten dira.

---

## 2. Kontsumoak (**Consumiciones** pestaña)

Hau da TPV-ren interfaze nagusia. Hiru zati ditu:

```
┌──────────────┬───────────────────────────┬──────────────────┐
│ CATEGORIAS   │ PRODUCTOS                 │ PEDIDO           │
│ (ListBox)    │ (txartel-botoiak)         │ (lerroak, TOTAL) │
│              │                           │                  │
│ edariak      │  Kola kontxa   2.50 €     │ Enviar           │
│ janari       │  Kalimotxo     3.00 €     │ 2 x Kola ...     │
│              │  AGOTADO (stock 0)        │ TOTAL: 5.00 €    │
│              │                           │ SUBTOTAL / COBRAR│
└──────────────┴───────────────────────────┴──────────────────┘
```

### Produktuak aukeratu (Ikus Osagaiak)

1. Ezkerreko **CATEGORIAS** zerrendatik (`ListaCategorias`) aukeratu kategoria.
   Erdiko eremua produktuekin betetzen da.
2. Sakatu nahi den produktuaren **txartela** (`ListaProductos`).
   Produtu bakoitzak izena, prezioa eta stocka erakusten ditu; stockik gabe
   badago **AGOTADO** etiketa gorria agertzen da eta ezin da gehitu.
3. Produktu bera hainbat aldiz sakatuz gero, lerro berean kantitatea
   handitzen da (ez da lerro berria sortzen).
4. Eskuineko **PEDIDO** zerrendan lerroak kolorez bereizten dira:
   - **laranja** = *pendiente* (oraindik zerbitzarira bidali gabe);
   - **berdea** = *enviado* (komandan jada erregistratuta).

### Kantitatea zuzendu

Sakatu laranjazko lerro bat: `PanelEditor` agertzen da.
Idatzi kopurua `TxtCantidadEditor`-en eta sakatu **Guardar**, edo kendu
lerroa **Eliminar** botoiarekin. Berdezko lerroak ezin dira editatu.

### Komanda bidali

Sakatu **Enviar**. Lerro pendiente guztiak zerbitzarira bidaltzen dira
(`PEDIDO_AGREGAR_LINEA`) eta berde bihurtzen dira. Stocka ez da oraindik
kenduko; prezioa zerbitzariak berretsi egiten du.

### Subtotala ikusi

**SUBTOTAL** botoiak produktu bakoitzaren eta guztizkoaren zerrenda
erakusten du. Lerro pendienteen bat badago, lehenik **Enviar** sakatu behar da.

### Erosketa osatu eta tiketa

1. Sakatu **COBRAR**.
2. Zerbitzariak:
   - erosketa `cobrada` egoerara aldatzen du;
   - **automatikoki stocka murrizten du** produktu bakoitzeko;
   - guztizkoa kalkulatzen du.
3. Mezu batek berrespena eta totala erakusten ditu; ondoren carta berriro
   kargatzen da (AGOTADO etiketak eguneratuta).

**VER TICKET** botoiak uneko edo azkeneko ordainduaren tiketa erakusten du:

```
Ticket del pedido 3
Cliente: ...
Fecha:   ...

 2 x Kola kontxa                 5.00 €
 1 x Sakramento burger           6.50 €

TOTAL: 11.50 € (IVA 10% incluido)
```

Hautatutako produktuak, kantitateak eta ordaindu beharreko guztizkoa agertzen
dira, erronkak eskatzen duen moduan.

Errore bat badago (stock nahikoa ez, adibidez), `MessageBox` batean
`Stock insuficiente del producto N: quedan M` mezua agertzen da eta carta
freskatzen da.

---

## Laburpena: erronkaren eskakizunak gidan

| Eskakizuna | Non ikusten den |
|---|---|
| Ikus Osagaiak erreserbak egitean | `CboMesa`, `CboTurno`, `TxtFecha`, `GridReservas` |
| Ikus Osagaiak produktuak aukeratzean | `ListaCategorias`, txartel-botoiak, `PanelEditor` |
| Ticketa produktu/kantitate/guztizkoarekin | **VER TICKET** (`BtnTicket_Click`) |
| Erosketa baieztatzean stocka automatikoki eguneratu | **COBRAR** → `PEDIDO_CONFIRMAR` → `Ped_Confirmar` |
| Bazkaria/afaria bereizita | `CboTurno` |
| Data zehaztu gabea onartuta | `TxtFecha` hutsik |
