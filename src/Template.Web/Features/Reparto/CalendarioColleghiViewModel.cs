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

        public string NomeMese { get; set; } = string.Empty;

        public int Mese { get; set; }

        public int Anno { get; set; }

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
    }

    public class RigaPersonaRepartoViewModel
    {
        public int Id { get; set; }

        public string Nominativo { get; set; } = string.Empty;

        public string Matricola { get; set; } = string.Empty;

        public string Ruolo { get; set; } = string.Empty;

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
        public string Testo { get; set; } = string.Empty;

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
    }
}