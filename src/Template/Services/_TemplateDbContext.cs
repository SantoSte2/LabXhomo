using Microsoft.EntityFrameworkCore;
using Template.Infrastructure;
using Template.Models;
using Template.Services.Shared;

namespace Template.Services
{
    public class TemplateDbContext : DbContext
    {
        public TemplateDbContext()
        {
        }

        public TemplateDbContext(DbContextOptions<TemplateDbContext> options) : base(options)
        {
            DataGenerator.InitializeUsers(this);
        }

        public DbSet<User> Users { get; set; }

        public DbSet<Dipendente> Dipendenti { get; set; }

        public DbSet<Richiesta> Richieste { get; set; }

        /*
         * Contiene i mesi definitivamente validati dai Super.
         */
        public DbSet<ValidazioneMensile> ValidazioniMensili { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Dipendente>()
                .HasIndex(d => d.Matricola)
                .IsUnique();

            modelBuilder.Entity<Dipendente>()
                .HasIndex(d => d.Username)
                .IsUnique();

            modelBuilder.Entity<Dipendente>()
                .HasIndex(d => d.Email)
                .IsUnique();

            modelBuilder.Entity<Richiesta>()
                .HasOne(r => r.Dipendente)
                .WithMany(d => d.Richieste)
                .HasForeignKey(r => r.DipendenteId);

            /*
             * Un dipendente non può avere due validazioni
             * per lo stesso mese e lo stesso anno.
             *
             * L'indice univoco protegge anche da eventuali
             * doppi invii del form o richieste duplicate.
             */
            modelBuilder.Entity<ValidazioneMensile>()
                .HasIndex(v => new
                {
                    v.DipendenteId,
                    v.Anno,
                    v.Mese
                })
                .IsUnique();

            /*
             * Relazione tra la validazione e il dipendente
             * al quale appartiene il mese.
             *
             * WithMany() evita di dover aggiungere una collezione
             * specifica dentro la classe Dipendente.
             */
            modelBuilder.Entity<ValidazioneMensile>()
                .HasOne(v => v.Dipendente)
                .WithMany()
                .HasForeignKey(v => v.DipendenteId)
                .OnDelete(DeleteBehavior.Restrict);

            /*
             * Relazione con il Super che ha eseguito
             * la chiusura mensile.
             *
             * Anche questa relazione punta alla tabella Dipendenti,
             * ma utilizza una chiave esterna differente.
             */
            modelBuilder.Entity<ValidazioneMensile>()
                .HasOne(v => v.Super)
                .WithMany()
                .HasForeignKey(v => v.SuperId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}