using HlidacStatu.RegistrVozidel.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace HlidacStatu.RegistrVozidel
{
    public static class Repo
    {
        // Pseudocode:
        // - Use db context with using var to ensure disposal.
        // - Compose query with AsNoTracking on both sets for projection without tracking.
        // - Join owners with vehicles by Pcv.
        // - Filter by provided ico.
        // - Select into a new VypisVozidel instance, assigning only the requested columns from p and vv.
        // - Execute with ToListAsync and ConfigureAwait(false).

        public static async Task<List<Models.Vozidlo>> GetForICOAsync(string ico)
        {
            using var client = new dbCtx();

            return await client.VlastnikProvozovatelVozidla
                .AsNoTracking()
                .Join(
                    client.VypisVozidel.AsNoTracking(),
                    p => p.Pcv,
                    vv => vv.Pcv,
                    (p, vv) => new { p, vv })
                .Where(x => x.p.Ico == ico)
                .Select(x => new Vozidlo
                {
                    // from p (VlastnikProvozovatelVozidla)
                    Ico = x.p.Ico,
                    Typ_subjekt = x.p.TypSubjektu,
                    Vztah_k_vozidlu = x.p.VztahKVozidlu,
                    Aktualni = x.p.Aktualni,

                    // from vv (VypisVozidel)
                    Kategorie_vozidla = x.vv.KategorieVozidla,
                    Tovarni_znacka = x.vv.TovarniZnacka,
                    typ = x.vv.Typ,
                    Rok_vyroby = x.vv.RokVyroby,
                    Datum_1_registrace = x.vv.Datum1Registrace,
                    Datum_1_registrace_v_CR = x.vv.Datum1RegistraceVCr,
                    Zdvihovy_objem = x.vv.ZdvihovyObjem,
                    Barva = x.vv.Barva,
                    Nejvyssi_rychlost = x.vv.NejvyssiRychlost,
                    PlneElektrickeVozidlo = x.vv.PlneElektrickeVozidlo,
                    HybridniVozidlo = x.vv.HybridniVozidlo,
                    Stupen_plneni_emisni_urovne = x.vv.StupenPlneniEmisniUrovne,
                    ProvozniHmotnost = x.vv.ProvozniHmotnost
                })
                .ToListAsync();
        }
    }
}
