using HlidacStatu.Entities;
using HlidacStatu.Entities.VZ;
using HlidacStatu.Extensions;
using HlidacStatu.Repositories;
using HlidacStatu.WebGenerator.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using System.Diagnostics;
using Serilog;
using HlidacStatu.Datasets;
using HlidacStatu.WebGenerator.Code;

namespace HlidacStatu.WebGenerator.Controllers
{
    public partial class HomeController : Controller
    {
        private readonly Serilog.ILogger _logger = Log.ForContext<HomeController>();

        public HomeController()
        {
        }

        public IActionResult Index()
        {
            return RedirectPermanent("https://www.hlidacstatu.cz/");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public ActionResult ImageBannerCore(string id, string title, string subtitle, string body, string footer,
            string img, string ratio = "16x9", string color = "blue-dark")
        {
            id = id ?? "social";
            if (System.Diagnostics.Debugger.IsAttached)
            {
                title = $"{DateTime.Now:HH:mm:ss:f} {title}";
            }
            if (!string.IsNullOrEmpty(img))
            {
                if (img.ToLower().StartsWith("http%3a") || img.ToLower().StartsWith("https%3a")
                    || img.ToLower().StartsWith("http:") || img.ToLower().StartsWith("https:"))
                {
                    //neresim, nechavam orig url
                }
                else
                {
                    //pridam full url na hlidace
                    if (img.StartsWith("/"))
                        img = System.Net.WebUtility.UrlEncode("https://www.hlidacstatu.cz") +img;
                    else
                        img = System.Net.WebUtility.UrlEncode("https://www.hlidacstatu.cz/") + img;
                }
            }

            string viewName = "ImageBannerCore16x9_social";
            if (id.ToLower() == "quote")
            {
                if (ratio == "1x1")
                    viewName = "ImageBannerCore1x1_quote";
                else
                    viewName = "ImageBannerCore16x9_quote";
            }
            else
            {
                if (ratio == "1x1")
                    viewName = "ImageBannerCore1x1_social";
                else
                    viewName = "ImageBannerCore16x9_social";
            }

            return View(viewName,
                new ImageBannerCoreData()
                { title = title, subtitle = subtitle, body = body, footer = footer, img = img, color = color });
        }



        public async Task<ActionResult> SocialBanner(string id, string v, string t, string st, string b, string f,
    string img, string rat = "16x9", string res = "1200x628", string col = "")
        {
            string mainUrl = "https://gen.hlidacstatu.cz";
                
                //HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
            // #if (DEBUG)
            //             if (System.Diagnostics.Debugger.IsAttached)
            //                 mainUrl = "http://local.hlidacstatu.cz";
            //             //mainUrl = "https://www.hlidacstatu.cz";
            // #endif
            //twitter Recommended size: 1024 x 512 pixels
            //fb Recommended size: 1200 pixels by 630 pixels

            string url = null;

            byte[] data = null;
            if (id?.ToLower() == "subjekt")
            {
                Firma fi = Firmy.Get(v);
                if (fi.Valid)
                {
                    if (!(await fi.NotInterestingToShowAsync()))
                    {
                        var social = new ImageBannerCoreData()
                        {
                            title = fi.SocialInfoTitle(),
                            body = (await fi.SocialInfoBodyAsync()),
                            footer = fi.SocialInfoFooter(),
                            subtitle = fi.SocialInfoSubTitle(),
                            img = fi.SocialInfoImageUrl(),
                            color = "blue-dark"
                        };
                        url = mainUrl + GetSocialBannerUrl(social, rat == "1x1", true);
                    }
                }
            }
            else if (id?.ToLower() == "zakazka")
            {
                VerejnaZakazka vz = await VerejnaZakazkaRepo.LoadFromESAsync(v);
                if (vz != null)
                    try
                    {
                        var path = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(id));
                    }
                    catch (Exception e)
                    {
                        _logger.Information(e, "VisitImg base64 encoding error");
                    }

                if (!vz.NotInterestingToShow())
                {
                    var social = new ImageBannerCoreData()
                    {
                        title = vz.SocialInfoTitle(),
                        body = vz.SocialInfoBody(),
                        footer = vz.SocialInfoFooter(),
                        subtitle = vz.SocialInfoSubTitle(),
                        img = vz.SocialInfoImageUrl(),
                        color = "blue-dark"
                    };
                    url = mainUrl + GetSocialBannerUrl(social, rat == "1x1", true);
                }
            }
            else if (id?.ToLower() == "osoba")
            {
                Osoba o = Osoby.GetByNameId.Get(v);
                if (o != null)
                {
                    if (await o.IsInterestingToShowAsync())
                    {
                        var social = new ImageBannerCoreData()
                        {
                            title = o.SocialInfoTitle(),
                            body = await o.SocialInfoBodyAsync(),
                            footer = o.SocialInfoFooter(),
                            subtitle = o.SocialInfoSubTitle(),
                            img = o.SocialInfoImageUrl(),
                            color = "blue-dark"
                        };
                        url = mainUrl + GetSocialBannerUrl(social, rat == "1x1", true);
                    }
                }
            }
            else if (id?.ToLower() == "smlouva")
            {
                Smlouva s = await SmlouvaRepo.LoadAsync(v, includePlaintext: false);
                if (s != null)
                {
                    if (!s.NotInterestingToShow())
                    {
                        var social = new ImageBannerCoreData()
                        {
                            title = s.SocialInfoTitle(),
                            body = s.SocialInfoBody(),
                            footer = s.SocialInfoFooter(),
                            subtitle = s.SocialInfoSubTitle(),
                            img = s.SocialInfoImageUrl(),
                            color = "blue-dark"
                        };
                        url = mainUrl + GetSocialBannerUrl(social, rat == "1x1", true);
                    }
                }
            }
            else if (id?.ToLower() == "insolvence")
            {
                var s = (await InsolvenceRepo.LoadFromEsAsync(v, false, true))?.Rizeni;
                if (s != null)
                {
                    if (!s.NotInterestingToShow())
                    {
                        var social = new ImageBannerCoreData()
                        {
                            title = s.SocialInfoTitle(),
                            body = s.SocialInfoBody(),
                            footer = s.SocialInfoFooter(),
                            subtitle = s.SocialInfoSubTitle(),
                            img = s.SocialInfoImageUrl(),
                            color = "blue-dark"
                        };
                        url = mainUrl + GetSocialBannerUrl(social, rat == "1x1", true);
                    }
                }
            }
            else if (id?.ToLower() == "dataset")
            {
                var s = DataSet.GetCachedDataset(v);
                if (s != null)
                {
                    if (!s.NotInterestingToShow())
                    {
                        var social = new ImageBannerCoreData()
                        {
                            title = await s.SocialInfoTitleAsync(),
                            body = await s.SocialInfoBodyAsync(),
                            footer = s.SocialInfoFooter(),
                            subtitle = s.SocialInfoSubTitle(),
                            img = s.SocialInfoImageUrl(),
                            color = "blue-dark"
                        };
                        url = mainUrl + GetSocialBannerUrl(social, rat == "1x1", true);
                    }
                }
            }
            else if (id?.ToLower() == "quote")
            {
                url = mainUrl + "/imagebannercore/quote"
                              + "?title=" + System.Net.WebUtility.UrlEncode(t)
                              + "&subtitle=" + System.Net.WebUtility.UrlEncode(st)
                              + "&body=" + System.Net.WebUtility.UrlEncode(b)
                              + "&footer=" + System.Net.WebUtility.UrlEncode(f)
                              + "&img=" + System.Net.WebUtility.UrlEncode(img)
                              + "&color=" + col
                              + "&ratio=" + rat;
                v = Devmasters.Crypto.Hash.ComputeHashToHex(url);
            }
            else if (id?.ToLower() == "kindex")
            {
                data = await RemoteUrlFromWebCache.GetWebPageScreenshotAsync(
                    mainUrl + "/kindex/banner/" + v, rat,
                    "kindex-banner-" + v,
                    HttpContext.Request.Query["refresh"] == "1");
            }
            else if (id?.ToLower() == "page" && string.IsNullOrEmpty(v) == false)
            {
                var pageUrl = v;
                string socialTitle = "";
                string socialHtml = "";
                string socialFooter = "";
                string socialSubFooter = "";
                string socialFooterImg = "";
                using (Devmasters.Net.HttpClient.URLContent net = new(pageUrl))
                {
                    net.Timeout = 40000;
                    var cont = net.GetContent().Text;
                    socialTitle = System.Net.WebUtility.HtmlDecode(
                        Devmasters.RegexUtil
                            .GetRegexGroupValues(cont,
                                @"<meta \s*  property=\""og:hlidac_title\"" \s*  content=\""(?<v>.*)\"" \s* />", "v")
                            .OrderByDescending(o => o.Length).FirstOrDefault()
                    );
                    socialHtml = System.Net.WebUtility.HtmlDecode(
                        Devmasters.RegexUtil
                            .GetRegexGroupValues(cont,
                                @"<meta \s*  property=\""og:hlidac_html\"" \s*  content=\""(?<v>.*)\"" \s* />", "v")
                            .OrderByDescending(o => o.Length).FirstOrDefault()
                    );
                    socialFooter = System.Net.WebUtility.HtmlDecode(
                        Devmasters.RegexUtil.GetRegexGroupValues(cont,
                                @"<meta \s*  property=\""og:hlidac_footer\"" \s*  content=\""(?<v>.*)\"" \s* />", "v")
                            .OrderByDescending(o => o.Length).FirstOrDefault()
                    );
                    socialSubFooter = System.Net.WebUtility.HtmlDecode(
                        Devmasters.RegexUtil.GetRegexGroupValues(cont,
                                @"<meta \s*  property=\""og:hlidac_subfooter\"" \s*  content=\""(?<v>.*)\"" \s* />",
                                "v")
                            .OrderByDescending(o => o.Length).FirstOrDefault()
                    );
                    socialFooterImg = System.Net.WebUtility.HtmlDecode(
                        Devmasters.RegexUtil.GetRegexGroupValues(cont,
                                @"<meta \s*  property=\""og:hlidac_footerimg\"" \s*  content=\""(?<v>.*)\"" \s* />",
                                "v")
                            .OrderByDescending(o => o.Length).FirstOrDefault()
                    );
                }

                if (string.IsNullOrEmpty(socialHtml))
                    return File(RemoteUrlFromWebCache.NoDataPicture, "image/png");
                else
                    url = mainUrl + "/imagebannercore/quote"
                                  + "?title=" +
                                  System.Net.WebUtility.UrlEncode(System.Net.WebUtility.HtmlDecode(socialTitle))
                                  + "&subtitle=" +
                                  System.Net.WebUtility.UrlEncode(System.Net.WebUtility.HtmlDecode(socialSubFooter))
                                  + "&body=" +
                                  System.Net.WebUtility.UrlEncode(System.Net.WebUtility.HtmlDecode(socialHtml))
                                  + "&footer=" +
                                  System.Net.WebUtility.UrlEncode(System.Net.WebUtility.HtmlDecode(socialFooter))
                                  + "&img=" + System.Net.WebUtility.UrlEncode(
                                      System.Net.WebUtility.HtmlDecode(socialFooterImg))
                                  + "&color=" + col
                                  + "&ratio=" + rat;
            }

            try
            {
                if (data == null && !string.IsNullOrEmpty(url))
                {
                    data = await RemoteUrlFromWebCache.GetWebPageScreenshotAsync(url, rat,
                        (id?.ToLower() ?? "null") + "-" + rat + "-" + v, HttpContext.Request.Query["refresh"] == "1");
                }
            }
            catch (Exception e)
            {
                _logger.Error(e, "Manager Save");
            }

            if (data == null || data.Length == 0)
                return File(RemoteUrlFromWebCache.NoDataPicture, "image/png");
            else
                return File(data, "image/png");
        }

        private string GetSocialBannerUrl(ImageBannerCoreData si, bool ratio1x1 = false, bool localUrl = true)
        {
            string url = "";
            if (localUrl == false)
                url = "https://www.hlidacstatu.cz";

            url = url + "/imagebannercore/social"
                      + "?title=" + System.Net.WebUtility.UrlEncode(si.title)
                      + "&subtitle=" + System.Net.WebUtility.UrlEncode(si.subtitle)
                      + "&body=" + System.Net.WebUtility.UrlEncode(si.body)
                      + "&footer=" + System.Net.WebUtility.UrlEncode(si.footer)
                      + "&img=" + System.Net.WebUtility.UrlEncode(si.img)
                      + "&ratio=" + (ratio1x1 ? "1x1" : "16x9")
                      + "&color=" + si.color;


            return url;
        }

    }
}
