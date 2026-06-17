#nullable enable


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using Template.Enums;

namespace Template.Models;

public class Richiesta
{
    public int Id { get; set; }

    public int DipendenteId { get; set; }

    public Dipendente? Dipendente { get; set; }

    public TipoRichiesta TipoRichiesta { get; set; }

    public TipoGiustificativo? TipoGiustificativo { get; set; }

    public TipoMancataTimbratura? TipoMancataTimbratura { get; set; }

    public DateTime DataInizio { get; set; }

    public DateTime? DataFine { get; set; }

    public TimeSpan? OraInizio { get; set; }

    public TimeSpan? OraFine { get; set; }

    public decimal? OreDichiarate { get; set; }

    public bool PostNotturno { get; set; }

    [MaxLength(500)]
    public string? Motivazione { get; set; }

    /*
     * Motivazione inserita dal Super durante
     * l'approvazione oppure il rifiuto.
     *
     * È distinta da Motivazione, che contiene invece
     * le eventuali note scritte dal dipendente.
     */
    [MaxLength(500)]
    public string? MotivazioneEsito { get; set; }

    /*
     * Data e ora in cui la richiesta
     * è stata approvata oppure respinta.
     */
    public DateTime? DataValutazione { get; set; }

    /*
     * Nominativo del Super che ha effettuato
     * la valutazione della richiesta.
     */
    [MaxLength(150)]
    public string? ValutataDa { get; set; }

    public StatoRichiesta Stato { get; set; } = StatoRichiesta.InAttesa;

    public DateTime DataCreazione { get; set; } = DateTime.Now;
}
