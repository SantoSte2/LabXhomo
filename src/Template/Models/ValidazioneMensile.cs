#nullable enable

using System;
using System.ComponentModel.DataAnnotations;

namespace Template.Models;

/// <summary>
/// Rappresenta la chiusura definitiva di un mese
/// per uno specifico dipendente.
///
/// La presenza di questo record significa che tutte
/// le richieste del mese sono state controllate dal Super.
/// </summary>
public class ValidazioneMensile
{
    public int Id { get; set; }

    /*
     * Dipendente al quale appartiene il mese validato.
     */
    public int DipendenteId { get; set; }

    public Dipendente? Dipendente { get; set; }

    /*
     * Mese e anno che identificano il periodo chiuso.
     *
     * Esempio:
     * Mese = 6
     * Anno = 2026
     * significa giugno 2026.
     */
    [Range(1, 12)]
    public int Mese { get; set; }

    [Range(2000, 2100)]
    public int Anno { get; set; }

    /*
     * Data e ora in cui è stata eseguita
     * la validazione definitiva.
     */
    public DateTime DataValidazione { get; set; }
        = DateTime.Now;

    /*
     * Super che ha effettuato la validazione.
     *
     * Anche il Super è memorizzato nella tabella Dipendenti,
     * ma possiede il ruolo RuoloUtente.Super.
     */
    public int SuperId { get; set; }

    public Dipendente? Super { get; set; }
}