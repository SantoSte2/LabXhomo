using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using Template.Enums;
using Template.Models;
using Template.Services;
using Template.Web.Services;
using System.Linq;
using System.Text.Json;

namespace Template.Web.Features.Dipendente
{
    [Authorize(Roles = "Dipendente")]
    public partial class DipendenteController : Controller
    {
        private readonly TemplateDbContext _dbContext;

        private const string RichiestaInConfermaSessionKey =
            "MarcTempo_RichiestaInConferma";

        public DipendenteController(TemplateDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public virtual IActionResult Index(int? mese = null, int? anno = null)
        {
            var oggi = DateTime.Today;
            var culturaItaliana = new CultureInfo("it-IT");

            var meseSelezionato = mese ?? oggi.Month;
            var annoSelezionato = anno ?? oggi.Year;

            var dataMeseSelezionato = new DateTime(annoSelezionato, meseSelezionato, 1);
            var mesePrecedente = dataMeseSelezionato.AddMonths(-1);
            var meseSuccessivo = dataMeseSelezionato.AddMonths(1);

            var dipendenteId = GetDipendenteIdDaSessione();

            if (dipendenteId == null)
            {
                return RedirectToAction("Login", "Login");
            }

            var totalizzatori = CalcolaTotalizzatoriDipendente(dipendenteId.Value);

            var eventiDelGiorno = CreaEventiDelGiorno(dipendenteId.Value);

            var giorniCalendario = CreaCalendarioMensile(
                dipendenteId.Value,
                meseSelezionato,
                annoSelezionato
            );

            var inizioMeseSelezionato = new DateTime(annoSelezionato, meseSelezionato, 1);
            var fineMeseSelezionato = inizioMeseSelezionato.AddMonths(1);

            // Mostriamo solo le richieste che cadono nel mese selezionato dal calendario.
            var ultimeRichieste = _dbContext.Richieste
                .Where(r =>
                    r.DipendenteId == dipendenteId.Value &&
                    r.DataInizio < fineMeseSelezionato &&
                    (r.DataFine ?? r.DataInizio) >= inizioMeseSelezionato)
                .OrderByDescending(r => r.DataCreazione)
                .Select(r => new RichiestaStoricoViewModel
                {
                    Id = r.Id,
                    Data = CreaPeriodo(r),
                    Tipo = r.TipoRichiesta == TipoRichiesta.Giustificativo
                        ? "Giustificativo"
                        : "Mancata timbratura",
                    Dettaglio = r.TipoRichiesta == TipoRichiesta.Giustificativo
                        ? (r.TipoGiustificativo != null ? r.TipoGiustificativo.ToString()! : "-")
                        : (r.TipoMancataTimbratura != null ? r.TipoMancataTimbratura.ToString()! : "-"),
                    Orario = CreaOrario(r),
                    Stato = r.Stato.ToString(),
                    StatoCssClass = CreaClasseStato(r.Stato),

                    /*
                     * La motivazione viene lasciata vuota
                     * finché la richiesta è ancora in attesa.
                     */
                    MotivazioneEsito =
                        string.IsNullOrWhiteSpace(
                            r.MotivazioneEsito)
                            ? string.Empty
                            : r.MotivazioneEsito,

                    ValutataDa =
                        string.IsNullOrWhiteSpace(
                            r.ValutataDa)
                            ? string.Empty
                            : r.ValutataDa,

                    DataValutazione =
                        r.DataValutazione.HasValue
                            ? r.DataValutazione.Value
                                .ToString("dd/MM/yyyy HH:mm")
                            : string.Empty


                })
                .ToList();

            var richiesteInAttesa = ultimeRichieste.Count(r => r.Stato == StatoRichiesta.InAttesa.ToString());

            var model = new HomeDipendenteViewModel
            {
                Nominativo = HttpContext.Session.GetString("Nominativo") ?? "Dipendente",
                Matricola = HttpContext.Session.GetString("Matricola") ?? "-",
                Iniziali = HttpContext.Session.GetString("Iniziali") ?? "DP",

                // I totalizzatori fanno sempre riferimento alla situazione del mese corrente,
                // indipendentemente dal mese consultato nel calendario.
                MeseTotalizzatori = culturaItaliana.TextInfo.ToTitleCase(
                    oggi.ToString("MMMM", culturaItaliana)
                ),
                AnnoTotalizzatori = oggi.Year,

                MeseCorrente = culturaItaliana.TextInfo.ToTitleCase(
                    dataMeseSelezionato.ToString("MMMM", culturaItaliana)
                ),
                AnnoCorrente = annoSelezionato,

                MeseSelezionato = meseSelezionato,
                AnnoSelezionato = annoSelezionato,

                MesePrecedente = mesePrecedente.Month,
                AnnoMesePrecedente = mesePrecedente.Year,

                MeseSuccessivo = meseSuccessivo.Month,
                AnnoMeseSuccessivo = meseSuccessivo.Year,

                GiorniCalendario = giorniCalendario,

                SaldoPermessoStudioOre = totalizzatori.SaldoPermessoStudioOre,
                SaldoFerieRolGiorni = totalizzatori.SaldoFerieRolGiorni,
                SaldoFerieRolOre = totalizzatori.SaldoFerieRolOre,
                SaldoRecuperoOre = totalizzatori.SaldoRecuperoOre,

                EventiDelGiorno = eventiDelGiorno,

                UltimeRichieste = ultimeRichieste
            };

            return View(model);
        }

        [HttpGet]
        public virtual IActionResult Richiesta(string tipo = "Giustificativo")
        {
            var model = new RichiestaViewModel
            {
                TipoSezione = NormalizzaTipoSezione(tipo),
                Nominativo = HttpContext.Session.GetString("Nominativo") ?? "Dipendente",
                Matricola = HttpContext.Session.GetString("Matricola") ?? "-",
                Iniziali = HttpContext.Session.GetString("Iniziali") ?? "DP",
                DataInizio = DateTime.Today,
                DataFine = DateTime.Today,
                TipiGiustificativo = CreaSelectList<TipoGiustificativo>(),
                TipiMancataTimbratura = CreaSelectList<TipoMancataTimbratura>()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public virtual IActionResult Richiesta(RichiestaViewModel model)
        {
            model.TipoSezione = NormalizzaTipoSezione(model.TipoSezione);
            PopolaDatiComuni(model);
            PopolaSelectList(model);

            if (model.TipoSezione == "Giustificativo")
            {
                ValidaGiustificativo(model);
            }
            else
            {
                ValidaMancataTimbratura(model);
            }

            if (model.TipoSezione == "Giustificativo")
            {
                ValidaSaldoFerie(model);
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var dipendenteId = GetDipendenteIdDaSessione();

            if (dipendenteId == null)
            {
                return RedirectToAction("Login", "Login");
            }



            /*
             * La richiesta è valida, ma non viene ancora registrata nel database.
             * Rimane nella sessione fino alla conferma esplicita dell'utente.
             */
            var richiestaTemporanea = new RichiestaInConfermaSessionModel
            {
                TipoSezione = model.TipoSezione,

                TipoGiustificativo =
                    model.TipoSezione == "Giustificativo"
                        ? model.TipoGiustificativo
                        : null,

                TipoMancataTimbratura =
                    model.TipoSezione == "MancataTimbratura"
                        ? model.TipoMancataTimbratura
                        : null,

                DataInizio = model.DataInizio!.Value,
                DataFine = model.DataFine,
                OraInizio = model.OraInizio,
                OraFine = model.OraFine,
                PostNotturno = model.PostNotturno,
                Motivazione = model.Motivazione,
                OreDichiarate = CalcolaOre(model)
            };

            SalvaRichiestaInConferma(richiestaTemporanea);

            /*
             * PRG: il POST non restituisce direttamente la View,
             * ma reindirizza alla pagina GET di riepilogo.
             */
            return RedirectToAction(nameof(Conferma));
        }

        [HttpGet]
        public virtual IActionResult Conferma()
        {
            var richiestaTemporanea = LeggiRichiestaInConferma();

            if (richiestaTemporanea == null)
            {
                return RedirectToAction(nameof(Richiesta));
            }

            var model = new ConfermaRichiestaViewModel
            {
                Nominativo =
                    HttpContext.Session.GetString("Nominativo")
                    ?? "Dipendente",

                Matricola =
                    HttpContext.Session.GetString("Matricola")
                    ?? "-",

                Iniziali =
                    HttpContext.Session.GetString("Iniziali")
                    ?? "DP",

                TipoRichiesta =
                    richiestaTemporanea.TipoSezione == "Giustificativo"
                        ? "Giustificativo"
                        : "Mancata timbratura",

                DettaglioRichiesta =
                    richiestaTemporanea.TipoSezione == "Giustificativo"
                        ? richiestaTemporanea.TipoGiustificativo?.ToString() ?? "-"
                        : richiestaTemporanea.TipoMancataTimbratura?.ToString() ?? "-",

                Periodo = CreaPeriodo(
                    richiestaTemporanea.DataInizio,
                    richiestaTemporanea.DataFine),

                Orario = CreaOrario(
                    richiestaTemporanea.OraInizio,
                    richiestaTemporanea.OraFine),

                Motivazione =
                    string.IsNullOrWhiteSpace(richiestaTemporanea.Motivazione)
                        ? "-"
                        : richiestaTemporanea.Motivazione,

                Stato = "In attesa di conferma"
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public virtual IActionResult ConfermaModifiche()
        {
            var richiestaTemporanea = LeggiRichiestaInConferma();
            var dipendenteId = GetDipendenteIdDaSessione();

            if (richiestaTemporanea == null || dipendenteId == null)
            {
                EliminaRichiestaInConferma();

                return RedirectToAction(
                    "Login",
                    "Login");
            }

            /*
             * Solo in questo punto la richiesta viene registrata
             * definitivamente e diventa visibile nel resto del sistema.
             */
            var richiesta = new Richiesta
            {
                DipendenteId = dipendenteId.Value,

                TipoRichiesta =
                    richiestaTemporanea.TipoSezione == "Giustificativo"
                        ? TipoRichiesta.Giustificativo
                        : TipoRichiesta.MancataTimbratura,

                TipoGiustificativo =
                    richiestaTemporanea.TipoSezione == "Giustificativo"
                        ? richiestaTemporanea.TipoGiustificativo
                        : null,

                TipoMancataTimbratura =
                    richiestaTemporanea.TipoSezione == "MancataTimbratura"
                        ? richiestaTemporanea.TipoMancataTimbratura
                        : null,

                DataInizio = richiestaTemporanea.DataInizio,
                DataFine = richiestaTemporanea.DataFine,
                OraInizio = richiestaTemporanea.OraInizio,
                OraFine = richiestaTemporanea.OraFine,
                PostNotturno = richiestaTemporanea.PostNotturno,
                Motivazione = richiestaTemporanea.Motivazione,
                OreDichiarate = richiestaTemporanea.OreDichiarate,

                Stato = StatoRichiesta.InAttesa,
                DataCreazione = DateTime.Now
            };

            _dbContext.Richieste.Add(richiesta);
            _dbContext.SaveChanges();

            /*
             * Rimuoviamo la bozza dalla sessione.
             * Un secondo invio non può quindi generare un duplicato.
             */
            EliminaRichiestaInConferma();

            /*
             * Dopo il salvataggio definitivo torniamo alla nuova Home comune,
             * cioè il calendario Ferie Reparto.
             *
             * Non utilizziamo nameof(Index), perché quello indicherebbe
             * l'Index del DipendenteController, cioè la vecchia Home.
             */
            return RedirectToAction(
                "Index",
                "Reparto");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public virtual IActionResult AnnullaModifiche()
        {
            var richiestaTemporanea = LeggiRichiestaInConferma();

            /*
             * La bozza viene eliminata senza mai entrare nel database.
             */
            EliminaRichiestaInConferma();

            return RedirectToAction(
                nameof(Richiesta),
                new
                {
                    tipo = richiestaTemporanea?.TipoSezione
                           ?? "Giustificativo"
                });
            //ALTRA RETURN POSSIBILE, DIRETTAMENTE ALLA INDEX -> return RedirectToAction(nameof(Index));
        }

        private static string CreaPeriodo(
            DateTime dataInizio,
            DateTime? dataFine)
        {
            var inizio = dataInizio.ToString("dd/MM/yyyy");

            if (dataFine == null)
            {
                return inizio;
            }

            var fine = dataFine.Value.ToString("dd/MM/yyyy");

            return inizio == fine
                ? inizio
                : $"{inizio} - {fine}";
        }

        private static string CreaOrario(
            TimeSpan? oraInizio,
            TimeSpan? oraFine)
        {
            var inizio = oraInizio?.ToString(@"hh\:mm") ?? "-";
            var fine = oraFine?.ToString(@"hh\:mm");

            return string.IsNullOrWhiteSpace(fine)
                ? inizio
                : $"{inizio} - {fine}";
        }

        private void ValidaSaldoFerie(RichiestaViewModel model)
        {
            if (model.TipoGiustificativo != TipoGiustificativo.FerieRol)
            {
                return;
            }

            var dipendenteId = GetDipendenteIdDaSessione();

            if (dipendenteId == null)
            {
                return;
            }

            var oreRichieste = CalcolaOre(model) ?? 0;
            var totalizzatori = CalcolaTotalizzatoriDipendente(dipendenteId.Value);

            if (oreRichieste > totalizzatori.SaldoFerieRolOre)
            {
                var oreEccedenti = oreRichieste - totalizzatori.SaldoFerieRolOre;

                ModelState.AddModelError(
                    nameof(model.TipoGiustificativo),
                    $"Saldo ferie insufficiente. La richiesta supera il saldo disponibile di {oreEccedenti:0.##} ore. Usa 'Ferie Straordinarie' se vuoi inviare comunque la richiesta."
                );
            }
        }

        private void SalvaRichiestaInConferma(
            RichiestaInConfermaSessionModel richiesta)
        {
            var json = JsonSerializer.Serialize(richiesta);

            HttpContext.Session.SetString(
                RichiestaInConfermaSessionKey,
                json);
        }

        private RichiestaInConfermaSessionModel? LeggiRichiestaInConferma()
        {
            var json = HttpContext.Session.GetString(
                RichiestaInConfermaSessionKey);

            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            return JsonSerializer.Deserialize<RichiestaInConfermaSessionModel>(
                json);
        }

        private void EliminaRichiestaInConferma()
        {
            HttpContext.Session.Remove(
                RichiestaInConfermaSessionKey);
        }

        /*
         * Costruisce il pannello "Eventi del giorno"
         * mostrato nella Home Dipendente.
         *
         * Per le ferie utilizziamo la stessa regola del calendario:
         * sabati, domeniche e festività nazionali non devono
         * essere visualizzati come giorni di ferie.
         */
        private List<EventoGiornoViewModel> CreaEventiDelGiorno(
            int dipendenteId)
        {
            var oggi = DateTime.Today;

            /*
             * Prima recuperiamo dal database le richieste
             * che comprendono la data odierna.
             */
            var richiesteOggi = _dbContext.Richieste
                .Where(r =>
                    r.DipendenteId == dipendenteId &&
                    r.DataInizio.Date <= oggi &&
                    (r.DataFine ?? r.DataInizio).Date >= oggi)
                .OrderByDescending(r => r.DataCreazione)
                .ToList();

            /*
             * Il controllo del giorno lavorativo viene eseguito
             * dopo il caricamento dal database, perché utilizza
             * il servizio delle festività italiane.
             */
            return richiesteOggi
                .Where(r =>
                    RichiestaVisibileNelCalendarioPersonale(
                        r,
                        oggi))
                .Select(r => new EventoGiornoViewModel
                {
                    Tipo =
                        r.TipoRichiesta == TipoRichiesta.Giustificativo
                            ? "Giustificativo"
                            : "Mancata timbratura",

                    Dettaglio =
                        r.TipoRichiesta == TipoRichiesta.Giustificativo
                            ? r.TipoGiustificativo?.ToString() ?? "-"
                            : r.TipoMancataTimbratura?.ToString() ?? "-",

                    Periodo =
                        CreaPeriodo(r),

                    Orario =
                        CreaOrario(r),

                    Stato =
                        r.Stato.ToString(),

                    StatoCssClass =
                        CreaClasseStato(r.Stato)
                })
                .ToList();
        }



        private List<GiornoCalendarioViewModel> CreaCalendarioMensile(int dipendenteId, int mese, int anno)
        {
            var primoGiornoDelMese = new DateTime(anno, mese, 1);
            var fineMese = primoGiornoDelMese.AddMonths(1);

            var offsetInizio = ((int)primoGiornoDelMese.DayOfWeek + 6) % 7;
            var inizioCalendario = primoGiornoDelMese.AddDays(-offsetInizio);

            const int giorniTotali = 42;

            var richiesteMese = _dbContext.Richieste
                .Where(r =>
                    r.DipendenteId == dipendenteId &&
                    r.DataInizio < fineMese &&
                    (r.DataFine ?? r.DataInizio) >= primoGiornoDelMese)
                .ToList();
            /*
             * Carichiamo una sola volta le festività dell'anno visualizzato.
             * Serviranno sia per escludere le ferie dai giorni festivi,
             * sia successivamente per colorare le festività nel calendario.
             */
            var festivitaAnno =
                CalendarioFestivitaItaliane.CreaPerAnno(anno);

            var giorni = new List<GiornoCalendarioViewModel>();

            for (var i = 0; i < giorniTotali; i++)
            {
                var data = inizioCalendario.AddDays(i);

                /*
                 * Recuperiamo le richieste che devono essere realmente
                 * mostrate nella cella del giorno corrente.
                 *
                 * Per ferie ROL e ferie straordinarie vengono esclusi:
                 * - sabato;
                 * - domenica;
                 * - festività nazionali.
                 *
                 * Le altre richieste continuano invece a essere mostrate
                 * normalmente nella data a cui si riferiscono.
                 */

                /*
                 * Verifichiamo se la data della cella è una festività nazionale.
                 * Il valore sarà null nei giorni normali.
                 */
                festivitaAnno.TryGetValue(
                    data.Date,
                    out var festivita);


                var richiesteDelGiorno = richiesteMese
                    .Where(r =>
                        RichiestaVisibileNelCalendarioPersonale(
                            r,
                            data))
                    .ToList();

                /*
                 * Convertiamo le richieste recuperate dal database
                 * negli eventi grafici utilizzati dal calendario.
                 *
                 * Ogni evento contiene:
                 * - un testo sintetico che identifica la richiesta;
                 * - la classe CSS corrispondente allo stato.
                 */
                var eventiGiorno = richiesteDelGiorno
                    .Select(r => new EventoCalendarioViewModel
                    {
                        Testo = CreaEtichettaCalendario(r),

                        StatoCssClass =
                            CreaClasseStato(r.Stato)
                    })
                    .ToList();


                giorni.Add(new GiornoCalendarioViewModel
                {
                    NumeroGiorno = data.Day,
                    Data = data,

                    AppartieneAlMese = data.Month == mese &&
                        data.Year == anno,

                    Oggi = data.Date == DateTime.Today,

                    /*
                     * La data è festiva quando è stata trovata
                     * nel dizionario delle festività italiane.
                     */
                    FestivoNazionale =
                        festivita != null,

                    /*
                     * Nei giorni normali salviamo una stringa vuota.
                     */
                    NomeFestivita =
                    festivita != null
                        ? festivita.Nome
                        : string.Empty,

                    /*
                     * La classe sarà:
                     * - marc-holiday-civil;
                     * - marc-holiday-religious;
                     * - stringa vuota nei giorni normali.
                     */
                    ClasseFestivita =
                        CreaClasseFestivita(festivita),

                    Eventi =    eventiGiorno
                });
            }

            return giorni;
        }

        /*
         * Converte la categoria della festività
         * nella classe CSS utilizzata dalla Home Dipendente.
         */
        private static string CreaClasseFestivita(
            FestivitaItaliana festivita)
        {
            /*
             * Nei giorni normali non aggiungiamo
             * alcuna classe CSS.
             */
            if (festivita == null)
            {
                return string.Empty;
            }

            /*
             * Le festività civili saranno blu,
             * quelle religiose saranno viola.
             */
            if (festivita.Categoria ==
                    CategoriaFestivita.Civile)
            {
                return "marc-holiday-civil";
            }

            return "marc-holiday-religious";
        }

        
        private int? GetDipendenteIdDaSessione()
        {
            return HttpContext.Session.GetInt32("DipendenteId");
        }

        private void PopolaDatiComuni(RichiestaViewModel model)
        {
            model.Nominativo = HttpContext.Session.GetString("Nominativo") ?? "Dipendente";
            model.Matricola = HttpContext.Session.GetString("Matricola") ?? "-";
            model.Iniziali = HttpContext.Session.GetString("Iniziali") ?? "DP";
        }

        private void PopolaSelectList(RichiestaViewModel model)
        {
            model.TipiGiustificativo = CreaSelectList<TipoGiustificativo>();
            model.TipiMancataTimbratura = CreaSelectList<TipoMancataTimbratura>();
        }

        private static string NormalizzaTipoSezione(string tipo)
        {
            return tipo == "MancataTimbratura"
                ? "MancataTimbratura"
                : "Giustificativo";
        }

        private void ValidaGiustificativo(RichiestaViewModel model)
        {
            if (model.TipoGiustificativo == null)
            {
                ModelState.AddModelError(nameof(model.TipoGiustificativo), "Seleziona un giustificativo");
            }

            if (model.DataInizio == null)
            {
                ModelState.AddModelError(nameof(model.DataInizio), "Inserisci la data di inizio");
            }

            if (model.DataFine == null)
            {
                ModelState.AddModelError(nameof(model.DataFine), "Inserisci la data di fine");
            }

            if (model.DataInizio != null && model.DataFine != null && model.DataFine < model.DataInizio)
            {
                ModelState.AddModelError(nameof(model.DataFine), "La data di fine non può essere precedente alla data di inizio");
            }

            if (model.OraInizio == null)
            {
                ModelState.AddModelError(nameof(model.OraInizio), "Inserisci l'ora di inizio");
            }

            if (model.OraFine == null)
            {
                ModelState.AddModelError(nameof(model.OraFine), "Inserisci l'ora di fine");
            }

            if (model.OraInizio != null && model.OraFine != null && model.OraFine <= model.OraInizio)
            {
                ModelState.AddModelError(nameof(model.OraFine), "L'ora di fine deve essere successiva all'ora di inizio");
            }
        }

        private void ValidaMancataTimbratura(RichiestaViewModel model)
        {
            if (model.TipoMancataTimbratura == null)
            {
                ModelState.AddModelError(nameof(model.TipoMancataTimbratura), "Seleziona la tipologia");
            }

            if (model.DataInizio == null)
            {
                ModelState.AddModelError(nameof(model.DataInizio), "Inserisci il giorno");
            }

            if (model.OraInizio == null)
            {
                ModelState.AddModelError(nameof(model.OraInizio), "Inserisci l'ora");
            }

            if (string.IsNullOrWhiteSpace(model.Motivazione))
            {
                ModelState.AddModelError(nameof(model.Motivazione), "Inserisci una motivazione");
            }
        }

        private TotalizzatoriDipendente CalcolaTotalizzatoriDipendente(int dipendenteId)
        {
            const decimal permessoStudioInizialeOre = 120;
            const decimal ferieRolInizialiOre = 120;
            const decimal recuperoInizialeOre = 2;
            const decimal orePerGiorno = 8;

            var saldoPermessoStudioOre = permessoStudioInizialeOre;
            var saldoFerieRolOre = ferieRolInizialiOre;
            var saldoRecuperoOre = recuperoInizialeOre;

            var richiesteDaConsiderare = _dbContext.Richieste
                .Where(r =>
                    r.DipendenteId == dipendenteId &&
                    r.Stato != StatoRichiesta.Respinta)
                .ToList();

            foreach (var richiesta in richiesteDaConsiderare)
            {
                if (richiesta.TipoRichiesta != TipoRichiesta.Giustificativo ||
                    richiesta.TipoGiustificativo == null)
                {
                    continue;
                }

                var ore = richiesta.OreDichiarate ?? CalcolaOreDaRichiesta(richiesta);

                if (ore <= 0)
                {
                    continue;
                }

                switch (richiesta.TipoGiustificativo.Value)
                {
                    case TipoGiustificativo.PermessoStudio:
                        saldoPermessoStudioOre -= ore;
                        break;

                    case TipoGiustificativo.FerieRol:
                        saldoFerieRolOre -= ore;
                        break;

                    case TipoGiustificativo.OreRecuperoGodute:
                        saldoRecuperoOre -= ore;
                        break;

                    case TipoGiustificativo.OreRecuperoMaturate:
                    case TipoGiustificativo.Straordinario:
                        saldoRecuperoOre += ore;
                        break;
                            
                    case TipoGiustificativo.FerieStraordinarie:
                        saldoFerieRolOre -= ore;
                        break;
                }
            }

            saldoPermessoStudioOre = Math.Max(0, saldoPermessoStudioOre);

            // Il saldo ferie può andare in negativo solo se vengono usate Ferie Straordinarie.
            saldoRecuperoOre = Math.Max(0, saldoRecuperoOre);

            return new TotalizzatoriDipendente
            {
                SaldoPermessoStudioOre = saldoPermessoStudioOre,
                SaldoFerieRolOre = saldoFerieRolOre,
                SaldoFerieRolGiorni = Math.Round(saldoFerieRolOre / orePerGiorno, 2),
                SaldoRecuperoOre = saldoRecuperoOre
            };
        }

        private static decimal CalcolaOreDaRichiesta(Richiesta richiesta)
        {
            if (richiesta.OraInizio == null || richiesta.OraFine == null)
            {
                return 0;
            }

            var differenza = richiesta.OraFine.Value - richiesta.OraInizio.Value;

            if (differenza.TotalMinutes <= 0)
            {
                return 0;
            }

            var dataFine = richiesta.DataFine ?? richiesta.DataInizio;
            var giorniDaConteggiare = CalcolaGiorniDaConteggiare(
                richiesta.DataInizio,
                dataFine,
                richiesta.TipoGiustificativo
            );

            if (giorniDaConteggiare <= 0)
            {
                return 0;
            }

            var oreGiornaliere = (decimal)differenza.TotalHours;
            var oreTotali = oreGiornaliere * giorniDaConteggiare;

            return Math.Round(oreTotali, 2);
        }

        private static decimal? CalcolaOre(RichiestaViewModel model)
        {
            if (model.OraInizio == null || model.OraFine == null || model.DataInizio == null)
            {
                return null;
            }

            var differenza = model.OraFine.Value - model.OraInizio.Value;

            if (differenza.TotalMinutes <= 0)
            {
                return null;
            }

            var dataFine = model.DataFine ?? model.DataInizio;
            var giorniDaConteggiare = CalcolaGiorniDaConteggiare(
                model.DataInizio.Value,
                dataFine.Value,
                model.TipoGiustificativo
            );

            if (giorniDaConteggiare <= 0)
            {
                return null;
            }

            var oreGiornaliere = (decimal)differenza.TotalHours;
            var oreTotali = oreGiornaliere * giorniDaConteggiare;

            return Math.Round(oreTotali, 2);
        }

        private static int CalcolaGiorniDaConteggiare(
            DateTime dataInizio,
            DateTime dataFine,
            TipoGiustificativo? tipoGiustificativo)
        {
            if (dataFine.Date < dataInizio.Date)
            {
                return 0;
            }

            var calcolaSoloGiorniLavorativi =
                tipoGiustificativo == TipoGiustificativo.FerieRol ||
                tipoGiustificativo == TipoGiustificativo.FerieStraordinarie;

            if (!calcolaSoloGiorniLavorativi)
            {
                return (dataFine.Date - dataInizio.Date).Days + 1;
            }

            return ContaGiorniLavorativi(dataInizio, dataFine);
        }

        /*
         * Il controller non contiene più una propria regola
         * per decidere quali giorni siano lavorativi.
         *
         * Tutta la logica viene delegata al servizio condiviso,
         * così calendario, saldo ferie e validazioni utilizzano
         * sempre le stesse condizioni.
         */
        private static int ContaGiorniLavorativi(
            DateTime dataInizio,
            DateTime dataFine)
        {
            return CalendarioFestivitaItaliane
                .ContaGiorniLavorativi(
                    dataInizio,
                    dataFine);
        }   
        private static string CreaDettaglioRichiesta(Richiesta richiesta)
        {
            if (richiesta.TipoRichiesta == TipoRichiesta.Giustificativo)
            {
                return richiesta.TipoGiustificativo?.ToString() ?? "-";
            }

            return richiesta.TipoMancataTimbratura?.ToString() ?? "-";
        }

        private static string CreaPeriodo(Richiesta richiesta)
        {
            var inizio = richiesta.DataInizio.ToString("dd/MM/yyyy");

            if (richiesta.DataFine == null)
            {
                return inizio;
            }

            var fine = richiesta.DataFine.Value.ToString("dd/MM/yyyy");

            return inizio == fine
                ? inizio
                : $"{inizio} - {fine}";
        }

        private static string CreaOrario(Richiesta richiesta)
        {
            var oraInizio = richiesta.OraInizio?.ToString(@"hh\:mm") ?? "-";
            var oraFine = richiesta.OraFine?.ToString(@"hh\:mm");

            if (string.IsNullOrWhiteSpace(oraFine))
            {
                return oraInizio;
            }

            return $"{oraInizio} - {oraFine}";
        }

        private static IEnumerable<SelectListItem> CreaSelectList<TEnum>() where TEnum : struct, Enum
        {
            return Enum.GetValues<TEnum>()
                .Select(value => new SelectListItem
                {
                    Value = value.ToString(),
                    Text = value.ToString()
                })
                .ToList();
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
        private class TotalizzatoriDipendente
        {
            public decimal SaldoPermessoStudioOre { get; set; }

            public decimal SaldoFerieRolGiorni { get; set; }

            public decimal SaldoFerieRolOre { get; set; }

            public decimal SaldoRecuperoOre { get; set; }
        }

        /*
         * Stabilisce se una richiesta deve essere mostrata
         * in un determinato giorno del calendario personale.
         *
         * La regola è diversa a seconda del tipo di richiesta:
         *
         * - ferie ROL e ferie straordinarie vengono mostrate
         *   soltanto nei giorni lavorativi;
         *
         * - le altre richieste, come una mancata timbratura,
         *   possono essere mostrate anche in un giorno festivo,
         *   perché fanno riferimento a una data precisa.
         */
        private static bool RichiestaVisibileNelCalendarioPersonale(
            Richiesta richiesta,
            DateTime data)
        {
            /*
             * Prima controlliamo che la data appartenga
             * all'intervallo della richiesta.
             */
            var dataInizio = richiesta.DataInizio.Date;

            var dataFine =
                (richiesta.DataFine ?? richiesta.DataInizio).Date;

            var dentroIlPeriodo =
                data.Date >= dataInizio &&
                data.Date <= dataFine;

            if (!dentroIlPeriodo)
            {
                return false;
            }

            /*
             * Individuiamo le richieste che rappresentano ferie.
             *
             * Soltanto per queste richieste dobbiamo escludere:
             * - sabato;
             * - domenica;
             * - festività nazionali.
             */
            var richiestaDiFerie =
                richiesta.TipoRichiesta ==
                    TipoRichiesta.Giustificativo &&

                (richiesta.TipoGiustificativo ==
                     TipoGiustificativo.FerieRol ||

                 richiesta.TipoGiustificativo ==
                     TipoGiustificativo.FerieStraordinarie);

            /*
             * Una richiesta non relativa alle ferie rimane visibile
             * normalmente nel giorno indicato.
             */
            if (!richiestaDiFerie)
            {
                return true;
            }

            /*
             * Le ferie vengono mostrate soltanto quando
             * la data è effettivamente lavorativa.
             */
            return CalendarioFestivitaItaliane
                .EGiornoLavorativo(data);
        }
        /*
 * Restituisce il testo breve mostrato
 * all'interno del calendario personale.
 *
 * Per i giustificativi controlliamo il tipo specifico,
 * mentre per le mancate timbrature utilizziamo
 * la relativa causale.
 */
        private static string CreaEtichettaCalendario(
            Richiesta richiesta)
        {
            /*
             * Gestione dei giustificativi.
             */
            if (richiesta.TipoRichiesta ==
                TipoRichiesta.Giustificativo)
            {
                return richiesta.TipoGiustificativo switch
                {
                    TipoGiustificativo.FerieRol =>
                        "Ferie/ROL",

                    TipoGiustificativo.FerieStraordinarie =>
                        "Ferie straordinarie",

                    /*
                     * Per gli altri giustificativi utilizziamo
                     * direttamente il nome del valore enum.
                     */
                    _ =>
                        richiesta.TipoGiustificativo?.ToString()
                        ?? "Giustificativo"
                };
            }

            /*
             * Gestione delle mancate timbrature.
             *
             * Se è presente una tipologia specifica,
             * viene mostrata nel calendario.
             */
            return richiesta.TipoMancataTimbratura?.ToString()
                   ?? "Mancata timbratura";
        }
    }
}