#nullable enable

using System;
using Template.Enums;

namespace Template.Web.Features.Dipendente
{
    /// <summary>
    /// Rappresenta una richiesta validata ma non ancora confermata.
    /// Viene conservata temporaneamente nella sessione dell'utente.
    /// </summary>
    public class RichiestaInConfermaSessionModel
    {
        public string TipoSezione { get; set; } = "Giustificativo";

        public TipoGiustificativo? TipoGiustificativo { get; set; }

        public TipoMancataTimbratura? TipoMancataTimbratura { get; set; }

        public DateTime DataInizio { get; set; }

        public DateTime? DataFine { get; set; }

        public TimeSpan? OraInizio { get; set; }

        public TimeSpan? OraFine { get; set; }

        public bool PostNotturno { get; set; }

        public string Motivazione { get; set; } = string.Empty;

        public decimal? OreDichiarate { get; set; }
    }
}