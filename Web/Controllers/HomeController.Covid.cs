using HlidacStatu.Datasets;
using HlidacStatu.Web.Models;

using Microsoft.AspNetCore.Mvc;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace HlidacStatu.Web.Controllers
{
    public partial class HomeController : Controller
    {
        public ActionResult KapacitaNemocnicData(string id)
        {
            var client = NemocniceData.Client();

            NemocniceOnlyData[] n = client.Search<NemocniceOnlyData>(s => s
                    .Size(200)
                    .Sort(o => o.Descending(f => f.datum))
                    .Query(q => q.MatchAll())
                )
                .Hits
                .Select(m => m.Source)
                .ToArray();

            if (id == "last")
                Content(Newtonsoft.Json.JsonConvert.SerializeObject(n.First()), "text/json");
            return Content(Newtonsoft.Json.JsonConvert.SerializeObject(n), "text/json");
        }

        public async Task<ActionResult> KapacitaNemocnic()
        {
            var ds = DataSet.CachedDatasets.Get("kapacity-nemocnic");


            NemocniceData?[] nAll = (await ds.SearchDataRawAsync("*", 1, 1000)).Result
                .Select(s => Newtonsoft.Json.JsonConvert.DeserializeObject<NemocniceData>(s.Item2))
                .OrderByDescending(m => m.lastUpdated)
                .Take(120)
                .Reverse()
                .ToArray();

            return View(nAll);
        }

        public ActionResult KapacitaKazdeNemocnice(string id)
        {
            return View((object)id);
        }

    }
}