using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Template.Enums;
using Template.Models;
using Template.Services;

namespace Template.Data;

public static class SeedData
{
    public static void Initialize(TemplateDbContext context)
    {
        if (context.Dipendenti.Any())
        {
            return;
        }

        var super = new Dipendente
        {
            Matricola = "SUP001",
            Nome = "Admin",
            Cognome = "MarcTempo",
            Username = "admin",
            Email = "admin@marctempo.it",
            Password = "Admin123",
            Ruolo = RuoloUtente.Super
        };

        var stefano = new Dipendente
        {
            Matricola = "DIP001",
            Nome = "Stefano",
            Cognome = "Santoni",
            Username = "stefano",
            Email = "stefano.santoni@marctempo.it",
            Password = "User123",
            Ruolo = RuoloUtente.Dipendente
        };

        var mario = new Dipendente
        {
            Matricola = "DIP002",
            Nome = "Mario",
            Cognome = "Rossi",
            Username = "mario",
            Email = "mario.rossi@marctempo.it",
            Password = "User123",
            Ruolo = RuoloUtente.Dipendente
        };

        var marioOmonimo = new Dipendente
        {
            Matricola = "DIP003",
            Nome = "Mario",
            Cognome = "Rossi",
            Username = "mario2",
            Email = "mario.rossi2@marctempo.it",
            Password = "User123",
            Ruolo = RuoloUtente.Dipendente
        };

        context.Dipendenti.AddRange(super, stefano, mario, marioOmonimo);

        context.Richieste.Add(new Richiesta
        {
            Dipendente = stefano,
            TipoRichiesta = TipoRichiesta.Giustificativo,
            TipoGiustificativo = TipoGiustificativo.SmartWorking,
            DataInizio = DateTime.Today,
            DataFine = DateTime.Today,
            OreDichiarate = 8,
            Motivazione = "Smart working giornaliero",
            Stato = StatoRichiesta.InAttesa
        });

        context.SaveChanges();
    }
}