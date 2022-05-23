using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
                .FirstOrDefault(w => w.Id == id);
            if (host == null)
                return RedirectToAction("Index", "StatniWeby");

            if (host.ValidHash(h))
                return View(host);
            else
                return RedirectToAction("Index", "StatniWeby");
        }


        static byte[] EmptyPng = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+P+/HgAFhAJ/wlseKgAAAABJRU5ErkJggg==");
        [HlidacCache(2 * 60, "id", false)]
        public async Task<ActionResult> Banner(int id, string h)
        {
            UptimeServer host = Repositories.UptimeServerRepo.AllActiveServers()
                .FirstOrDefault(w => w.Id == id);
            if (host == null)
            {
                return File(EmptyPng, "image/png");
            }
            if (!host.ValidHash(h))
            {
                return File(EmptyPng, "image/png");
            }
            UptimeServer.HostAvailability? webDay = null;
            UptimeServer.HostAvailability? webWeek = null;

            if (System.Diagnostics.Debugger.IsAttached)
            {
                webDay = HlidacStatu.Repositories.UptimeServerRepo.GetAvailabilityNoCache(TimeSpan.FromHours(24), id)
                    .FirstOrDefault();
                webWeek = HlidacStatu.Repositories.UptimeServerRepo.GetAvailabilityNoCache(TimeSpan.FromHours(24 * 7), id)
                    .FirstOrDefault();
            }
            else
            {
                webDay = HlidacStatu.Repositories.UptimeServerRepo.AvailabilityForDayById(id);
                webWeek = HlidacStatu.Repositories.UptimeServerRepo.AvailabilityForWeekById(id);
            }

            if (webDay == null || webWeek == null)
            {
                return File(EmptyPng, "image/png");
            }
            var webssl = await UptimeSSLRepo.LoadLatestAsync(webDay.Host.HostDomain());

            HlidacStatu.KIndexGenerator.WebyLabel img = new HlidacStatu.KIndexGenerator.WebyLabel();
            var rgb = new Devmasters.Imaging.RGB(UptimeSSL.StatusOrigColor(webssl.SSLGrade()).Replace("#", ""));
            var data = img.GenerateImageByteArray(
                webDay.Host.Name,
                webDay.Host.Description,

                HlidacStatu.Util.RenderData.ToDate(webssl.CertExpiration(), "d.M.yyyy"),
                    webssl.OverallGrade, System.Drawing.Color.FromArgb(rgb.R, rgb.G, rgb.B),

                webDay.Statistics().PercentOfTime.OK,
                    $"{HlidacStatu.XLib.RenderTools.FormatAvailability(webDay.Statistics().DurationTotal.OK, HlidacStatu.XLib.RenderTools.DateTimePart.Minute)} ({webDay.Statistics().PercentOfTime.OK:P1})",
                webWeek.Statistics().PercentOfTime.OK,
                    $"{HlidacStatu.XLib.RenderTools.FormatAvailability(webWeek.Statistics().DurationTotal.OK, HlidacStatu.XLib.RenderTools.DateTimePart.Minute)} ({webWeek.Statistics().PercentOfTime.OK:P1})",


                webWeek.Statistics().PercentOfTime.Pomale,
                    $"{HlidacStatu.XLib.RenderTools.FormatAvailability(webDay.Statistics().DurationTotal.Pomale, HlidacStatu.XLib.RenderTools.DateTimePart.Minute)} ({webDay.Statistics().PercentOfTime.Pomale:P1})",
                webWeek.Statistics().PercentOfTime.Pomale,
                    $"{HlidacStatu.XLib.RenderTools.FormatAvailability(webWeek.Statistics().DurationTotal.Pomale, HlidacStatu.XLib.RenderTools.DateTimePart.Minute)} ({webWeek.Statistics().PercentOfTime.Pomale:P1})",

                webWeek.Statistics().PercentOfTime.Nedostupne,
                    $"{HlidacStatu.XLib.RenderTools.FormatAvailability(webDay.Statistics().DurationTotal.Nedostupne, HlidacStatu.XLib.RenderTools.DateTimePart.Minute)} ({webDay.Statistics().PercentOfTime.Nedostupne:P1})",
                webWeek.Statistics().PercentOfTime.Nedostupne,
                    $"{HlidacStatu.XLib.RenderTools.FormatAvailability(webWeek.Statistics().DurationTotal.Nedostupne, HlidacStatu.XLib.RenderTools.DateTimePart.Minute)} ({webWeek.Statistics().PercentOfTime.Nedostupne:P1})"

                );
            return File(data, "image/png");

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