#nullable enable

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Template.Enums;
using Template.Models;
using Template.Services;
using Template.Web.Services;

namespace Template.Web.Features.Reparto
{
    [Authorize]
    public partial class RepartoController : Controller
    {
        private readonly TemplateDbContext _dbContext;

        public RepartoController(TemplateDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public virtual IActionResult Index(
            string vista = "Mensile",
            int? mese = null,
            int? anno = null,
            DateTime? dal = null,
            DateTime? al = null)
        {
            var oggi = DateTime.Today;
            var culturaItaliana = new CultureInfo("it-IT");

            var vistaNormalizzata = string.Equals(
                vista,
                "Annuale",
                StringComparison.OrdinalIgnoreCase)
                    ? "Annuale"
                    : "Mensile";

            /*
             * Quando è presente una data iniziale personalizzata,
             * usiamo il suo mese e anno come riferimento per:
             * - titolo;
             * - pulsanti precedente e successivo;
             * - passaggio alla vista annuale.
             */
            var meseSelezionato =
                dal?.Month ??
                mese ??
                oggi.Month;

            var annoSelezionato =
                dal?.Year ??
                anno ??
                oggi.Year;

            var primoGiornoMese = new DateTime(
                annoSelezionato,
                meseSelezionato,
                1);

            /*
             * Periodo predefinito:
             * dal primo all'ultimo giorno del mese selezionato.
             */
            var dataDal =
                dal?.Date ??
                primoGiornoMese;

            var dataAl =
                al?.Date ??
                primoGiornoMese
                    .AddMonths(1)
                    .AddDays(-1);

            var messaggioPeriodo = string.Empty;

            /*
             * La data finale non può precedere quella iniziale.
             * In caso di errore la riportiamo sulla stessa data.
             */
            if (dataAl < dataDal)
            {
                dataAl = dataDal;

                messaggioPeriodo =
                    "La data finale era precedente alla data iniziale ed è stata corretta.";
            }

            /*
             * Limitiamo la vista mensile a un massimo di 31 giorni.
             *
             * La tabella resta così leggibile e non diventa
             * eccessivamente larga.
             */
            if ((dataAl - dataDal).TotalDays > 30)
            {
                dataAl = dataDal.AddDays(30);

                messaggioPeriodo =
                    "Il periodo massimo visualizzabile è di 31 giorni.";
            }

            var mesePrecedente = primoGiornoMese.AddMonths(-1);
            var meseSuccessivo = primoGiornoMese.AddMonths(1);

            /*
             * Controlliamo se l'intervallo rappresenta
             * esattamente il mese completo.
             */
            var ultimoGiornoMese =
                primoGiornoMese
                    .AddMonths(1)
                    .AddDays(-1);

            var meseCompleto =
                dataDal == primoGiornoMese &&
                dataAl == ultimoGiornoMese;

            var nomeMese =
                culturaItaliana.TextInfo.ToTitleCase(
                    primoGiornoMese.ToString(
                        "MMMM",
                        culturaItaliana));

            /*
             * Per il mese completo mostriamo "Giugno 2026".
             * Per un filtro personalizzato mostriamo le due date.
             */
            var titoloPeriodo = meseCompleto
                ? $"{nomeMese} {annoSelezionato}"
                : $"{dataDal:dd/MM/yyyy} - {dataAl:dd/MM/yyyy}";

            

            /*
             * La matricola identifica in modo univoco
             * l'utente attualmente autenticato.
             *
             * La utilizziamo per riconoscere la sua riga
             * nel calendario del reparto.
             */
            var matricolaUtenteCorrente =
                HttpContext.Session.GetString("Matricola")
                ?? string.Empty;


            var model = new CalendarioColleghiViewModel
            {
                NominativoUtente =
                    HttpContext.Session.GetString("Nominativo") ?? "Utente",

                MatricolaUtente =
                    HttpContext.Session.GetString("Matricola") ?? "-",

                InizialiUtente =
                    HttpContext.Session.GetString("Iniziali") ?? "MT",

                RuoloUtente =
                    HttpContext.Session.GetString("Ruolo") ?? "Utente",

                Vista = vistaNormalizzata,

                Mese = meseSelezionato,
                Anno = annoSelezionato,

                NomeMese = nomeMese,

                /*
                 * Periodo effettivamente mostrato nella griglia.
                 */
                DataDal = dataDal,
                DataAl = dataAl,

                TitoloPeriodo = titoloPeriodo,

                MessaggioPeriodo = messaggioPeriodo,

                MesePrecedente = mesePrecedente.Month,
                AnnoMesePrecedente = mesePrecedente.Year,

                MeseSuccessivo = meseSuccessivo.Month,
                AnnoMeseSuccessivo = meseSuccessivo.Year
            };

            if (model.VistaAnnuale)
            {
                // La vista annuale utilizza sempre 31 colonne.
                // I giorni inesistenti vengono mostrati come celle disabilitate.
                model.GiorniAnno = Enumerable.Range(1, 31).ToList();
                model.MesiAnno = CreaVistaAnnuale(annoSelezionato);
            }
            else
            {
                /*
                 * La vista mensile non è più obbligata
                 * a mostrare tutto il mese.
                 *
                 * I giorni e le celle vengono costruiti
                 * usando il periodo Dal / Al.
                 */
                model.Giorni =
                    CreaGiorniPeriodo(
                        dataDal,
                        dataAl);

                model.Persone =
                    CreaRighePersonePeriodo(
                        dataDal,
                        dataAl,
                        matricolaUtenteCorrente);
            }

            return View(model);
        }

       

        /*
         * Costruisce le intestazioni dei giorni compresi
         * nell'intervallo Dal / Al.
         *
         * Oltre alle informazioni grafiche del calendario,
         * calcola anche il totale delle presenze giornaliere
         * richiesto nella vista del Super.
         */
        private List<GiornoRepartoViewModel>
            CreaGiorniPeriodo(
                DateTime dataDal,
                DateTime dataAl)
        {
            var culturaItaliana =
                new CultureInfo("it-IT");

            var giorni =
                new List<GiornoRepartoViewModel>();

            /*
             * Recuperiamo gli identificativi dei soli Dipendenti.
             *
             * Il Super compare nella griglia, ma non partecipa
             * al conteggio "dipendenti presenti".
             */
            var dipendentiIds = _dbContext.Dipendenti
                .Where(d =>
                    d.Ruolo == RuoloUtente.Dipendente)
                .Select(d => d.Id)
                .ToList();

            var totaleDipendenti =
                dipendentiIds.Count;

            var finePeriodoEsclusiva =
                dataAl.Date.AddDays(1);

            /*
             * Per il conteggio consideriamo soltanto
             * richieste già approvate.
             *
             * Una richiesta in attesa non rappresenta ancora
             * un'assenza effettiva; una richiesta respinta
             * non produce alcun effetto sulla presenza.
             */
            var richiesteApprovatePeriodo =
                _dbContext.Richieste
                    .Where(r =>
                        dipendentiIds.Contains(
                            r.DipendenteId) &&

                        r.TipoRichiesta ==
                            TipoRichiesta.Giustificativo &&

                        r.Stato ==
                            StatoRichiesta.Approvata &&

                        r.DataInizio <
                            finePeriodoEsclusiva &&

                        (r.DataFine ??
                            r.DataInizio) >=
                            dataDal.Date)
                    .ToList();

            /*
             * Conserviamo le festività già calcolate
             * per ciascun anno attraversato dal periodo.
             */
            var festivitaPerAnno =
                new Dictionary<
                    int,
                    IReadOnlyDictionary<
                        DateTime,
                        FestivitaItaliana>>();

            for (var data = dataDal.Date;
                 data <= dataAl.Date;
                 data = data.AddDays(1))
            {
                if (!festivitaPerAnno.TryGetValue(
                        data.Year,
                        out var festivitaAnno))
                {
                    festivitaAnno =
                        CalendarioFestivitaItaliane
                            .CreaPerAnno(data.Year);

                    festivitaPerAnno[data.Year] =
                        festivitaAnno;
                }

                festivitaAnno.TryGetValue(
                    data.Date,
                    out var festivita);

                var weekend =
                    data.DayOfWeek ==
                        DayOfWeek.Saturday ||

                    data.DayOfWeek ==
                        DayOfWeek.Sunday;

                /*
                 * Recuperiamo le richieste approvate
                 * che comprendono la giornata corrente.
                 *
                 * RichiestaVisibileNelGiorno esclude già
                 * weekend e festività nazionali.
                 */
                var richiesteDelGiorno =
                    richiesteApprovatePeriodo
                        .Where(r =>
                            RichiestaVisibileNelGiorno(
                                r,
                                data))
                        .ToList();

                /*
                 * Ogni dipendente deve essere contato una sola volta,
                 * anche nel caso anomalo di richieste sovrapposte.
                 */
                var dipendentiAssentiIds =
                    richiesteDelGiorno
                        .Where(
                            RichiestaContaComeAssenzaGiornaliera)
                        .Select(r => r.DipendenteId)
                        .Distinct()
                        .ToHashSet();

                /*
                 * Lo Smart Working non rappresenta assenza.
                 * Il dipendente rimane quindi tra i presenti,
                 * ma viene conteggiato anche separatamente.
                 */
                var dipendentiSmartIds =
                    richiesteDelGiorno
                        .Where(
                            RichiestaContaComeSmartWorking)
                        .Select(r => r.DipendenteId)
                        .Distinct()
                        .ToHashSet();

                giorni.Add(
                    new GiornoRepartoViewModel
                    {
                        Data = data,

                        NumeroGiorno =
                            data.Day,

                        NomeGiornoBreve =
                            culturaItaliana
                                .DateTimeFormat
                                .GetAbbreviatedDayName(
                                    data.DayOfWeek),

                        Oggi =
                            data.Date ==
                            DateTime.Today,

                        Weekend =
                            weekend,

                        FestivoNazionale =
                            festivita != null,

                        NomeFestivita =
                            festivita?.Nome ??
                            string.Empty,

                        ClasseFestivita =
                            CreaClasseFestivita(
                                festivita),

                        TotaleDipendenti =
                            totaleDipendenti,

                        DipendentiPresenti =
                            totaleDipendenti -
                            dipendentiAssentiIds.Count,

                        DipendentiSmartWorking =
                            dipendentiSmartIds.Count
                    });
            }

            return giorni;
        }


        /*
         * Stabilisce se una richiesta approvata deve ridurre
         * il numero dei dipendenti presenti nella giornata.
         *
         * Le ferie sono sempre considerate assenza giornaliera.
         *
         * Un permesso viene considerato assenza completa
         * soltanto quando non possiede un intervallo orario.
         * Un permesso di alcune ore non elimina quindi
         * il dipendente dal conteggio giornaliero.
         */
        private static bool RichiestaContaComeAssenzaGiornaliera(
            Richiesta richiesta)
        {
            var tipo =
                richiesta.TipoGiustificativo?.ToString()
                ?? string.Empty;

            /*
             * Lo Smart Working è attività lavorativa
             * e non deve mai essere contato come assenza.
             */
            if (tipo.Contains(
                    "Smart",
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            /*
             * Ferie ROL e ferie straordinarie
             * rappresentano assenza per l'intera giornata.
             */
            if (tipo.Contains(
                    "Ferie",
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            /*
             * Il permesso senza orario viene interpretato
             * come permesso giornaliero.
             */
            if (tipo.Contains(
                    "Permesso",
                    StringComparison.OrdinalIgnoreCase))
            {
                return
                    !richiesta.OraInizio.HasValue &&
                    !richiesta.OraFine.HasValue;
            }

            return false;
        }

        /*
         * Individua le richieste approvate
         * appartenenti alla categoria Smart Working.
         */
        private static bool RichiestaContaComeSmartWorking(
            Richiesta richiesta)
        {
            var tipo =
                richiesta.TipoGiustificativo?.ToString()
                ?? string.Empty;

            return tipo.Contains(
                "Smart",
                StringComparison.OrdinalIgnoreCase);
        }


        /*
         * Costruisce una riga per ogni persona
         * e una cella per ogni data del periodo scelto.
         */
        private List<RigaPersonaRepartoViewModel>
            CreaRighePersonePeriodo(
                DateTime dataDal,
                DateTime dataAl,
                string matricolaUtenteCorrente)
        {
            /*
             * La query usa un limite finale esclusivo.
             * In questo modo vengono comprese anche
             * le richieste che terminano proprio in DataAl.
             */
            var finePeriodoEsclusiva =
                dataAl.Date.AddDays(1);

            /*
             * Recuperiamo tutti i giustificativi che intersecano
             * il periodo selezionato nella schermata Dal / Al.
             *
             * In questa prima query non elenchiamo manualmente
             * FerieRol, SmartWorking, PermessoStudio eccetera:
             * il filtro delle tipologie visibili viene applicato
             * subito dopo tramite un metodo dedicato.
             */
            var richiestePeriodo = _dbContext.Richieste
                .Where(r =>
                    r.TipoRichiesta ==
                        TipoRichiesta.Giustificativo &&

                    r.TipoGiustificativo != null &&

                    r.DataInizio <
                        finePeriodoEsclusiva &&

                    (r.DataFine ?? r.DataInizio) >=
                        dataDal.Date)
                .ToList()

                /*
                 * Dopo il caricamento manteniamo soltanto
                 * le categorie utili al calendario di reparto:
                 * - ferie;
                 * - permessi;
                 * - Smart Working.
                 *
                 * Eventuali giustificativi più riservati
                 * non vengono mostrati a tutti i colleghi.
                 */
                .Where(RichiestaDaMostrareNelCalendarioReparto)
                .ToList();

            var persone =
                _dbContext.Dipendenti
                    .OrderBy(d => d.Ruolo)
                    .ThenBy(d => d.Cognome)
                    .ThenBy(d => d.Nome)
                    .ToList();

            var righe =
                new List<RigaPersonaRepartoViewModel>();

            var festivitaPerAnno =
                new Dictionary<
                    int,
                    IReadOnlyDictionary<
                        DateTime,
                        FestivitaItaliana>>();

            foreach (var persona in persone)
            {
                var riga = new RigaPersonaRepartoViewModel
                {
                    Id = persona.Id,
                    Nominativo = persona.Nominativo,
                    Matricola = persona.Matricola,
                    Ruolo = persona.Ruolo.ToString(),

                    /*
                     * Il confronto viene fatto senza distinguere
                     * maiuscole e minuscole ed eliminando
                     * eventuali spazi accidentali.
                     */
                    UtenteCorrente =
                        !string.IsNullOrWhiteSpace(
                            matricolaUtenteCorrente) &&

                        string.Equals(
                            persona.Matricola?.Trim(),
                            matricolaUtenteCorrente.Trim(),
                            StringComparison.OrdinalIgnoreCase)
                };

                for (var data = dataDal.Date;
                     data <= dataAl.Date;
                     data = data.AddDays(1))
                {
                    if (!festivitaPerAnno.TryGetValue(
                            data.Year,
                            out var festivitaAnno))
                    {
                        festivitaAnno =
                            CalendarioFestivitaItaliane
                                .CreaPerAnno(data.Year);

                        festivitaPerAnno[data.Year] =
                            festivitaAnno;
                    }

                    festivitaAnno.TryGetValue(
                        data.Date,
                        out var festivita);

                    /*
                     * Selezioniamo gli eventi della persona
                     * visibili nel giorno corrente.
                     */
                    var eventi =
                        richiestePeriodo
                            .Where(r =>
                                r.DipendenteId ==
                                    persona.Id &&

                                RichiestaVisibileNelGiorno(
                                    r,
                                    data))
                            .Select(r =>
                                new EventoFerieRepartoViewModel
                                {
                                    /*
                                     * Il testo ora può essere:
                                     * - Ferie;
                                     * - Ferie straord.;
                                     * - Smart working;
                                     * - Permesso studio;
                                     * - Permesso.
                                     */
                                    Testo =
                                        CreaEtichettaRichiestaReparto(r),

                                    /*
                                     * Stato leggibile mostrato direttamente
                                     * nella cella del calendario.
                                     */
                                    StatoTesto =
                                        CreaTestoStato(r.Stato),

                                    /*
                                     * Colore usato soltanto come supporto visivo.
                                     */
                                    StatoCssClass =
                                        CreaClasseStato(r.Stato)
                                })

                            .ToList();

                    riga.Celle.Add(
                        new CellaFerieRepartoViewModel
                        {
                            Data = data,

                            Oggi =
                                data.Date ==
                                DateTime.Today,

                            Weekend =
                                data.DayOfWeek ==
                                    DayOfWeek.Saturday ||

                                data.DayOfWeek ==
                                    DayOfWeek.Sunday,

                            FestivoNazionale =
                                festivita != null,

                            NomeFestivita =
                                festivita?.Nome ??
                                string.Empty,

                            ClasseFestivita =
                                CreaClasseFestivita(
                                    festivita),

                            Eventi = eventi
                        });
                }

                righe.Add(riga);
            }

            return righe;
        }

   
        private static bool RichiestaVisibileNelGiorno(
            Richiesta richiesta,
            DateTime data)
        {
            /*
             * Verifichiamo innanzitutto che la data sia compresa
             * nel periodo richiesto dal dipendente.
             */
            var dentroIlPeriodo =
                richiesta.DataInizio.Date <= data.Date &&
                (richiesta.DataFine ?? richiesta.DataInizio).Date
                    >= data.Date;

            if (!dentroIlPeriodo)
            {
                return false;
            }

            /*
             * La richiesta viene mostrata solamente nei giorni lavorativi.
             *
             * Sono quindi esclusi:
             * - sabato;
             * - domenica;
             * - festività nazionali civili;
             * - festività nazionali religiose.
             */
            return CalendarioFestivitaItaliane
                .EGiornoLavorativo(data);
        }

        /*
         * Stabilisce quali giustificativi possono essere mostrati
         * nel calendario condiviso del reparto.
         *
         * Usiamo il nome del valore enum per non dipendere
         * dall'elenco esatto presente nel progetto.
         *
         * Sono ammesse soltanto le categorie che contengono:
         * - "Ferie";
         * - "Permesso";
         * - "Smart".
         */
        private static bool RichiestaDaMostrareNelCalendarioReparto(
            Richiesta richiesta)
        {
            if (richiesta.TipoRichiesta !=
                    TipoRichiesta.Giustificativo ||
                richiesta.TipoGiustificativo == null)
            {
                return false;
            }

            var tipo =
                richiesta.TipoGiustificativo.Value.ToString();

            return
                tipo.Contains(
                    "Ferie",
                    StringComparison.OrdinalIgnoreCase) ||

                tipo.Contains(
                    "Permesso",
                    StringComparison.OrdinalIgnoreCase) ||

                tipo.Contains(
                    "Smart",
                    StringComparison.OrdinalIgnoreCase);
        }

        /*
         * Trasforma il valore tecnico dell'enum
         * in un'etichetta breve e leggibile.
         *
         * Il controllo viene effettuato sulle parole contenute
         * nel nome, così funziona anche con valori come:
         * - SmartWorking;
         * - PermessoStudio;
         * - FerieRol;
         * - FerieStraordinarie.
         */
        private static string CreaEtichettaRichiestaReparto(
            Richiesta richiesta)
        {
            var tipo =
                richiesta.TipoGiustificativo?.ToString()
                ?? "Giustificativo";

            /*
             * Controlliamo prima le ferie straordinarie,
             * perché anche il loro nome contiene la parola "Ferie".
             */
            if (tipo.Contains(
                    "FerieStraord",
                    StringComparison.OrdinalIgnoreCase))
            {
                return "Ferie straord.";
            }

            if (tipo.Contains(
                    "Ferie",
                    StringComparison.OrdinalIgnoreCase))
            {
                return "Ferie";
            }

            if (tipo.Contains(
                    "Smart",
                    StringComparison.OrdinalIgnoreCase))
            {
                return "Smart working";
            }

            if (tipo.Contains(
                    "PermessoStudio",
                    StringComparison.OrdinalIgnoreCase))
            {
                return "Permesso studio";
            }

            if (tipo.Contains(
                    "Permesso",
                    StringComparison.OrdinalIgnoreCase))
            {
                return "Permesso";
            }

            /*
             * Caso di sicurezza: restituiamo il nome originale
             * qualora venga aggiunto un nuovo tipo ammesso.
             */
            return tipo;
        }

        
        private static string CreaClasseStato(StatoRichiesta stato)
        {
            return stato switch
            {
                StatoRichiesta.Approvata => "marc-status-approved",
                StatoRichiesta.Respinta => "marc-status-rejected",
                _ => "marc-status-pending"
            };
        }

        /*
         * Restituisce lo stato in una forma leggibile
         * da mostrare direttamente nel calendario.
         *
         * Non utilizziamo soltanto il colore:
         * il significato rimane comprensibile anche
         * a persone con difficoltà nella percezione cromatica.
         */
        private static string CreaTestoStato(
            StatoRichiesta stato)
        {
            return stato switch
            {
                StatoRichiesta.Approvata =>
                    "Approvata",

                StatoRichiesta.Respinta =>
                    "Respinta",

                _ =>
                    "In attesa"
            };
        }
        private List<RigaMeseAnnualeViewModel> CreaVistaAnnuale(int anno)
        {
            var culturaItaliana = new CultureInfo("it-IT");

            var inizioAnno = new DateTime(
                anno,
                1,
                1);

            var fineAnno = inizioAnno.AddYears(1);

            /*
             * Carichiamo una sola volta tutte le festività dell'anno.
             *
             * Il dizionario ci permette di verificare velocemente
             * se ogni data della griglia annuale è festiva.
             */
            var festivitaAnno =
                CalendarioFestivitaItaliane.CreaPerAnno(
                    anno);

            /*
             * Recuperiamo tutti i giustificativi che intersecano
             * l'anno visualizzato.
             *
             * Il successivo metodo di filtro mantiene soltanto
             * le categorie condivisibili nel calendario:
             * ferie, permessi e Smart Working.
             */
            var richiesteAnno = _dbContext.Richieste
                .Where(r =>
                    r.TipoRichiesta ==
                        TipoRichiesta.Giustificativo &&

                    r.TipoGiustificativo != null &&

                    r.DataInizio < fineAnno &&

                    (r.DataFine ?? r.DataInizio) >=
                        inizioAnno)
                .ToList()

                /*
                 * Riutilizziamo la stessa regola già adottata
                 * nella vista mensile.
                 */
                .Where(
                    RichiestaDaMostrareNelCalendarioReparto)
                .ToList();

            /*
             * Creiamo un dizionario dei dipendenti indicizzato per Id.
             * Serve per recuperare rapidamente nome, matricola
             * e iniziali quando troviamo una richiesta.
             */
            var persone = _dbContext.Dipendenti
                .ToDictionary(d => d.Id);

            var righe =
                new List<RigaMeseAnnualeViewModel>();

            /*
             * La vista annuale contiene una riga per ogni mese.
             */
            for (var numeroMese = 1;
                 numeroMese <= 12;
                 numeroMese++)
            {
                var primoGiornoMese = new DateTime(
                    anno,
                    numeroMese,
                    1);

                var giorniNelMese =
                    DateTime.DaysInMonth(
                        anno,
                        numeroMese);

                var riga = new RigaMeseAnnualeViewModel
                {
                    NumeroMese = numeroMese,

                    NomeMese =
                        culturaItaliana.TextInfo.ToTitleCase(
                            primoGiornoMese.ToString(
                                "MMMM",
                                culturaItaliana)),

                    /*
                     * Evidenziamo il mese corrente soltanto
                     * quando stiamo consultando l'anno corrente.
                     */
                    MeseCorrente =
                        numeroMese == DateTime.Today.Month &&
                        anno == DateTime.Today.Year
                };

                /*
                 * Ogni riga ha sempre 31 colonne.
                 *
                 * Per febbraio, aprile, giugno, settembre e novembre
                 * alcune celle finali non rappresentano date reali.
                 */
                for (var giorno = 1;
                     giorno <= 31;
                     giorno++)
                {
                    /*
                     * Se il giorno non esiste nel mese,
                     * generiamo una cella disabilitata.
                     */
                    if (giorno > giorniNelMese)
                    {
                        riga.Celle.Add(
                            new CellaFerieAnnualeViewModel
                            {
                                Giorno = giorno,
                                Esiste = false
                            });

                        continue;
                    }

                    var data = new DateTime(
                        anno,
                        numeroMese,
                        giorno);

                    /*
                     * Cerchiamo la data tra le festività nazionali.
                     *
                     * Quando la data è normale, festivita sarà null.
                     * Quando è festiva conterrà nome e categoria.
                     */
                    festivitaAnno.TryGetValue(
                        data.Date,
                        out var festivita);

                    /*
                     * Raggruppiamo le richieste visibili in questa data
                     * in base al dipendente.
                     *
                     * In questo modo ogni persona compare una sola volta,
                     * anche nel caso di richieste sovrapposte.
                     */
                    var gruppiPersona = richiesteAnno   
                        .Where(r =>
                            RichiestaVisibileNelGiorno(
                                r,
                                data))
                        .GroupBy(r => r.DipendenteId)
                        .ToList();

                    /*
                     * Ogni gruppo contiene tutte le richieste
                     * dello stesso dipendente visibili nel giorno corrente.
                     */
                    var presenze = gruppiPersona
                        .Where(g =>
                            persone.ContainsKey(g.Key))
                        .Select(g =>
                        {
                            var persona =
                                persone[g.Key];

                            /*
                             * Potrebbero esistere più richieste della stessa persona
                             * nello stesso giorno.
                             *
                             * Scegliamo una richiesta rappresentativa con questa priorità:
                             * 1. Approvata;
                             * 2. In attesa;
                             * 3. Respinta.
                             *
                             * In questo modo tipologia, testo e colore
                             * appartengono tutti alla stessa richiesta.
                             */
                            var richiestaRappresentativa =
                                g.FirstOrDefault(r =>
                                    r.Stato ==
                                    StatoRichiesta.Approvata)

                                ?? g.FirstOrDefault(r =>
                                    r.Stato ==
                                    StatoRichiesta.InAttesa)

                                ?? g.First();

                            return new PersonaFerieAnnualeViewModel
                            {
                                Iniziali =
                                    persona.Iniziali,

                                Nominativo =
                                    persona.Nominativo,

                                Matricola =
                                    persona.Matricola,

                                /*
                                 * Qui non usiamo né r né g:
                                 * utilizziamo la singola richiesta scelta sopra.
                                 */
                                TipoRichiestaTesto =
                                    CreaEtichettaRichiestaReparto(
                                        richiestaRappresentativa),

                                Stato =
                                    richiestaRappresentativa
                                        .Stato
                                        .ToString(),

                                StatoTesto =
                                    CreaTestoStato(
                                        richiestaRappresentativa.Stato),

                                StatoCssClass =
                                    CreaClasseStato(
                                        richiestaRappresentativa.Stato)
                            };
                        })
                        .OrderBy(p =>
                            p.Nominativo)
                        .ToList();
                    /*
                     * Creiamo la cella reale del calendario annuale.
                     *
                     * Oltre a weekend, giorno corrente e persone,
                     * aggiungiamo le informazioni della festività.
                     */
                    riga.Celle.Add(
                        new CellaFerieAnnualeViewModel
                        {
                            Giorno = giorno,
                            Data = data,
                            Esiste = true,

                            Oggi =
                                data.Date == DateTime.Today,

                            Weekend =
                                data.DayOfWeek ==
                                    DayOfWeek.Saturday ||

                                data.DayOfWeek ==
                                    DayOfWeek.Sunday,

                            FestivoNazionale =
                                festivita != null,

                            NomeFestivita =
                                festivita?.Nome ??
                                string.Empty,

                            ClasseFestivita =
                                CreaClasseFestivita(
                                    festivita),

                            Persone = presenze
                        });
                }

                righe.Add(riga);
            }

            return righe;
        }
      
        private static string CreaClasseFestivita(
            FestivitaItaliana? festivita)
        {
            if (festivita == null)
            {
                return string.Empty;
            }

            return festivita.Categoria == CategoriaFestivita.Civile
                ? "reparto-holiday-civil"
                : "reparto-holiday-religious";
        }
    }
}