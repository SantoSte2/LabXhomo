# MarcTempo

MarcTempo è un’applicazione web sviluppata in **ASP.NET Core MVC (.NET 8)** per la gestione delle richieste di ferie, permessi, Smart Working e mancate timbrature all’interno di un reparto aziendale.

Il progetto nasce come applicazione d’esame e riproduce un flusso realistico di gestione delle presenze, distinguendo due ruoli principali:

- **Dipendente**, che inserisce e controlla le proprie richieste;
- **Super**, che visualizza il reparto, controlla le presenze e approva o respinge le richieste con motivazione obbligatoria.

La schermata principale comune è **Ferie Reparto**, dalla quale entrambi i ruoli possono consultare il calendario mensile o annuale.

---

## Obiettivi del progetto

MarcTempo permette di:

- consultare ferie, permessi e Smart Working del reparto;
- evitare sovrapposizioni tra colleghi;
- inserire nuove richieste;
- approvare o respingere le richieste;
- mostrare sempre lo stato della richiesta anche tramite testo;
- calcolare le presenze giornaliere del reparto;
- distinguere weekend e festività italiane;
- mantenere un’interfaccia responsive per desktop, tablet e smartphone.

---

## Tecnologie utilizzate

- **ASP.NET Core MVC**
- **.NET 8**
- **C#**
- **Razor Views**
- **Entity Framework Core**
- **EF Core InMemory**
- **Cookie Authentication**
- **Session**
- **HTML5**
- **CSS personalizzato**
- **JavaScript**
- **Visual Studio 2022**

Il progetto deriva dal template universitario composto dai progetti `Template` e `Template.Web`.

---

## Ruoli applicativi

### Dipendente

Il Dipendente può:

- accedere tramite e-mail;
- visualizzare la Home comune del reparto;
- consultare il calendario mensile;
- consultare il calendario annuale;
- utilizzare il filtro personalizzato `Dal / Al`;
- visualizzare le richieste dei colleghi;
- riconoscere la propria riga tramite il badge `Tu`;
- inserire una nuova richiesta;
- consultare il riepilogo prima della conferma;
- visualizzare il proprio dettaglio personale;
- controllare lo stato delle richieste;
- leggere la motivazione inserita dal Super;
- consultare i propri totalizzatori.

### Super

Il Super può:

- accedere alla stessa Home comune;
- consultare ferie, permessi e Smart Working del reparto;
- visualizzare il numero di presenti per ogni giornata;
- distinguere i dipendenti in Smart Working;
- aprire la pagina di gestione richieste;
- selezionare un dipendente tramite ricerca;
- approvare o respingere una richiesta;
- inserire obbligatoriamente una motivazione;
- visualizzare lo storico della valutazione;
- controllare nominativo e data della valutazione.

---

## Home comune: Ferie Reparto

La pagina `Reparto/Index` rappresenta il punto di ingresso principale per entrambi i ruoli.

La pagina contiene:

- navigazione mensile;
- vista annuale;
- filtro data `Dal / Al`;
- periodo predefinito sul mese corrente;
- intervallo massimo di 31 giorni;
- calendario orizzontale con dipendenti sulle righe;
- giorni del periodo sulle colonne;
- weekend evidenziati;
- festività civili e religiose italiane;
- badge testuali per ferie, permessi e Smart Working;
- stato della richiesta scritto esplicitamente;
- identificazione dell’utente corrente tramite `Tu`.

Gli stati disponibili sono:

- `In attesa`;
- `Approvata`;
- `Respinta`.

Il colore viene utilizzato come supporto visivo, ma lo stato è sempre indicato anche tramite testo.

---

## Vista annuale

La vista annuale consente di osservare l’intero anno tramite una griglia compatta.

Per ogni richiesta vengono visualizzati:

- iniziali dell’utente;
- tipologia della richiesta;
- stato della richiesta.

Esempi:

```text
SS
Ferie
Approvata
```

```text
SS
Smart working
In attesa
```

```text
MR
Permesso studio
Respinta
```

Le etichette `SAB`, `DOM`, `CIV` e `REL` vengono mantenute per conservare una struttura compatta.

---

## Tipologie di richiesta

Il calendario condiviso può mostrare:

- ferie ROL;
- ferie straordinarie;
- Smart Working;
- permesso;
- permesso studio.

Il progetto gestisce inoltre le mancate timbrature all’interno del flusso personale del Dipendente.

---

## Flusso di inserimento richiesta

Il Dipendente segue questo percorso:

1. selezione della tipologia;
2. inserimento dei dati;
3. controllo del riepilogo;
4. conferma oppure annullamento;
5. salvataggio della richiesta;
6. ritorno alla Home comune.

Il flusso utilizza il pattern **Post/Redirect/Get**, evitando duplicazioni causate dall’aggiornamento della pagina.

---

## Approvazione e rifiuto

Il Super seleziona una richiesta dalla pagina di gestione.

Per approvare o respingere è obbligatorio inserire una motivazione.

Dopo la valutazione vengono memorizzati:

- nuovo stato;
- motivazione;
- data e ora;
- nominativo del Super.

Una richiesta già valutata non può essere approvata o respinta una seconda volta.

La motivazione è visibile anche al Dipendente nella pagina di dettaglio personale.

---

## Calcolo delle presenze

Per ogni giornata il Super visualizza:

- dipendenti presenti;
- totale dipendenti;
- numero di persone in Smart Working.

Regole principali:

- ferie approvate riducono i presenti;
- permessi approvati possono ridurre i presenti;
- Smart Working non riduce il numero dei presenti;
- richieste in attesa non modificano il conteggio;
- richieste respinte non modificano il conteggio;
- weekend e festività vengono indicati come giorni non lavorativi.

---

## Regole di business

Il progetto applica le seguenti regole:

- ferie conteggiate solo nei giorni lavorativi;
- sabato e domenica esclusi;
- festività escluse dal conteggio;
- ferie ordinarie non consentite con saldo insufficiente;
- ferie straordinarie autorizzate anche con saldo negativo;
- richieste multi-giorno visualizzate su tutte le date coinvolte;
- stato e totalizzatori aggiornati dopo la valutazione;
- motivazione obbligatoria per approvazione e rifiuto.

---

## Autenticazione e sessione

L’accesso avviene tramite e-mail.

L’applicazione utilizza:

- autenticazione tramite cookie;
- claims per ruolo e identità;
- sessione per i dati necessari all’interfaccia;
- ricostruzione della sessione dopo il riavvio dell’applicazione.

Dopo il login:

- Dipendente e Super vengono indirizzati a `Reparto/Index`;
- ogni ruolo visualizza i pulsanti coerenti con le proprie autorizzazioni.

---

## Struttura principale

```text
Template.sln
│
├── Template
│   ├── Models
│   ├── Data
│   └── Servizi condivisi
│
└── Template.Web
    ├── Features
    │   ├── Login
    │   ├── Dipendente
    │   ├── Super
    │   └── Reparto
    │
    ├── Views
    ├── wwwroot
    │   └── css
    │       └── marctempo
    │
    ├── Startup.cs
    └── appsettings.json
```

---

## Avvio del progetto

### Requisiti

- Visual Studio 2022;
- .NET 8 SDK;
- browser moderno.

### Procedura

1. Aprire `Template.sln`.
2. Impostare `Template.Web` come progetto di avvio.
3. Ripristinare i pacchetti NuGet.
4. Compilare la soluzione.
5. Avviare il progetto con Visual Studio.

Da terminale:

```bash
dotnet restore
dotnet build
dotnet run --project Template.Web
```

---

## Persistenza dei dati

Il progetto utilizza attualmente **EF Core InMemory**.

Questo significa che:

- non sono necessarie migration;
- i dati vengono mantenuti durante l’esecuzione;
- i dati vengono azzerati al riavvio dell’applicazione.

Questa scelta è coerente con l’ambito didattico e dimostrativo del progetto.

---

## Test eseguiti

Sono stati verificati con esito positivo:

- login Dipendente;
- login Super;
- logout;
- ricostruzione della sessione;
- redirect in base al ruolo;
- inserimento richiesta;
- annullamento richiesta;
- conferma richiesta;
- pattern Post/Redirect/Get;
- filtro `Dal / Al`;
- limite massimo di 31 giorni;
- correzione automatica delle date invertite;
- calendario mensile;
- calendario annuale;
- weekend e festività;
- ferie, permessi e Smart Working;
- stato testuale dei badge;
- riga utente con badge `Tu`;
- conteggio presenze;
- conteggio Smart Working;
- approvazione con motivazione;
- rifiuto con motivazione;
- blocco della motivazione vuota;
- blocco della doppia valutazione;
- visualizzazione della motivazione lato Dipendente;
- aggiornamento dei totalizzatori;
- responsive desktop, tablet e smartphone.

Tutti i test funzionali e di sicurezza applicativa previsti sono risultati positivi.

---

## Stato del progetto

Il progetto è completo e funzionante.

Le funzionalità principali richieste sono state implementate e collaudate:

- Home comune del reparto;
- gestione richieste;
- vista mensile;
- vista annuale;
- filtro personalizzato;
- conteggio presenze;
- motivazioni di approvazione e rifiuto;
- interfaccia responsive;
- separazione dei ruoli.

---

## Autore

**Stefano Santoni**
