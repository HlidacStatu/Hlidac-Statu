using HlidacStatu.RegistrVozidel.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace HlidacStatu.RegistrVozidel
{
    public static partial class Repo
    {
        private static readonly ILogger _logger = Serilog.Log.ForContext(typeof(Repo));

        public static async Task<List<Models.VozidloLight>> GetForICOAsync(string ico,
            Enums.Vztah_k_vozidluEnum? vztah = null
            )
        {
            using var client = new dbCtx();

            var q1 = client.VlastnikProvozovatelVozidla
                .AsNoTracking()
                .Join(
                    client.VypisVozidel.AsNoTracking(),
                    p => p.Pcv,
                    vv => vv.Pcv,
                    (p, vv) => new { p, vv })
                .Where(x => x.p.Ico == ico);
            if (vztah != null)
            {
                q1 = q1.Where(x => x.p.VztahKVozidlu == (decimal?)vztah);
            }
            var q2 = q1.Join(
                client.TechnickeProhlidky.AsNoTracking()
                    .GroupBy(tp => tp.Pcv)
                    .Select(g => new
                    {
                        Pcv = g.Key,
                        PosledniStk = g.Max(tp => tp.PlatnostOd),
                        PlatnostStkMax = g.Max(tp => tp.PlatnostDo)
                    }),
                x => x.p.Pcv,
                stk => stk.Pcv,
                (x, stk) => new { x.p, x.vv, stk.PosledniStk, stk.PlatnostStkMax }
                );

            var q3 =  q2
                .Select(x => new VozidloLight
                {
                    // from p (VlastnikProvozovatelVozidla)
                    Pcv = x.p.Pcv,
                    Ico = x.p.Ico,
                    Typ_subjekt = x.p.TypSubjektu,
                    Vztah_k_vozidlu = x.p.VztahKVozidlu,
                    Aktualni = x.p.Aktualni,
                    DatumOd = x.p.DatumOd,
                    DatumDo = x.p.DatumDo,

                    // from vv (VypisVozidel)
                    Palivo = x.vv.Palivo,
                    Kategorie_vozidla = x.vv.KategorieVozidla,
                    Tovarni_znacka = x.vv.TovarniZnacka,
                    Model = x.vv.ObchodniOznaceni,
                    Rok_vyroby = x.vv.RokVyroby,
                    Datum_1_registrace = x.vv.Datum1Registrace,
                    Datum_1_registrace_v_CR = x.vv.Datum1RegistraceVCr,
                    Zdvihovy_objem = x.vv.ZdvihovyObjem,
                    Barva = x.vv.Barva,
                    Nejvyssi_rychlost = x.vv.NejvyssiRychlost,
                    PlneElektrickeVozidlo = x.vv.PlneElektrickeVozidlo,
                    HybridniVozidlo = x.vv.HybridniVozidlo,
                    Stupen_plneni_emisni_urovne = x.vv.StupenPlneniEmisniUrovne,
                    ProvozniHmotnost = x.vv.ProvozniHmotnost,

                    // from stk (TechnickeProhlidky)
                    PosledniStk = x.PosledniStk,
                    PlastnostStk = x.PlatnostStkMax
                });
            var final = await q3
                .ToListAsync();
            return final;
        }
    }
}
