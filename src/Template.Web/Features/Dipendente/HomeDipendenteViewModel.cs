using System;
using System.Collections.Generic;
using System.Linq;

namespace Template.Web.Features.Dipendente
{
    public class HomeDipendenteViewModel
    {
        public string Nominativo { get; set; } = string.Empty;

        public string Matricola { get; set; } = string.Empty;

        public string Iniziali { get; set; } = string.Empty;

        public string MeseCorrente { get; set; } = string.Empty;

        public int AnnoCorrente { get; set; }


        public int MeseSelezionato { get; set; }

        public int AnnoSelezionato { get; set; }

        public int MesePrecedente { get; set; }

        public int AnnoMesePrecedente { get; set; }

        public int MeseSuccessivo { get; set; }

        public int AnnoMeseSuccessivo { get; set; }

        public List<GiornoCalendarioViewModel> GiorniCalendario { get; set; } = new();


        public decimal SaldoPermessoStudioOre { get; set; }

        public decimal SaldoFerieRolGiorni { get; set; }

        public decimal SaldoFerieRolOre { get; set; }

        public bool SaldoFerieRolNegativo => SaldoFerieRolOre < 0;
        public decimal SaldoRecuperoOre { get; set; }

        public List<EventoGiornoViewModel> EventiDelGiorno { get; set; } = new();

        public List<RichiestaStoricoViewModel> UltimeRichieste { get; set; } = new();

        public string MeseTotalizzatori { get; set; } = string.Empty;

        public int AnnoTotalizzatori { get; set; }

        

    }

    public class EventoGiornoViewModel
    {
        public string Tipo { get; set; } = string.Empty;

        public string Dettaglio { get; set; } = string.Empty;

        public string Periodo { get; set; } = string.Empty;

        public string Orario { get; set; } = string.Empty;

        public string Stato { get; set; } = string.Empty;

        public string StatoCssClass { get; set; } = string.Empty;
    }

    public class RichiestaStoricoViewModel
    {
        public int Id { get; set; }

        public string Data { get; set; } = string.Empty;

        public string Tipo { get; set; } = string.Empty;

        public string Dettaglio { get; set; } = string.Empty;

        public string Orario { get; set; } = string.Empty;

        public string Stato { get; set; } = string.Empty;

        public string StatoCssClass { get; set; } = string.Empty;

        /*
         * Motivazione inserita dal Super durante
         * l'approvazione oppure il rifiuto.
         */
        public string MotivazioneEsito { get; set; }
            = string.Empty;

        /*
         * Nominativo del Super che ha valutato
         * la richiesta.
         */
        public string ValutataDa { get; set; }
            = string.Empty;

        /*
         * Data e ora della valutazione,
         * già formattate dal controller.
         */
        public string DataValutazione { get; set; }
            = string.Empty;

        /*
         * La View mostra il riquadro soltanto
         * quando la richiesta è stata effettivamente valutata.
         */
        public bool HaValutazioneSuper =>
            !string.IsNullOrWhiteSpace(
                MotivazioneEsito);

    }

    public class GiornoCalendarioViewModel
    {
        public int NumeroGiorno { get; set; }

        public DateTime Data { get; set; }

        public bool AppartieneAlMese { get; set; }

        public bool Oggi { get; set; }

        public List<EventoCalendarioViewModel> Eventi { get; set; } = new List<EventoCalendarioViewModel>();

        public bool HaEventi
        {
            get
            {
                return Eventi != null &&
                       Eventi.Count > 0;
            }
        }


        /*
         * Indica che il giorno è una festività nazionale.
         */
        public bool FestivoNazionale { get; set; }

        /*
         * Nome completo della festività:
         * per esempio "Festa della Repubblica".
         */
        public string NomeFestivita { get; set; }
            = string.Empty;

        /*
         * Classe CSS utilizzata per distinguere
         * festività civili e religiose.
         */
        public string ClasseFestivita { get; set; }
            = string.Empty;

    }
    public class EventoCalendarioViewModel
    {
        public string Testo { get; set; } = string.Empty;

        public string StatoCssClass { get; set; } = string.Empty;
    }
}