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
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace HlidacStatu.Web.Controllers
{
    public class DotaceController : Controller
    {
        public const string EMPTY_FORM_VALUE = "###e_m-p###";
        public ActionResult Index()
        {
            return View();
        }

        private class poskytovateleCacheModel : HlidacStatu.Searching.SimpleAutocomplete.LocalItem
        {
            public poskytovateleCacheModel(string text, string id, long count, decimal sum) : base(text, id)
            {
                this.Count = count;
                this.Sum = sum;
            }

            [Newtonsoft.Json.JsonIgnore(), System.Text.Json.Serialization.JsonIgnore()]
            public long Count { get; set; }

            [Newtonsoft.Json.JsonIgnore(), System.Text.Json.Serialization.JsonIgnore()]
            public decimal Sum { get; set; }
        }
        private static Devmasters.Cache.LocalMemory.AutoUpdatedCache<poskytovateleCacheModel[]> poskytovateleCache =
            new Devmasters.Cache.LocalMemory.AutoUpdatedCache<poskytovateleCacheModel[]>(
            TimeSpan.FromHours(1), (o) =>
            {
                var poskytovateleIcos = HlidacStatu.Repositories.DotaceRepo.ReportPoskytovatelePoLetechAsync(null, null).ConfigureAwait(false).GetAwaiter().GetResult();
                var poskytovatele = poskytovateleIcos
                     .Select(i => new poskytovateleCacheModel(
                         Firmy.GetJmeno(i.IcoPoskytovatele).RemoveAccents(),
                         i.IcoPoskytovatele,
                         i.Count,
                         i.Sum)
                     )
                     .ToArray();
                return poskytovatele;
            }
            );
        public async Task<ActionResult> GetPoskytovatele(string id)
        {
            string[] s = id.RemoveAccents().Split(' ', '.', ',');
            var all = poskytovateleCache.Get();
            var res = all.Select(m => new { hits = m.GetHits(s), item = m })
                .Where(m => m.hits > 0)
                .OrderByDescending(o => o.hits)
                .Take(30)
                .OrderByDescending(o => o.hits).ThenBy(o => o.item.Text)
                .Take(10)
                .Select(m => m.item)
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

            var qS = "";
            Dictionary<string, string> qss = this.Request.Query
                .ToDictionary(q => q.Key, q => q.Value.ToString().Replace(EMPTY_FORM_VALUE,""));

            if (qss.ContainsKey("btnAnalyza") || qss.ContainsKey("btnQuery"))
            {
                var dotacePrefixes = DotaceRepo.Searching.Irules
                                        .SelectMany(m => m.Prefixes)
                                        .ToArray();
                foreach (var qkey in qss.Keys)
                {
                    if (dotacePrefixes.Any(m => string.Equals(m, qkey + ":", StringComparison.InvariantCultureIgnoreCase)))
                    {
                        if (!string.IsNullOrWhiteSpace(qss[qkey]))
                            qS = HlidacStatu.Searching.Query.ModifyQueryAND(qS, $"{qkey}:{qss[qkey]}");
                    }
                    else if (!string.IsNullOrWhiteSpace(qss[qkey]))
                    {
                        if (qkey=="text")
                            qS = HlidacStatu.Searching.Query.ModifyQueryAND(qS, $"{qss[qkey]}");
                        else if (qkey== "poskytovatel")
                            qS = HlidacStatu.Searching.Query.ModifyQueryAND(qS, $"icoposkytovatel:{qss[qkey]}");
                        else if (qkey == "hints.subsidyType")
                            qS = HlidacStatu.Searching.Query.ModifyQueryAND(qS, $"hints.subsidyType:{qss[qkey]}");
                    }

                }
                if (!string.IsNullOrWhiteSpace(qS))
                {
                    if (qss.ContainsKey("btnAnalyza"))
                        return Redirect("/dotace/analyza?q=" + System.Net.WebUtility.UrlEncode(qS));
                    else if (qss.ContainsKey("btnQuery"))
                        return Redirect("/dotace/hledat?q=" + System.Net.WebUtility.UrlEncode(qS));
                }
            }
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