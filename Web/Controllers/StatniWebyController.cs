using HlidacStatu.Lib.Data.External.Zabbix;
using HlidacStatu.Web.Filters;

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
                return View(ZabTools.WebyItems(iid.ToString()));
            }
            else if (id?.ToLower() == "ustredni")
            {
                var list = ZabTools.WebyItems(id);
                if (list == null)
                    return RedirectToAction("Index", "StatniWeby");
                if (list.Count() == 0)
                    return RedirectToAction("Index", "StatniWeby");

                ViewBag.SubTitle = "Weby ústředních orgánů státní správy";
                return View(list);

            }
            else if (id?.ToLower() == "krajske" && false)
            {
                var list = ZabTools.WebyItems(id);
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

            IEnumerable<ZabHostAvailability> data = null;

            switch (id)
            {
                case "index":
                    data = ZabTools.WebyData("0")
                        ?.OrderBy(o => o.Host.publicname)
                        ?.Reverse()
                        ?.ToList();

                    break;
                case "1":
                case "2":
                case "3":
                    data = ZabTools.WebyData(ZabTools.WebyItems(id))
                        ?.OrderBy(o => o.Host.publicname)
                        ?.Reverse()
                        ?.ToList();
                    break;
                case "ustredni":
                    data = ZabTools.WebyData(ZabTools.WebyItems("ustredni"))
                        ?.OrderBy(o => o.Host.publicname)
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
                ZabHost host = ZabTools.Weby().Where(w => w.hostid == id.ToString() & w.itemIdResponseTime != null).FirstOrDefault();
                if (host != null)
                {
                    if (host.ValidHash(hh))
                    {
                        data = new ZabHostAvailability[] { ZabTools.GetHostAvailabilityLong(host) };
                    }
                }
            }


            if (data != null)
            {
                var dataArr = data.ToArray();
                for (int i = 0; i < dataArr.Length; i++)
                {
                    dataArr[i].Host.publicname = Devmasters.TextUtil.ShortenText(dataArr[i].Host.publicname, 40);
                }
                var dataready = new
                {
                    data = dataArr.AsEnumerable()
                      .Select((x, l) => x.DataForChart(fromDate, toDate, l))
                      .SelectMany(x => x)
                      .ToArray(),
                    cats = data.ToDictionary(k => k.Host.hostid, d => new { host = d.Host, lastResponse = d.Data.Last() }),
                    categories = data.Select(m => m.Host.hostid).ToArray(),
                    colsize = data.Select(d => d.ColSize(fromDate, toDate)).Max(),
                };
                content = Newtonsoft.Json.JsonConvert.SerializeObject(dataready);
            }
            return Content(content, "application/json");
        }


        [HlidacCache(10 * 60, "id;h;embed", false)]
        public ActionResult Info(int id, string h)
        {
            ZabHost host = ZabTools.Weby().Where(w => w.hostid == id.ToString() & w.itemIdResponseTime != null).FirstOrDefault();
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