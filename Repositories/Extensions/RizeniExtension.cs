using HlidacStatu.Util;

using System;
using System.Collections.Generic;
using System.Linq;
using Serilog;
using Rizeni = HlidacStatu.Entities.Insolvence.Rizeni;
using HlidacStatu.Entities.Facts;

namespace HlidacStatu.Extensions
{
    public static class RizeniExtension
    {
        private static readonly ILogger _logger = Log.ForContext(typeof(RizeniExtension));
        public static bool OdstranenoZInsolvencnihoRejstriku(this Rizeni rizeni)
        {
            return OdstranenoZInsolvencnihoRejstriku(rizeni.UrlInIR());
        }

        public static bool OdstranenoZInsolvencnihoRejstriku(string url)
        {
            try
            {
                string html;
                using (Devmasters.Net.HttpClient.URLContent net = new Devmasters.Net.HttpClient.URLContent(url))
                {
                    html = net.GetContent().Text;
                }

                Devmasters.XPath doc = new Devmasters.XPath(html);
                var spocet = doc
                    .GetNodeText(
                        "//table[@class='vysledekLustrace']//tr//td[contains(text(),'POČET')]/following-sibling::*")
                    ?.Trim();
                var pocet = ParseTools.ToInt(spocet);
                if (pocet.HasValue && pocet.Value == 0)
                {
                    return true;
                }
            }
            catch (Exception e)
            {
                _logger.Error(e, "");
            }

            return false;
        }

        public static bool NotInterestingToShow(this Rizeni rizeni)
        {
            return false;
        } //TODO

        public static string SocialInfoTitle(this Rizeni rizeni)
        {
            return rizeni.BookmarkName();
        }

        public static string SocialInfoSubTitle(this Rizeni rizeni)
        {
            return "Soud: " + rizeni.SoudFullName();
        }

        public static string SocialInfoBody(this Rizeni rizeni)
        {
            return "<ul>" +
                   rizeni.InfoFacts().RenderFacts( 4, true, true, "", "<li>{0}</li>", true)
                   + "</ul>";
        }

        public static string SocialInfoFooter(this Rizeni rizeni)
        {
            return "Údaje k " + RenderData.ToDate(rizeni.PosledniZmena);
        }

        public static string SocialInfoImageUrl(this Rizeni rizeni)
        {
            return string.Empty;
        }

        public static InfoFact[] InfoFacts(this Rizeni rizeni)
        {
            List<InfoFact> data = new List<InfoFact>();
            string sumTxt =
                $"Zahájena {RenderData.ToDate(rizeni.DatumZalozeni)}. {rizeni.StavRizeniDetail()}. Řeší ji {rizeni.SoudFullName()}.";

            data.Add(new InfoFact()
            {
                Level = InfoFact.ImportanceLevel.Summary,
                Text = sumTxt
            });

            if (rizeni.Dluznici.Count > 0)
            {
                sumTxt = Devmasters.Lang.CS.Plural.GetWithZero(rizeni.Dluznici.Count,
                    "",
                    "Dlužníkem je " + rizeni.Dluznici.First().FullNameWithYear(),
                    "Dlužníky jsou " + rizeni.Dluznici.Select(m => m.FullNameWithYear()).Aggregate((f, s) => f + ", " + s),
                    "Dlužníky jsou" + rizeni.Dluznici.Take(3).Select(m => m.FullNameWithYear())
                                        .Aggregate((f, s) => f + ", " + s)
                                    + "a " + Devmasters.Lang.CS.Plural.Get(rizeni.Dluznici.Count - 3, " jeden další", "{0} další",
                                        "{0} dalších")
                                    + ". "
                );
                data.Add(new InfoFact()
                {
                    Level = InfoFact.ImportanceLevel.High,
                    Text = sumTxt
                });
            }

            if (rizeni.Veritele.Count > 0)
            {
                sumTxt = Devmasters.Lang.CS.Plural.GetWithZero(rizeni.Veritele.Count,
                    "",
                    "Evidujeme jednoho věřitele.",
                    "Evidujeme {0} věřitele.",
                    "Evidujeme {0} věřitelů."
                );
                data.Add(new InfoFact()
                {
                    Level = InfoFact.ImportanceLevel.High,
                    Text = sumTxt
                });
            }

            return data.OrderByDescending(o => o.Level).ToArray();
        }
    }
}