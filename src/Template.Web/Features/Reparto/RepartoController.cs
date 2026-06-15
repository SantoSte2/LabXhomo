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
        public virtual IActionResult Index(string vista  = "Mensile", int? mese = null, int? anno = null)
        {
            var oggi = DateTime.Today;
            var culturaItaliana = new CultureInfo("it-IT");

            var vistaNormalizzata = string.Equals(
                vista,
                "Annuale",
                StringComparison.OrdinalIgnoreCase)
                    ? "Annuale"
                    : "Mensile";

            var meseSelezionato = mese ?? oggi.Month;
            var annoSelezionato = anno ?? oggi.Year;

            var primoGiornoMese = new DateTime(
                annoSelezionato,
                meseSelezionato,
                1);

            var mesePrecedente = primoGiornoMese.AddMonths(-1);
            var meseSuccessivo = primoGiornoMese.AddMonths(1);

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

                NomeMese = culturaItaliana.TextInfo.ToTitleCase(
                    primoGiornoMese.ToString("MMMM", culturaItaliana)
                ),

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
                model.Giorni = CreaGiorniDelMese(primoGiornoMese);
                model.Persone = CreaRighePersone(primoGiornoMese);
            }

            return View(model);
        }

        private static List<GiornoRepartoViewModel> CreaGiorniDelMese(
            DateTime primoGiornoMese)
        {
            var culturaItaliana = new CultureInfo("it-IT");

            var giorniNelMese = DateTime.DaysInMonth(
                primoGiornoMese.Year,
                primoGiornoMese.Month);

            var giorni = new List<GiornoRepartoViewModel>();

            /*
             * Creiamo una sola volta l'elenco delle festività dell'anno.
             *
             * Il dizionario ha:
             * - come chiave la data della festività;
             * - come valore il nome e la categoria della festività.
             *
             * Evitiamo così di ricalcolare tutte le festività
             * per ogni singolo giorno del mese.
             */
            var festivitaAnno =
                CalendarioFestivitaItaliane.CreaPerAnno(
                    primoGiornoMese.Year);

            for (var giorno = 1;
                 giorno <= giorniNelMese;
                 giorno++)
            {
                var data = new DateTime(
                    primoGiornoMese.Year,
                    primoGiornoMese.Month,
                    giorno);

                /*
                 * Cerchiamo la data corrente nel dizionario.
                 *
                 * Se esiste, "festivita" conterrà ad esempio:
                 * - Natale;
                 * - Festa della Repubblica;
                 * - Pasqua;
                 * - ecc.
                 *
                 * Se non esiste, il valore sarà null.
                 */
                festivitaAnno.TryGetValue(
                    data.Date,
                    out var festivita);

                giorni.Add(new GiornoRepartoViewModel
                {
                    Data = data,

                    NumeroGiorno = giorno,

                    NomeGiornoBreve =
                        culturaItaliana.DateTimeFormat
                            .GetAbbreviatedDayName(
                                data.DayOfWeek),

                    Oggi =
                        data.Date == DateTime.Today,

                    Weekend =
                        data.DayOfWeek == DayOfWeek.Saturday ||
                        data.DayOfWeek == DayOfWeek.Sunday,

                    /*
                     * Queste proprietà verranno utilizzate dalla View
                     * per mostrare nome, tooltip e colore della festività.
                     */
                    FestivoNazionale =
                        festivita != null,

                    NomeFestivita =
                        festivita?.Nome ?? string.Empty,

                    ClasseFestivita =
                        CreaClasseFestivita(festivita)
                });
            }

            return giorni;
        }

        private List<RigaPersonaRepartoViewModel> CreaRighePersone(
     DateTime primoGiornoMese)
        {
            var fineMese =
                primoGiornoMese.AddMonths(1);

            /*
             * Anche qui carichiamo le festività una volta sola.
             *
             * Questo secondo utilizzo serve per applicare lo stile festivo
             * anche alle celle delle persone, non soltanto all'intestazione.
             */
            var festivitaAnno =
                CalendarioFestivitaItaliane.CreaPerAnno(
                    primoGiornoMese.Year);

            /*
             * Per questa pagina carichiamo esclusivamente:
             * - ferie ROL;
             * - ferie straordinarie.
             *
             * La condizione sulle date seleziona anche le richieste
             * che iniziano prima del mese ma terminano al suo interno.
             */
            var richiesteFerieMese = _dbContext.Richieste
                .Where(r =>
                    r.TipoRichiesta ==
                        TipoRichiesta.Giustificativo &&

                    (r.TipoGiustificativo ==
                        TipoGiustificativo.FerieRol ||

                     r.TipoGiustificativo ==
                        TipoGiustificativo.FerieStraordinarie) &&

                    r.DataInizio < fineMese &&

                    (r.DataFine ?? r.DataInizio) >=
                        primoGiornoMese)
                .ToList();

            /*
             * Ordiniamo prima per ruolo e successivamente
             * per cognome e nome.
             */
            var persone = _dbContext.Dipendenti
                .OrderBy(d => d.Ruolo)
                .ThenBy(d => d.Cognome)
                .ThenBy(d => d.Nome)
                .ToList();

            var righe =
                new List<RigaPersonaRepartoViewModel>();

            foreach (var persona in persone)
            {
                var riga = new RigaPersonaRepartoViewModel
                {
                    Id = persona.Id,
                    Nominativo = persona.Nominativo,
                    Matricola = persona.Matricola,
                    Ruolo = persona.Ruolo.ToString()
                };

                var giorniNelMese =
                    DateTime.DaysInMonth(
                        primoGiornoMese.Year,
                        primoGiornoMese.Month);

                for (var giorno = 1;
                     giorno <= giorniNelMese;
                     giorno++)
                {
                    var data = new DateTime(
                        primoGiornoMese.Year,
                        primoGiornoMese.Month,
                        giorno);

                    /*
                     * Verifichiamo se il giorno corrente è una festività.
                     * Le informazioni verranno poi copiate nella cella.
                     */
                    festivitaAnno.TryGetValue(
                        data.Date,
                        out var festivita);

                    /*
                     * Cerchiamo tutte le richieste della persona
                     * visibili nel giorno corrente.
                     */
                    var eventi = richiesteFerieMese
                        .Where(r =>
                            r.DipendenteId == persona.Id &&
                            RichiestaVisibileNelGiorno(
                                r,
                                data))
                        .Select(r =>
                            new EventoFerieRepartoViewModel
                            {
                                Testo =
                                    CreaEtichettaFerie(r),

                                StatoCssClass =
                                    CreaClasseStato(r.Stato)
                            })
                        .ToList();

                    riga.Celle.Add(
                        new CellaFerieRepartoViewModel
                        {
                            Data = data,

                            Oggi =
                                data.Date == DateTime.Today,

                            Weekend =
                                data.DayOfWeek ==
                                    DayOfWeek.Saturday ||

                                data.DayOfWeek ==
                                    DayOfWeek.Sunday,

                            /*
                             * Informazioni della festività associate
                             * alla singola cella della persona.
                             */
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

        private static string CreaEtichettaFerie(Richiesta richiesta)
        {
            return richiesta.TipoGiustificativo == TipoGiustificativo.FerieStraordinarie
                ? "Ferie straord."
                : "Ferie";
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
             * Recuperiamo soltanto le richieste di ferie che
             * intersecano almeno un giorno dell'anno selezionato.
             *
             * Una richiesta può:
             * - iniziare e finire nello stesso anno;
             * - iniziare nell'anno precedente;
             * - terminare nell'anno successivo.
             */
            var richiesteFerie = _dbContext.Richieste
                .Where(r =>
                    r.TipoRichiesta ==
                        TipoRichiesta.Giustificativo &&

                    (r.TipoGiustificativo ==
                        TipoGiustificativo.FerieRol ||

                     r.TipoGiustificativo ==
                        TipoGiustificativo.FerieStraordinarie) &&

                    r.DataInizio < fineAnno &&

                    (r.DataFine ?? r.DataInizio) >=
                        inizioAnno)
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
                    var gruppiPersona = richiesteFerie
                        .Where(r =>
                            RichiestaVisibileNelGiorno(
                                r,
                                data))
                        .GroupBy(r => r.DipendenteId)
                        .ToList();

                    /*
                     * Costruiamo le iniziali mostrate nella cella annuale.
                     */
                    var presenze = gruppiPersona
                        .Where(g =>
                            persone.ContainsKey(g.Key))
                        .Select(g =>
                        {
                            var persona = persone[g.Key];

                            /*
                             * Se esistono più richieste della stessa persona
                             * nello stesso giorno, scegliamo lo stato
                             * più significativo.
                             */
                            var stato =
                                ScegliStatoGiornaliero(g);

                            return new PersonaFerieAnnualeViewModel
                            {
                                Iniziali =
                                    persona.Iniziali,

                                Nominativo =
                                    persona.Nominativo,

                                Matricola =
                                    persona.Matricola,

                                Stato =
                                    stato.ToString(),

                                StatoCssClass =
                                    CreaClasseStato(stato)
                            };
                        })
                        .OrderBy(p => p.Nominativo)
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
        private static StatoRichiesta ScegliStatoGiornaliero(
            IEnumerable<Richiesta> richieste)
        {
            // Se esistono più richieste dello stesso dipendente nello stesso giorno,
            // mostriamo lo stato più significativo.
            if (richieste.Any(r => r.Stato == StatoRichiesta.Approvata))
            {
                return StatoRichiesta.Approvata;
            }

            if (richieste.Any(r => r.Stato == StatoRichiesta.InAttesa))
            {
                return StatoRichiesta.InAttesa;
            }

            return StatoRichiesta.Respinta;
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