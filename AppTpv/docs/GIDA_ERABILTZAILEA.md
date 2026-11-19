# AppTpv — Erabiltzailearen gida

Gida hau `usuario` rol arruntarekin logeatzen den pertsonarentzat da.
Bi pestaña baino ez ditu ikusiko: **Kontsumizioak** eta **Erreserbak**.
Administrazio-pestañak (**Erabiltzaileak**, **Izakinak**) ezkutatuta egongo dira.

Interfazeak: `BezeroaWPF/Leihoak/SaioHasieraLeihoa.xaml`,
`Leihoak/LeihoNagusia.xaml`, `Ikuspegiak/KontsumizioIkuspegia.xaml` eta
`Ikuspegiak/ErreserbaIkuspegia.xaml`.

## Saioa hasi eta amaitu

1. `SaioHasieraLeihoa` leihoan, idatzi zure erabiltzaile-izena eta pasahitza.
   (Proban, administratzaileak sortutako edozein erabiltzaile erabil daiteke;
   hasierako kontua `admin` / `admin123` da.)
2. Sakatu **Sartu**. Kredentzialak okerrak badira, `TxtErrorea`-en
   `Erabiltzaile-izena edo pasahitza ez dira zuzenak` agertzen da.
3. Amaitzeko, sakatu goiko eskuineko **Amaitu saioa** (`BtnIrten`).
   Saioa zerbitzarian ere itxi egiten da.

---

## 1. Mahai-erreserbak (**Erreserbak** pestaña)

Erreserbetarako **Ikus Osagaiak** (WPF kontrolak) erabiltzen dira:
`ComboBox` (mahaiak eta txanda), `TextBox` (data, komensalak, izena, oharrak)
eta `DataGrid` (erreserben zerrenda). Ez dago komando-testurik idatzi beharrik.

### Erreserba berria sortu

Formularioa ezkerrean dago (`ErreserbaIkuspegia.xaml`):

| Kontrola | Zer egiten duen |
|---|---|
| `CboMahia` | Mahai zerrenda: `Mahai 1 (edukiera 4)` moduko testua erakusten du |
| `CboTxanda` | **bazkaria** edo **afaria** aukeratu |
| `TxtPertsonak` | Pertsona kopurua (lehenespenez `2`) |
| `TxtData` | `YYYY-MM-DD` formatuan; **hutsik uzten bada, data gabeko erreserba** da |
| `TxtBezeroa` | Erreserba egiten duen bezeroaren izena |
| `TxtOharrak` | Oharrak (aukerakoa) |

Urratsak:

1. Aukeratu mahia eta txanda.
2. Idatzi komensal-kopurua, bezeroaren izena eta (aukerakoa) data.
3. Sakatu **Sortu**.
4. `TxtAbisua`-n `Reserva creada.` agertzen da; eskuineko `GridErreserbak`
   taula eguneratzen da.

Zerbitzariak automatikoki egiaztatzen ditu:

- komensalak mahiaren edukiera baino gehiago ez direla
  (`Mahaiak gehienez 4 lagun onartzen ditu`);
- mahia ez dagoela jada erreserbatuta **data + txanda** berean;
- data behar bezala idatzita dagoela (`YYYY-MM-DD`).

### Erreserba aldatu

1. Hautatu fila `GridErreserbak`-en — formularioa automatikoki betetzen da.
2. Aldatu nahi dituzun eremuak.
3. Sakatu **Aldatu**.

### Erreserba bertan behera utzi

1. Hautatu fila.
2. Sakatu **Utzi erreserba** eta berretsi.

Erreserba ez da fisikoki ezabatzen: `estado = 'cancelada'` bihurtzen da.

### Zerrenda iragazi

`TxtDataIragazkia` eremuan data idatzi eta **Eguneratu** sakatu: data horretako
erreserbak soilik erakusten dira. Hutsik uzten bada, guztiak erakusten dira.

---

## 2. Kontsumoak (**Kontsumizioak** pestaña)

Hau da TPV-ren interfaze nagusia. Hiru zati ditu:

```
┌──────────────┬─────────────────────────┬──────────────────────┐
│ KATEGORIAK   │ PRODUKTUAK              │ ESKAERA              │
├──────────────┼─────────────────────────┼──────────────────────┤
│ (ListBox)    │ (txartel-botoiak)       │ (lerroak, GUZTIRA)   │
│              │                         │                      │
│ edariak      │ Kola kontxa   2.50 €    │ Bidali               │
│ janari       │ Kalimotxo     3.00 €    │ 2 x Kola ...         │
│              │ AGORTUTA (izakinak 0)   │ GUZTIRA: 5.00 €      │
│              │                         │ AZPITOTALA / KOBRATU │
└──────────────┴─────────────────────────┴──────────────────────┘
```

### Produktuak aukeratu (Ikus Osagaiak)

1. Ezkerreko **CATEGORIAS** zerrendatik (`ListaKategoriak`) aukeratu kategoria.
   Erdiko eremua produktuekin betetzen da.
2. Sakatu nahi den produktuaren **txartela** (`ListaProduktuak`).
   Produtu bakoitzak izena, prezioa eta stocka erakusten ditu; stockik gabe
   badago **AGORTUTA** etiketa gorria agertzen da eta ezin da gehitu.
3. Produktu bera hainbat aldiz sakatuz gero, lerro berean kantitatea
   handitzen da (ez da lerro berria sortzen).
4. Eskuineko **PEDIDO** zerrendan lerroak kolorez bereizten dira:
   - **laranja** = *zain* (oraindik zerbitzarira bidali gabe);
   - **berdea** = *bidalita* (komandan jada erregistratuta).

### Kantitatea zuzendu

Sakatu laranjazko lerro bat: `EditorePanela` agertzen da.
Idatzi kopurua `TxtKopuruEditorea`-n eta sakatu **Guardar**, edo kendu
lerroa **Eliminar** botoiarekin. Berdezko lerroak ezin dira editatu.

### Komanda bidali

Sakatu **Bidali**. Lerro zain guztiak zerbitzarira bidaltzen dira
(`PEDIDO_AGREGAR_LINEA`) eta berde bihurtzen dira. Stocka ez da oraindik
kenduko; prezioa zerbitzariak berretsi egiten du.

### Subtotala ikusi

**AZPITOTALA** botoiak produktu bakoitzaren eta guztizkoaren zerrenda
erakusten du. Lerro zainen bat badago, lehenik **Bidali** sakatu behar da.

### Erosketa osatu eta tiketa

1. Sakatu **KOBRATU**.
2. Zerbitzariak:
   - erosketa `cobrada` egoerara aldatzen du;
   - **automatikoki stocka murrizten du** produktu bakoitzeko;
   - guztizkoa kalkulatzen du.
3. Mezu batek berrespena eta totala erakusten ditu; ondoren carta berriro
   kargatzen da (AGORTUTA etiketak eguneratuta).

**IKUSI TIKETA** botoiak uneko edo azkeneko ordainduaren txartela erakusten du:

```
Eskaeraren txartela 3
Bezeroa: ...
Data:    ...

 2 x Kola kontxa                 5.00 €
 1 x Sakramento burger           6.50 €

GUZTIRA: 11.50 € (BEZa %10 barne)
```

Hautatutako produktuak, kantitateak eta ordaindu beharreko guztizkoa agertzen
dira, erronkak eskatzen duen moduan.

Errore bat badago (stock nahikoa ez, adibidez), `MessageBox` batean
`Ez dago nahikoa izakin: M unitate geratzen dira produktu horretatik` mezua agertzen da eta carta
freskatzen da.

---

## Laburpena: erronkaren eskakizunak gidan

| Eskakizuna | Non ikusten den |
|---|---|
| Ikus Osagaiak erreserbak egitean | `CboMahia`, `CboTxanda`, `TxtData`, `GridErreserbak` |
| Ikus Osagaiak produktuak aukeratzean | `ListaKategoriak`, txartel-botoiak, `EditorePanela` |
| Ticketa produktu/kantitate/guztizkoarekin | **IKUSI TIKETA** (`BtnTicket_Click`) |
| Erosketa baieztatzean stocka automatikoki eguneratu | **KOBRATU** → `PEDIDO_CONFIRMAR` → `Eska_Baieztatu` |
| Bazkaria/afaria bereizita | `CboTxanda` |
| Data zehaztu gabea onartuta | `TxtData` hutsik |
