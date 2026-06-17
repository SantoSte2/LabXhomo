using System;
using System.Collections.Generic;
using System.Linq;

namespace Template.Web.Features.Reparto
{
    public class CalendarioColleghiViewModel
    {
        public string NominativoUtente { get; set; } = string.Empty;

        public string MatricolaUtente { get; set; } = string.Empty;

        public string InizialiUtente { get; set; } = string.Empty;

        public string RuoloUtente { get; set; } = string.Empty;

        /*
         * Proprietà calcolate utilizzate dalla View.
         *
         * Evitiamo di confrontare direttamente le stringhe del ruolo
         * in diversi punti del file Razor.
         */
        public bool UtenteDipendente =>
            string.Equals(
                RuoloUtente,
                "Dipendente",
                StringComparison.OrdinalIgnoreCase);

        /*
         * Identifica l'utente con ruolo Super.
         */
        public bool UtenteSuper =>
            string.Equals(
                RuoloUtente,
                "Super",
                StringComparison.OrdinalIgnoreCase);

        public string NomeMese { get; set; } = string.Empty;

        public int Mese { get; set; }

        public int Anno { get; set; }

        /*
         * Estremi del periodo visualizzato nella vista mensile.
         *
         * Il valore predefinito sarà:
         * - Dal: primo giorno del mese;
         * - Al: ultimo giorno del mese.
         */
        public DateTime DataDal { get; set; }

        public DateTime DataAl { get; set; }

        /*
         * Titolo mostrato sopra la griglia.
         *
         * Quando viene visualizzato l'intero mese sarà, ad esempio:
         * "Giugno 2026".
         *
         * Per un intervallo personalizzato sarà:
         * "10/06/2026 - 24/06/2026".
         */
        public string TitoloPeriodo { get; set; } = string.Empty;

        /*
         * Eventuale messaggio mostrato quando l'intervallo
         * inserito dall'utente deve essere corretto.
         */
        public string MessaggioPeriodo { get; set; } = string.Empty;

        public int MesePrecedente { get; set; }

        public int AnnoMesePrecedente { get; set; }

        public int MeseSuccessivo { get; set; }

        public int AnnoMeseSuccessivo { get; set; }

        public string Vista { get; set; } = "Mensile";

        public bool VistaMensile => Vista == "Mensile";

        public bool VistaAnnuale => Vista == "Annuale";

        public List<int> GiorniAnno { get; set; } = new();

        public List<RigaMeseAnnualeViewModel> MesiAnno { get; set; } = new();

        public List<GiornoRepartoViewModel> Giorni { get; set; } = new();

        public List<RigaPersonaRepartoViewModel> Persone { get; set; } = new();
    }

    public class GiornoRepartoViewModel
    {
        public DateTime Data { get; set; }

        public int NumeroGiorno { get; set; }

        public string NomeGiornoBreve { get; set; } = string.Empty;

        public bool Oggi { get; set; }

        public bool Weekend { get; set; }

        public bool FestivoNazionale { get; set; }

        public string NomeFestivita { get; set; } = string.Empty;

        public string ClasseFestivita { get; set; } = string.Empty;

        /*
         * Numero complessivo degli utenti con ruolo Dipendente.
         * Gli utenti Super non vengono inclusi nel conteggio.
         */
        public int TotaleDipendenti { get; set; }

        /*
         * Dipendenti considerati presenti nella giornata.
         *
         * Lo Smart Working viene contato come presenza,
         * mentre ferie e permessi giornalieri approvati
         * vengono considerati assenza.
         */
        public int DipendentiPresenti { get; set; }

        /*
         * Numero di dipendenti che lavorano in Smart Working.
         * Sono già compresi nel valore DipendentiPresenti.
         */
        public int DipendentiSmartWorking { get; set; }

        /*
         * Proprietà calcolata utile per la View.
         */
        public int DipendentiAssenti =>
            Math.Max(
                0,
                TotaleDipendenti - DipendentiPresenti);
    }

    public class RigaPersonaRepartoViewModel
    {
        public int Id { get; set; }

        public string Nominativo { get; set; } = string.Empty;

        public string Matricola { get; set; } = string.Empty;

        public string Ruolo { get; set; } = string.Empty;

        /*
         * Indica che questa riga appartiene
         * all'utente attualmente autenticato.
         *
         * La View la utilizza per mostrare l'etichetta "Tu"
         * e rendere immediatamente riconoscibili
         * le richieste personali.
         */
        public bool UtenteCorrente { get; set; }

        public List<CellaFerieRepartoViewModel> Celle { get; set; } = new();
    }

    public class CellaFerieRepartoViewModel
    {
        public DateTime Data { get; set; }

        public bool Oggi { get; set; }

        public bool Weekend { get; set; }

        public List<EventoFerieRepartoViewModel> Eventi { get; set; } = new();

        public bool HaEventi => Eventi.Any();

        public bool FestivoNazionale { get; set; }

        public string NomeFestivita { get; set; } = string.Empty;

        public string ClasseFestivita { get; set; } = string.Empty;
    }

    public class EventoFerieRepartoViewModel
    {
        /*
         * Tipologia sintetica mostrata nella cella:
         * - Ferie;
         * - Ferie straord.;
         * - successivamente anche Smart o Permesso.
         */
        public string Testo { get; set; } = string.Empty;

        /*
         * Stato scritto esplicitamente.
         *
         * In questo modo l'utente non deve ricordare
         * il significato dei colori della legenda.
         */
        public string StatoTesto { get; set; } = string.Empty;

        /*
         * Il colore rimane come rinforzo grafico,
         * ma non è più l'unico elemento informativo.
         */
        public string StatoCssClass { get; set; } = string.Empty;
    }

    public class RigaMeseAnnualeViewModel
    {
        public int NumeroMese { get; set; }

        public string NomeMese { get; set; } = string.Empty;

        public bool MeseCorrente { get; set; }

        public List<CellaFerieAnnualeViewModel> Celle { get; set; } = new();
    }

    public class CellaFerieAnnualeViewModel
    {
        public int Giorno { get; set; }

        public DateTime? Data { get; set; }

        public bool Esiste { get; set; }

        public bool Oggi { get; set; }

        public bool Weekend { get; set; }

        public List<PersonaFerieAnnualeViewModel> Persone { get; set; } = new();

        public bool HaPersone => Persone.Any();

        public bool FestivoNazionale { get; set; }

        public string NomeFestivita { get; set; } = string.Empty;

        public string ClasseFestivita { get; set; } = string.Empty;
    }

    public class PersonaFerieAnnualeViewModel
    {
        public string Iniziali { get; set; } = string.Empty;

        public string Nominativo { get; set; } = string.Empty;

        public string Matricola { get; set; } = string.Empty;

        public string Stato { get; set; } = string.Empty;

        public string StatoCssClass { get; set; } = string.Empty;

        /*
         * Stato formattato in modo leggibile
         * direttamente nella cella annuale.
         *
         * Non utilizziamo soltanto il colore:
         * l'utente può leggere esplicitamente
         * "In attesa", "Approvata" oppure "Respinta".
         */
        public string StatoTesto { get; set; }
            = string.Empty;

        /*
         * Tipologia sintetica della richiesta mostrata
         * nella cella annuale:
         * - Ferie;
         * - Smart working;
         * - Permesso studio;
         * - Ferie straord.
         */
        public string TipoRichiestaTesto { get; set; }
            = string.Empty;
    }
}