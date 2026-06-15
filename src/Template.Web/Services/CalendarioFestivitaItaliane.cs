#nullable enable

using System;
using System.Collections.Generic;

using System.Linq;
namespace Template.Web.Services;

public enum CategoriaFestivita
{
    Civile,
    Religiosa
}

public sealed class FestivitaItaliana
{
    public DateTime Data { get; init; }

    public string Nome { get; init; } = string.Empty;

    public CategoriaFestivita Categoria { get; init; }
}

public static class CalendarioFestivitaItaliane
{
    public static IReadOnlyDictionary<DateTime, FestivitaItaliana>
        CreaPerAnno(int anno)
    {
        var pasqua = CalcolaPasqua(anno);

        var festivita = new List<FestivitaItaliana>
        {
            Crea(anno, 1, 1, "Capodanno", CategoriaFestivita.Civile),

            Crea(anno, 1, 6, "Epifania", CategoriaFestivita.Religiosa),

            new FestivitaItaliana
            {
                Data = pasqua,
                Nome = "Pasqua",
                Categoria = CategoriaFestivita.Religiosa
            },

            new FestivitaItaliana
            {
                Data = pasqua.AddDays(1),
                Nome = "Lunedì dell'Angelo",
                Categoria = CategoriaFestivita.Religiosa
            },

            Crea(
                anno,
                4,
                25,
                "Festa della Liberazione",
                CategoriaFestivita.Civile),

            Crea(
                anno,
                5,
                1,
                "Festa del Lavoro",
                CategoriaFestivita.Civile),

            Crea(
                anno,
                6,
                2,
                "Festa della Repubblica",
                CategoriaFestivita.Civile),

            Crea(
                anno,
                8,
                15,
                "Assunzione di Maria",
                CategoriaFestivita.Religiosa),

            Crea(
                anno,
                10,
                4,
                "San Francesco d'Assisi",
                CategoriaFestivita.Religiosa),

            Crea(
                anno,
                11,
                1,
                "Ognissanti",
                CategoriaFestivita.Religiosa),

            Crea(
                anno,
                12,
                8,
                "Immacolata Concezione",
                CategoriaFestivita.Religiosa),

            Crea(
                anno,
                12,
                25,
                "Natale",
                CategoriaFestivita.Religiosa),

            Crea(
                anno,
                12,
                26,
                "Santo Stefano",
                CategoriaFestivita.Religiosa)
        };

        return festivita.ToDictionary(
            elemento => elemento.Data.Date);
    }

    /*
     * Verifica se una singola data è un giorno lavorativo.
     *
     * Un giorno viene considerato lavorativo soltanto quando:
     * - non è sabato;
     * - non è domenica;
     * - non coincide con una festività nazionale.
     *
     * Questo metodo viene utilizzato quando dobbiamo controllare
     * una sola data, per esempio nel calendario colleghi.
     */
    public static bool EGiornoLavorativo(DateTime data)
    {
        /*
         * Recuperiamo le festività dell'anno della data controllata.
         */
        var festivitaAnno = CreaPerAnno(data.Year);

        return EGiornoLavorativo(
            data,
            festivitaAnno);
    }

    /*
     * Conta i giorni lavorativi compresi tra due date,
     * includendo sia la data iniziale sia quella finale.
     *
     * Esempio:
     * dal 1° giugno al 3 giugno 2026:
     * - 1 giugno: lavorativo;
     * - 2 giugno: Festa della Repubblica;
     * - 3 giugno: lavorativo.
     *
     * Risultato: 2 giorni lavorativi.
     */
    public static int ContaGiorniLavorativi(
        DateTime dataInizio,
        DateTime dataFine)
    {
        var inizio = dataInizio.Date;
        var fine = dataFine.Date;

        /*
         * Protezione contro intervalli non validi.
         * Normalmente questa situazione viene già bloccata
         * dalla validazione del form.
         */
        if (fine < inizio)
        {
            return 0;
        }

        var totaleGiorniLavorativi = 0;

        /*
         * Conserviamo le festività già calcolate per ogni anno.
         *
         * Questo è utile quando una richiesta attraversa
         * il passaggio tra dicembre e gennaio:
         * ogni calendario annuale viene generato una sola volta.
         */
        var festivitaPerAnno =
            new Dictionary<
                int,
                IReadOnlyDictionary<DateTime, FestivitaItaliana>>();

        for (var data = inizio;
             data <= fine;
             data = data.AddDays(1))
        {
            /*
             * Recuperiamo dal dizionario le festività dell'anno.
             * Se non sono ancora presenti, le calcoliamo e le salviamo.
             */
            if (!festivitaPerAnno.TryGetValue(
                    data.Year,
                    out var festivitaAnno))
            {
                festivitaAnno = CreaPerAnno(data.Year);

                festivitaPerAnno[data.Year] =
                    festivitaAnno;
            }

            /*
             * Incrementiamo il conteggio soltanto quando la data
             * rispetta tutte le condizioni del giorno lavorativo.
             */
            if (EGiornoLavorativo(
                    data,
                    festivitaAnno))
            {
                totaleGiorniLavorativi++;
            }
        }

        return totaleGiorniLavorativi;
    }

    /*
     * Versione interna del controllo.
     *
     * Riceve il dizionario delle festività già pronto,
     * evitando di ricostruirlo durante ogni iterazione.
     */
    private static bool EGiornoLavorativo(
        DateTime data,
        IReadOnlyDictionary<DateTime, FestivitaItaliana>
            festivitaAnno)
    {
        /*
         * Il fine settimana non è lavorativo.
         */
        if (data.DayOfWeek == DayOfWeek.Saturday ||
            data.DayOfWeek == DayOfWeek.Sunday)
        {
            return false;
        }

        /*
         * Se la data è presente nel calendario delle festività,
         * non deve essere considerata lavorativa.
         */
        return !festivitaAnno.ContainsKey(
            data.Date);
    }

    private static FestivitaItaliana Crea(
        int anno,
        int mese,
        int giorno,
        string nome,
        CategoriaFestivita categoria)
    {
        return new FestivitaItaliana
        {
            Data = new DateTime(anno, mese, giorno),
            Nome = nome,
            Categoria = categoria
        };
    }

    /*
     * Algoritmo gregoriano di Meeus/Jones/Butcher.
     * Permette di calcolare Pasqua per qualsiasi anno.
     */
    private static DateTime CalcolaPasqua(int anno)
    {
        var a = anno % 19;
        var b = anno / 100;
        var c = anno % 100;
        var d = b / 4;
        var e = b % 4;
        var f = (b + 8) / 25;
        var g = (b - f + 1) / 3;
        var h = (19 * a + b - d - g + 15) % 30;
        var i = c / 4;
        var k = c % 4;
        var l = (32 + 2 * e + 2 * i - h - k) % 7;
        var m = (a + 11 * h + 22 * l) / 451;

        var mese = (h + l - 7 * m + 114) / 31;
        var giorno = ((h + l - 7 * m + 114) % 31) + 1;

        return new DateTime(
            anno,
            mese,
            giorno);
    }
}