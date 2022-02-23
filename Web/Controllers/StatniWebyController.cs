using HlidacStatu.Entities;
using HlidacStatu.Repositories;
using HlidacStatu.Web.Filters;

using InfluxDB.Client;

using Microsoft.AspNetCore.Mvc;

using System;
using System.Collections.Generic;
using System.Linq;

namespace HlidacStatu.Web.Controllers
{
    public class StatniWebyController : Controller
    {

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Dalsi(string id)
        {
            ViewBag.ID = id;
            if (Devmasters.TextUtil.IsNumeric(id))
            { //priorita

                int iid = Convert.ToInt32(id);
                if (iid < 1)
                    return RedirectToAction("Index", "StatniWeby");
                if (iid > 3)
                    return RedirectToAction("Index", "StatniWeby");

                ViewBag.SubTitle = "Další státní weby";
                return View(Repositories.UptimeServerRepo.ServersIn(iid.ToString()));
            }
            else if (id?.ToLower() == "ustredni")
            {
                var list = Repositories.UptimeServerRepo.ServersIn(id);
                if (list == null)
                    return RedirectToAction("Index", "StatniWeby");
                if (list.Count() == 0)
                    return RedirectToAction("Index", "StatniWeby");

                ViewBag.SubTitle = "Weby ústředních orgánů státní správy";
                return View(list);

            }
            else if (id?.ToLower() == "krajske" && false)
            {
                var list = Repositories.UptimeServerRepo.ServersIn(id);
                if (list == null)
                    return RedirectToAction("Index", "StatniWeby");
                if (list.Count() == 0)
                    return RedirectToAction("Index", "StatniWeby");

                ViewBag.SubTitle = "Weby a služby krajských úřadů";
                return View(list);

            }
            else
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

            switch (id)
            {
                case "index":
                    data = Repositories.UptimeServerRepo.AvailabilityForDayByGroup("0")
                        ?.OrderBy(o => o.Host.Name)
                        ?.Reverse()
                        ?.ToList();

                    break;
                case "1":
                case "2":
                case "3":
                    data = Repositories.UptimeServerRepo.AvailabilityForDayByGroup(id)
                        ?.OrderBy(o => o.Host.Name)
                        ?.Reverse()
                        ?.ToList();
                    break;
                case "ustredni":
                    data = Repositories.UptimeServerRepo.AvailabilityForDayByGroup("ustredni")
                        ?.OrderBy(o => o.Host.Name)
                        ?.Reverse()
                        ?.ToList();
                    break;
                case "krajske":
                    break;
                default:
                    break;
            }
            if (id.StartsWith("w"))
            {
                id = id.Replace("w", "");
                var host = Repositories.UptimeServerRepo.Load(id);
                if (host != null)
                {
                    if (host.ValidHash(hh))
                    {
                        data = new UptimeServer.HostAvailability[] { UptimeServerRepo.AvailabilityForWeekById(host.Id) };
                    }
                }
            }


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
                      .SelectMany(x => x)
                      .ToArray(),
                    cats = data.ToDictionary(k => k.Host.Id, d => new { host = d.Host, lastResponse = d.Data.Last() }),
                    categories = data.Select(m => m.Host.Id).ToArray(),
                    colsize = data.Select(d => d.ColSize(fromDate, toDate)).Max(),
                };
                content = Newtonsoft.Json.JsonConvert.SerializeObject(dataready);
            }
            return Content(content, "application/json");
        }


        [HlidacCache(10 * 60, "id;h;embed", false)]
        public ActionResult Info(int id, string h)
        {
            UptimeServer host = Repositories.UptimeServerRepo.AllServers()
                .FirstOrDefault(w => w.Id == id.ToString())
                ;
            if (host == null)
                return RedirectToAction("Index", "StatniWeby");

            if (host.ValidHash(h))
                return View(host);
            else
                return RedirectToAction("Index", "StatniWeby");
        }

        [HlidacCache(10 * 60, "id;h;embed", false)]
        public ActionResult InfoHttps(int id, string h)
        {
            UptimeServer host = Repositories.UptimeServerRepo.AllServers()
                .FirstOrDefault(w => w.Id == id.ToString())
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