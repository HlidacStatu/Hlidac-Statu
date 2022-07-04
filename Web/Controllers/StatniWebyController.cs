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

        [HlidacCache(1 * 60, "id;embed", false)]

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




        [HlidacCache(2 * 60, "id;embed", false)]
        public ActionResult Info(int id, string h)
        {
            UptimeServer host = Repositories.UptimeServerRepo.AllActiveServers()
                .FirstOrDefault(w => w.Id == id);
            if (host == null)
                return RedirectToAction("Index", "StatniWeby");

            return View(host);
        }


        static byte[] EmptyPng = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+P+/HgAFhAJ/wlseKgAAAABJRU5ErkJggg==");
        [HlidacCache(2 * 60, "id", false)]
        public async Task<ActionResult> Banner(int id)
        {
            UptimeServer host = Repositories.UptimeServerRepo.AllActiveServers()
                .FirstOrDefault(w => w.Id == id);
            if (host == null)
            {
                return File(EmptyPng, "image/png");
            }
            UptimeServer.HostAvailability? webDay = null;
            UptimeServer.HostAvailability? webWeek = null;

            //if (System.Diagnostics.Debugger.IsAttached)
            //{
            //    webDay = HlidacStatu.Repositories.UptimeServerRepo.GetAvailabilityNoCache(TimeSpan.FromHours(24), id)
            //        .FirstOrDefault();
            //    webWeek = HlidacStatu.Repositories.UptimeServerRepo.GetAvailabilityNoCache(TimeSpan.FromHours(24 * 7), id)
            //        .FirstOrDefault();
            //}
            //else
            if (true)
            {
                try
                {
                    webDay = await Devmasters.Net.HttpClient.Simple.GetAsync<UptimeServer.HostAvailability?>(
                        Framework.Constants.ApiURL + $"api/v2/weby/availabilityForDayById?id={id}",
                        headers: new Dictionary<string, string> { { "Authorization", Framework.Constants.ApiToken } }
                    );
                    webWeek = await Devmasters.Net.HttpClient.Simple.GetAsync<UptimeServer.HostAvailability?>(
                        Framework.Constants.ApiURL + $"api/v2/weby/availabilityForWeekById?id={id}",
                        headers: new Dictionary<string, string> { { "Authorization", Framework.Constants.ApiToken } }
                    ); 

                }
                catch (Exception e)
                {
                }

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

                webssl.OverallGrade, System.Drawing.Color.FromArgb(rgb.R, rgb.G, rgb.B),
                    webssl.CertExpirationToString(false) + " " + UptimeSSL.StatusDescription(webssl.SSLGrade(), true),

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


        [HlidacCache(2 * 60, "id;embed", false)]
        public ActionResult InfoHttps(int id)
        {
            UptimeServer host = Repositories.UptimeServerRepo.AllActiveServers()
                .FirstOrDefault(w => w.Id == id)
                ;
            if (host == null)
                return RedirectToAction("Index", "StatniWeby");

            return View(host);
        }
        //[GZipOrDeflate()]
        public ActionResult Data(int? id)
        {
            return Json(new { error = "API presunuto. Viz hlidacStatu.cz/api. Omlouvame se." });
        }
        
        public IEnumerable<StatniWebyAutocomplete> Autocomplete(string query)
        {
            var results = UptimeServerRepo.AutocompleteSearch(query);
            return results;
        }

        public ActionResult Https()
        {
            return View();
        }

        public ActionResult ipv6()
        {
            return View();
        }





    }
}