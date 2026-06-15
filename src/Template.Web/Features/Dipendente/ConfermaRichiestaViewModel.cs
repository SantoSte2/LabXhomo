namespace Template.Web.Features.Dipendente
{
    public class ConfermaRichiestaViewModel
    {
        public int RichiestaId { get; set; }

        public string Nominativo { get; set; } = string.Empty;

        public string Matricola { get; set; } = string.Empty;

        public string Iniziali { get; set; } = string.Empty;

        public string TipoRichiesta { get; set; } = string.Empty;

        public string DettaglioRichiesta { get; set; } = string.Empty;

        public string Periodo { get; set; } = string.Empty;

        public string Orario { get; set; } = string.Empty;

        public string Motivazione { get; set; } = string.Empty;

        public string Stato { get; set; } = string.Empty;
    }
}