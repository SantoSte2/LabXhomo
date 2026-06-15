using System.Collections.Generic;

namespace Template.Web.Features.Super
{
    public class SuperDashboardViewModel
    {
        public string NominativoSuper { get; set; } = string.Empty;

        public string MatricolaSuper { get; set; } = string.Empty;

        public string InizialiSuper { get; set; } = string.Empty;

        public string SearchTerm { get; set; } = string.Empty;

        public int? DipendenteSelezionatoId { get; set; }

        public List<DipendenteRicercaViewModel> Dipendenti { get; set; } = new();

        public DipendenteRicercaViewModel? DipendenteSelezionato { get; set; }

        public List<RichiestaSuperViewModel> Richieste { get; set; } = new();
    }

    public class DipendenteRicercaViewModel
    {
        public int Id { get; set; }

        public string Matricola { get; set; } = string.Empty;

        public string Nome { get; set; } = string.Empty;

        public string Cognome { get; set; } = string.Empty;

        public string Nominativo => $"{Nome} {Cognome}";

        public string VoceRicerca => $"{Nome} {Cognome} - {Matricola}";
    }

    public class RichiestaSuperViewModel
    {
        public int Id { get; set; }

        public string Tipo { get; set; } = string.Empty;

        public string Dettaglio { get; set; } = string.Empty;

        public string Data { get; set; } = string.Empty;

        public string Orario { get; set; } = string.Empty;

        public string Motivazione { get; set; } = string.Empty;

        public string Stato { get; set; } = string.Empty;


        public string StatoCssClass { get; set; } = string.Empty;

        public bool PuoEssereValidata { get; set; }
    }
}