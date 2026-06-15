using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using Template.Enums;

namespace Template.Models;

public class Dipendente
{
    public int Id { get; set; }

    [Required]
    [MaxLength(20)]
    public string Matricola { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Nome { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Cognome { get; set; } = string.Empty;

    [Required]
    [MaxLength(80)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Password { get; set; } = string.Empty;

    public RuoloUtente Ruolo { get; set; }

    public ICollection<Richiesta> Richieste { get; set; } = new List<Richiesta>();

    public string Nominativo => $"{Nome} {Cognome}";

    public string Iniziali
    {
        get
        {
            var primaNome = string.IsNullOrWhiteSpace(Nome) ? "" : Nome[0].ToString();
            var primaCognome = string.IsNullOrWhiteSpace(Cognome) ? "" : Cognome[0].ToString();

            return $"{primaNome}{primaCognome}".ToUpper();
        }
    }
}
