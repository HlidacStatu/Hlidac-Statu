using System;
using System.Collections.Generic;
using System.Linq;

using HlidacStatu.Entities;
using HlidacStatu.Repositories;
using HlidacStatu.Web.Filters;

using Microsoft.AspNetCore.Mvc;

namespace HlidacStatu.Web.Controllers
{
    public class StatniWebyController : Controller
    {

        [HlidacCache(1 * 60, "id;h;embed", false)]

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Dalsi(string id)
        {
            ViewBag.ID = id;
            var servers = Repositories.UptimeServerRepo.ServersIn(id.ToString());
            if (servers != null || servers?.Count() > 0)
            {
                ViewBag.SubTitle = HlidacStatu.Web.Framework.WebyChartUtil.TableGroupsTitle(id);
                return View((list: servers, id: id));
            }

            return RedirectToAction("Index", "StatniWeby");


        }

        public ActionResult JakMerime()
        {
            return View();
        }
        public ActionResult OpenData()
        {
            return View();
        }


        [HlidacCache(60, "id;hh;f;t;h", false)]
        public ActionResult ChartData(string id, string hh, long? f, long? t, int? h = 24)
        {
            id = id?.ToLower() ?? "";
            string content = "{}";
            DateTime fromDate = DateTime.Now.AddHours(-1 * h.Value);
            DateTime toDate = DateTime.Now;
            if (f.HasValue)
                fromDate = new DateTime(f.Value);
            if (t.HasValue)
                toDate = new DateTime(t.Value);

            IEnumerable<Entities.UptimeServer.HostAvailability> data = null;
            if (id.StartsWith("w"))
            {
                id = id.Replace("w", "");
                var host = Repositories.UptimeServerRepo.Load(Convert.ToInt32(id));
                if (host != null)
                {
                    if (host.ValidHash(hh))
                    {
                        data = new UptimeServer.HostAvailability[] { UptimeServerRepo.AvailabilityForWeekById(host.Id) };
                    }
                }
            }
            else
                data = Repositories.UptimeServerRepo.AvailabilityForDayByGroup(id);

            if (data?.Count() > 0)
            {
                var dataArr = data.ToArray();
                for (int i = 0; i < dataArr.Length; i++)
                {
                    dataArr[i].Host.Name = Devmasters.TextUtil.ShortenText(dataArr[i].Host.Name, 40);
                }
                var dataready = new
                {
                    data = dataArr.AsEnumerable()
                      .Select((x, l) => x.DataForChart(fromDate, toDate, l))
                      //.Reverse()
                      .SelectMany(x => x)
                      .ToArray(),
                    cats = data
                        //.Reverse()
                        .ToDictionary(k => k.Host.Id, d => new { host = d.Host, lastResponse = d.Data.Last() }),
                    categories = data.Select(m => m.Host.Id)
                            //.Reverse()
                            .ToArray(),
                    colsize = data.Select(d => d.ColSize(fromDate, toDate)).Max(),
                };
                content = Newtonsoft.Json.JsonConvert.SerializeObject(dataready);
            }
            return Content(content, "application/json");
        }


        [HlidacCache(2 * 60, "id;h;embed", false)]
        public ActionResult Info(int id, string h)
        {
            UptimeServer host = Repositories.UptimeServerRepo.AllActiveServers()
                .FirstOrDefault(w => w.Id == id)
                ;
            if (host == null)
                return RedirectToAction("Index", "StatniWeby");

            if (host.ValidHash(h))
                return View(host);
            else
                return RedirectToAction("Index", "StatniWeby");
        }

        [HlidacCache(2 * 60, "id;h;embed", false)]
        public ActionResult InfoHttps(int id, string h)
        {
            UptimeServer host = Repositories.UptimeServerRepo.AllActiveServers()
                .FirstOrDefault(w => w.Id == id)
                ;
            if (host == null)
                return RedirectToAction("Index", "StatniWeby");

            if (host.ValidHash(h))
                return View(host);
            else
                return RedirectToAction("Index", "StatniWeby");
        }
        //[GZipOrDeflate()]
        public ActionResult Data(int? id, string h)
        {
            return Json(new { error = "API presunuto. Viz hlidacStatu.cz/api. Omlouvame se." });
        }

        public ActionResult Https()
        {
            return View();
        }






    }
}