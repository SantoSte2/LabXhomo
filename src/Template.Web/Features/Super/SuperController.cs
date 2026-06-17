using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using Template.Enums;
using Template.Models;
using Template.Services;

namespace Template.Web.Features.Super
{
    [Authorize(Roles = "Super")]
    public partial class SuperController : Controller
    {
        private readonly TemplateDbContext _dbContext;

        public SuperController(TemplateDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public virtual IActionResult Index(string searchTerm = "", int? dipendenteId = null)
        {
            var queryDipendenti = _dbContext.Dipendenti
                .Where(d => d.Ruolo == RuoloUtente.Dipendente);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var termine = searchTerm.Trim().ToLower();

                queryDipendenti = queryDipendenti.Where(d =>
                    d.Nome.ToLower().Contains(termine) ||
                    d.Cognome.ToLower().Contains(termine) ||
                    d.Matricola.ToLower().Contains(termine));
            }

            var dipendenti = queryDipendenti
                .OrderBy(d => d.Cognome)
                .ThenBy(d => d.Nome)
                .Select(d => new DipendenteRicercaViewModel
                {
                    Id = d.Id,
                    Matricola = d.Matricola,
                    Nome = d.Nome,
                    Cognome = d.Cognome
                })
                .ToList();

            var model = new SuperDashboardViewModel
            {
                NominativoSuper = HttpContext.Session.GetString("Nominativo") ?? "Super",
                MatricolaSuper = HttpContext.Session.GetString("Matricola") ?? "-",
                InizialiSuper = HttpContext.Session.GetString("Iniziali") ?? "SP",
                SearchTerm = searchTerm,
                DipendenteSelezionatoId = dipendenteId,
                Dipendenti = dipendenti
            };

            if (dipendenteId != null)
            {
                model.DipendenteSelezionato = _dbContext.Dipendenti
                    .Where(d => d.Id == dipendenteId.Value)
                    .Select(d => new DipendenteRicercaViewModel
                    {
                        Id = d.Id,
                        Matricola = d.Matricola,
                        Nome = d.Nome,
                        Cognome = d.Cognome
                    })
                    .FirstOrDefault();

                model.Richieste = _dbContext.Richieste
                    .Include(r => r.Dipendente)
                    .Where(r => r.DipendenteId == dipendenteId.Value)
                    .OrderByDescending(r => r.DataCreazione)
                    .Select(r => new RichiestaSuperViewModel
                    {
                        Id = r.Id,
                        Tipo = r.TipoRichiesta == TipoRichiesta.Giustificativo
                            ? "Giustificativo"
                            : "Mancata timbratura",

                        Dettaglio = r.TipoRichiesta == TipoRichiesta.Giustificativo
                            ? (r.TipoGiustificativo != null ? r.TipoGiustificativo.ToString()! : "-")
                            : (r.TipoMancataTimbratura != null ? r.TipoMancataTimbratura.ToString()! : "-"),

                        Data = CreaPeriodo(r),
                        Orario = CreaOrario(r),
                        Motivazione = string.IsNullOrWhiteSpace(r.Motivazione) ? "-" : r.Motivazione,
                        /*
                         * Esito della valutazione del Super.
                         */
                        MotivazioneEsito =
                            string.IsNullOrWhiteSpace(r.MotivazioneEsito)
                                ? "-"
                                : r.MotivazioneEsito,

                        DataValutazione =
                            r.DataValutazione.HasValue
                                ? r.DataValutazione.Value
                                    .ToString("dd/MM/yyyy HH:mm")
                                : "-",

                        ValutataDa =
                            string.IsNullOrWhiteSpace(r.ValutataDa)
                                ? "-"
                                : r.ValutataDa,

                        Stato = r.Stato.ToString(),
                        StatoCssClass = CreaClasseStato(r.Stato),
                        PuoEssereValidata = r.Stato == StatoRichiesta.InAttesa
                    })
                    .ToList();
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public virtual IActionResult Approva(
    int richiestaId,
    int dipendenteId,
    string motivazioneEsito,
    string searchTerm = "")
        {
            return ValutaRichiesta(
                richiestaId,
                dipendenteId,
                motivazioneEsito,
                StatoRichiesta.Approvata,
                searchTerm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public virtual IActionResult Respingi(
            int richiestaId,
            int dipendenteId,
            string motivazioneEsito,
            string searchTerm = "")
        {
            return ValutaRichiesta(
                richiestaId,
                dipendenteId,
                motivazioneEsito,
                StatoRichiesta.Respinta,
                searchTerm);
        }

        /*
         * Applica l'esito scelto dal Super.
         *
         * La stessa logica viene utilizzata sia per
         * l'approvazione sia per il rifiuto, evitando
         * duplicazioni tra le due action POST.
         */
        private IActionResult ValutaRichiesta(
            int richiestaId,
            int dipendenteId,
            string motivazioneEsito,
            StatoRichiesta nuovoStato,
            string searchTerm)
        {
            /*
             * La motivazione del Super è obbligatoria.
             * Usiamo TempData perché dopo il controllo
             * eseguiamo un redirect secondo il pattern PRG.
             */
            if (string.IsNullOrWhiteSpace(
                    motivazioneEsito))
            {
                TempData["ErroreValutazione"] =
                    "Inserisci una motivazione prima di approvare o respingere la richiesta.";

                return RedirectToAction(
                    "Index",
                    new
                    {
                        searchTerm,
                        dipendenteId
                    });
            }

            var richiesta = _dbContext.Richieste
                .FirstOrDefault(r =>
                    r.Id == richiestaId &&
                    r.DipendenteId == dipendenteId);

            if (richiesta == null)
            {
                TempData["ErroreValutazione"] =
                    "La richiesta selezionata non è stata trovata.";

                return RedirectToAction(
                    "Index",
                    new
                    {
                        searchTerm,
                        dipendenteId
                    });
            }

            /*
             * Una richiesta già valutata non può essere
             * approvata o respinta una seconda volta.
             */
            if (richiesta.Stato !=
                StatoRichiesta.InAttesa)
            {
                TempData["ErroreValutazione"] =
                    "La richiesta è già stata valutata.";

                return RedirectToAction(
                    "Index",
                    new
                    {
                        searchTerm,
                        dipendenteId
                    });
            }

            richiesta.Stato =
                nuovoStato;

            richiesta.MotivazioneEsito =
                motivazioneEsito.Trim();

            richiesta.DataValutazione =
                DateTime.Now;

            richiesta.ValutataDa =
                HttpContext.Session.GetString("Nominativo")
                ?? "Super";

            _dbContext.SaveChanges();

            TempData["EsitoValutazione"] =
                nuovoStato == StatoRichiesta.Approvata
                    ? "Richiesta approvata correttamente."
                    : "Richiesta respinta correttamente.";

            return RedirectToAction(
                "Index",
                new
                {
                    searchTerm,
                    dipendenteId
                });
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

        private static string CreaClasseStato(StatoRichiesta stato)
        {
            return stato switch
            {
                StatoRichiesta.Approvata => "marc-status-approved",
                StatoRichiesta.Respinta => "marc-status-rejected",
                _ => "marc-status-pending"
            };
        }
    }
}