using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Template.Enums;

namespace Template.Web.Features.Dipendente
{
    public class RichiestaViewModel
    {
        public string TipoSezione { get; set; } = "Giustificativo";

        public string Nominativo { get; set; } = string.Empty;

        public string Matricola { get; set; } = string.Empty;

        public string Iniziali { get; set; } = string.Empty;

        [Display(Name = "Giustificativo")]
        public TipoGiustificativo? TipoGiustificativo { get; set; }

        [Display(Name = "Tipologia")]
        public TipoMancataTimbratura? TipoMancataTimbratura { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Dal giorno")]
        public DateTime? DataInizio { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Al giorno")]
        public DateTime? DataFine { get; set; }

        [DataType(DataType.Time)]
        [Display(Name = "Ora in")]
        public TimeSpan? OraInizio { get; set; }

        [DataType(DataType.Time)]
        [Display(Name = "Ora out")]
        public TimeSpan? OraFine { get; set; }

        [Display(Name = "Post notturno")]
        public bool PostNotturno { get; set; }

        [MaxLength(500)]
        [Display(Name = "Motivazione")]
        public string Motivazione { get; set; } = string.Empty;

        public IEnumerable<SelectListItem> TipiGiustificativo { get; set; } = new List<SelectListItem>();

        public IEnumerable<SelectListItem> TipiMancataTimbratura { get; set; } = new List<SelectListItem>();
    }
}