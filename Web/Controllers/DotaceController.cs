using System;
using System.Threading.Tasks;
using HlidacStatu.Entities;
using HlidacStatu.Repositories;
using HlidacStatu.Web.Filters;
using Microsoft.AspNetCore.Mvc;
using Devmasters.Enums;
using System.Collections.Generic;
using System.Linq;
using Devmasters;

namespace HlidacStatu.Web.Controllers
{
    public class DotaceController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        private class poskytovateleCacheModel
        {
            public string Ico { get; set; }
            public string FirmaNazev { get; set; }
            public long Count { get; set; }
            public decimal Sum { get; set; }
        }
        private static Devmasters.Cache.LocalMemory.AutoUpdatedCache<poskytovateleCacheModel[]> poskytovateleCache =
            new Devmasters.Cache.LocalMemory.AutoUpdatedCache<poskytovateleCacheModel[]>(
            TimeSpan.FromHours(1), (o) =>
            {
                var poskytovateleIcos = HlidacStatu.Repositories.DotaceRepo.ReportPoskytovatelePoLetechAsync(null, null).ConfigureAwait(false).GetAwaiter().GetResult();
                var poskytovatele = poskytovateleIcos
                     .Select(i => new poskytovateleCacheModel()
                     {
                         Ico = i.IcoPoskytovatele,
                         FirmaNazev = Firmy.GetJmeno(i.IcoPoskytovatele).RemoveAccents(),
                         Count = i.Count,
                         Sum = i.Sum
                     })
                     .ToArray();
                return poskytovatele;
            }
            );
        public async Task<ActionResult> GetPoskytovatele(string id)
        {
            string[] s = id.RemoveAccents().Split(' ','.',',');
            var all = poskytovateleCache.Get();
            var res = all.Where(m =>
                            {
                                foreach (var word in s)
                                {
                                    if (m.FirmaNazev.Contains(word, StringComparison.InvariantCultureIgnoreCase)
                                    || m.Ico.Contains(word, StringComparison.InvariantCultureIgnoreCase)
                                       )
                                        return true;

                                }
                                return false;
                            }
                        )
                .OrderByDescending(o => o.Sum).ThenBy(o => o.FirmaNazev)
                .Take(10)
                .Select(m => new { name = m.FirmaNazev, value = m.Ico })
                .ToArray();
            return Json(res);
        }

        public async Task<ActionResult> Hledat(Repositories.Searching.DotaceSearchResult model)
        {
            if (model == null || ModelState.IsValid == false)
            {
                return View(new Repositories.Searching.DotaceSearchResult());
            }

            // var aggs = new Nest.AggregationContainerDescriptor<Dotace>()
            //     .Sum("souhrn", s => s
            //         .Field(f => f.AssumedAmount)
            //
            //     );

            var aggs = new Nest.AggregationContainerDescriptor<Dotace>()
                .Filter("filtered_sum", f => f
                    .Filter(q => q
                        .Term(t => t
                            .Field(f => f.Hints.IsOriginal)
                            .Value(true)
                        )
                    )
                    .Aggregations(subAgg => subAgg
                        .Sum("souhrn", s => s
                            .Field(f => f.AssumedAmount)
                        )
                    )
                );


            var res = await DotaceRepo.Searching.SimpleSearchAsync(model, anyAggregation: aggs);

            AuditRepo.Add(
                Audit.Operations.UserSearch
                , User?.Identity?.Name
                , HlidacStatu.Util.RealIpAddress.GetIp(HttpContext)?.ToString()
                , "Dotace"
                , res.IsValid ? "valid" : "invalid"
                , res.Q, res.OrigQuery);


            return View(res);
        }

        public async Task<ActionResult> Analyza(string q)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return Redirect("/dotace");
            }
            return View((object)q);
        }

        public async Task<ActionResult> Detail(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return Redirect("/dotace");
            }
            var dotace = await DotaceRepo.GetAsync(id);
            if (dotace is null)
            {
                return NotFound();
            }

            return View(dotace);
        }
        public async Task<ActionResult> DetailZdroj(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return Redirect("/dotace");
            }
            var dotace = await DotaceRepo.GetAsync(id);
            if (dotace is null)
            {
                return NotFound();
            }

            return View(dotace);
        }
        [HlidacCache(22 * 60 * 60, "", false)]
        public async Task<ActionResult> PoLetech()
        {
            DotaceRepo.Statistics.TypeStatsPerYear[] data = await DotaceRepo.ReportPoLetechPerTypeAsync();
            return View(data);
        }

        [HlidacCache(22 * 60 * 60, "rok;ftyp;dtyp", false)]
        public async Task<ActionResult> TopPrijemci(int? rok = null, int? ftyp = null, int? dtyp = null)
        {
            Firma.TypSubjektuEnum? typSubj = null;
            if (Enum.TryParse<Firma.TypSubjektuEnum>(ftyp?.ToString(), out Firma.TypSubjektuEnum t))
                typSubj = t;

            Dotace.Hint.Type? typDotace = null;
            if (Enum.TryParse<Dotace.Hint.Type>(dtyp?.ToString(), out Dotace.Hint.Type td))
                typDotace = td;

            ViewData["rok"] = rok;
            ViewData["ftyp"] = ftyp;
            ViewData["dtyp"] = dtyp;


            var data = await DotaceRepo.ReportTopPrijemciAsync(rok, typSubj, typDotace);

            return View(data);
        }

        public async Task<ActionResult> VyberDotaci(
            [FromQuery] int makequery

            )
        {

            return View();
        }

        [HlidacCache(22 * 60 * 60, "typdotace;rok", false)]
        public async Task<ActionResult> TopPoskytovatele(int typDotace, int? rok = null)
        {
            var data = await DotaceRepo.ReportPoskytovatelePoLetechAsync((Dotace.Hint.Type)typDotace, rok);
            ViewData["rok"] = rok;
            ViewData["typDotace"] = typDotace;
            return View(data);
        }

        [HlidacCache(22 * 60 * 60, "rok;cat", false)]
        public async Task<ActionResult> TopKategorie(int? rok = null, int? cat = null)
        {
            ViewData["rok"] = rok;
            ViewData["cat"] = cat;
            if (cat == null)
            {
                System.Collections.Generic.Dictionary<Dotace.Hint.CalculatedCategories, Lib.Analytics.SimpleStat> data = await DotaceRepo.PoKategoriichAsync(rok, null);
                return View("TopKategoriePrehled", data);

            }
            else
            {
                System.Collections.Generic.List<(string Ico, Dotace.Hint.CalculatedCategories Category, long Count, decimal Sum)> data =
                    await DotaceRepo.ReportPrijemciPoKategoriichAsync(rok, cat);
                return View(data);
            }
        }

        [HlidacCache(22 * 60 * 60, "programname;programcode;rok", false)]
        public async Task<ActionResult> Program(string programName, string programCode)
        {
            var data = await DotaceRepo.ProgramStatisticAsync(programName, programCode);
            ViewData["programName"] = programName;
            ViewData["programCode"] = programCode;
            return View(data);
        }

        [HlidacCache(22 * 60 * 60, "rok", false)]
        public async Task<ActionResult> DotacniExperti(int? rok = null)
        {
            var data = await DotaceRepo.DotacniExperti(rok);
            ViewData["rok"] = rok;
            return View(data);
        }

        [HlidacCache(22 * 60 * 60, "typdotace;rok", false)]
        public async Task<ActionResult> TopDotacniProgramy(int typDotace, int? rok = null)
        {
            ViewData["rok"] = rok;
            ViewData["typDotace"] = typDotace;
            return View();
        }

        [HlidacCache(22 * 60 * 60, "rok", false)]
        public async Task<ActionResult> DotovaniSponzori(int? rok = null)
        {
            var data = await DotaceRepo.DotovaniSponzori(rok);
            ViewData["rok"] = rok;
            return View(data);
        }

        public ActionResult Reporty()
        {
            return View();
        }
    }
}