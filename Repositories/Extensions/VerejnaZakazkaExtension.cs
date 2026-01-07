using HlidacStatu.Entities.Facts;
using HlidacStatu.Entities.VZ;
using HlidacStatu.Repositories;
using HlidacStatu.Util;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HlidacStatu.Repositories.Cache;


namespace HlidacStatu.Extensions
{
    public static class VerejnaZakazkaExtension
    {
        public static string SocialInfoTitle(this VerejnaZakazka verejnaZakazka)
        {
            return Devmasters.TextUtil.ShortenText(verejnaZakazka.NazevZakazky, 50);
        }

        public static string SocialInfoSubTitle(this VerejnaZakazka verejnaZakazka)
        {
            if ((verejnaZakazka.CPV?.Count ?? 0) == 0)
            {
                return "";
            }
            
            if (verejnaZakazka.CPV?.Count == 1)
            {
                var cpv = verejnaZakazka.CPV.First();
                return $"{VerejnaZakazka.CPVToText(cpv)} ({cpv})";
            }
            
            if (verejnaZakazka.CPV?.Count == 2)
            {
                return string.Join("",verejnaZakazka.CPV.Select(cpv => $"{VerejnaZakazka.CPVToText(cpv)} ({cpv})"));
            }

            var cpvs = verejnaZakazka.CPV.ToArray();
            return $"{VerejnaZakazka.CPVToText(cpvs[0])} ({cpvs[0]})"
                   + $"{VerejnaZakazka.CPVToText(cpvs[1])} ({cpvs[1]}) "
                   + Devmasters.Lang.CS.Plural.Get(cpvs.Length - 2, "a další obor", "+ {0} obory",
                       "+ {0} oborů");
        }
        

        public static async Task<string> SocialInfoBodyAsync(this VerejnaZakazka verejnaZakazka)
        {
            return "<ul>" +
                   (await verejnaZakazka.InfoFactsAsync()).RenderFacts(4, true, true, "", "<li>{0}</li>", true)
                   + "</ul>";
        }


        public static string SocialInfoFooter(this VerejnaZakazka verejnaZakazka)
        {
            return verejnaZakazka.DatumUverejneni.HasValue
                ? "Poslední změna " + verejnaZakazka.DatumUverejneni.Value.ToShortDateString()
                : (verejnaZakazka.DatumUzavreniSmlouvy.HasValue
                    ? "Datum&nbsp;uzavření&nbsp;smlouvy " +
                      verejnaZakazka.DatumUzavreniSmlouvy.Value.ToShortDateString()
                    : "");
        }

        public static string SocialInfoImageUrl(this VerejnaZakazka verejnaZakazka)
        {
            return string.Empty;
        }

        public static async Task<InfoFact[]> InfoFactsAsync(this VerejnaZakazka verejnaZakazka)
        {
            List<InfoFact> f = new List<InfoFact>();

            string hlavni =
                $"Veřejná zakázka od <b>{Devmasters.TextUtil.ShortenText(verejnaZakazka.Zadavatel?.Jmeno, 60)}</b>"
                + (verejnaZakazka.DatumUverejneni.HasValue
                    ? " vyhlášena dne " + verejnaZakazka.DatumUverejneni.Value.ToShortDateString()
                    : ". "
                );
            if (verejnaZakazka.Dodavatele.Count() == 0)
                hlavni += $"";
            else if (verejnaZakazka.Dodavatele.Count() == 1)
                hlavni += $"Zakázku vyhrál <b>{verejnaZakazka.Dodavatele[0].Jmeno}</b>. ";
            else if (verejnaZakazka.Dodavatele.Count() > 1)
                hlavni += $"Zakázku vyhrál <b>{verejnaZakazka.Dodavatele[0].Jmeno}</b> a další. ";
            if (verejnaZakazka.KonecnaHodnotaBezDPH.HasValue && verejnaZakazka.KonecnaHodnotaBezDPH != 0)
            {
                hlavni +=
                    $"Konečná hodnota zakázky je <b>{RenderData.NicePrice(verejnaZakazka.KonecnaHodnotaBezDPH.Value, mena: verejnaZakazka.KonecnaHodnotaMena ?? "Kč")}</b>.";
            }
            else if (verejnaZakazka.OdhadovanaHodnotaBezDPH.HasValue && verejnaZakazka.OdhadovanaHodnotaBezDPH != 0)
            {
                hlavni +=
                    $"Odhadovaná hodnota zakázky je <b>{RenderData.NicePrice(verejnaZakazka.OdhadovanaHodnotaBezDPH.Value, mena: verejnaZakazka.OdhadovanaHodnotaMena ?? "Kč")}</b>.";
            }

            f.Add(new InfoFact(hlavni, Fact.ImportanceLevel.Summary));

            //sponzori
            foreach (var subj in verejnaZakazka.Dodavatele.Union(
                new VerejnaZakazka.Subject[] { verejnaZakazka.Zadavatel }))
            {
                if (subj != null)
                {
                    var firma = Firmy.Get(subj.ICO);
                    if (firma?.Valid == true && await firma.IsSponzorAsync() && firma.JsemSoukromaFirma())
                    {
                        f.Add(new InfoFact(
                            $"{firma.Jmeno}: " +
                            string.Join("<br />",
                                (await firma.SponzoringAsync())
                                    .OrderByDescending(s => s.DarovanoDne)
                                    .Select(s => s.ToHtml())
                                    .Take(2)),
                            Fact.ImportanceLevel.Medium)
                        );
                    }
                }
            }


            //politici
            foreach (var ss in verejnaZakazka.Dodavatele)
            {
                var firmySVazbamiNaPolitiky = await MaterializedViewsCache.FirmySVazbamiNaPolitiky_NedavneAsync();
                if (!string.IsNullOrEmpty(ss.ICO) && firmySVazbamiNaPolitiky.SoukromeFirmy.TryGetValue(ss.ICO, out var politici))
                {
                    if (politici.Count > 0)
                    {
                        var sPolitici = Osoby.GetById.Get(politici[0]).FullNameWithYear();
                        if (politici.Count == 2)
                        {
                            sPolitici = sPolitici + " a " + Osoby.GetById.Get(politici[1]).FullNameWithYear();
                        }
                        else if (politici.Count == 3)
                        {
                            sPolitici = sPolitici
                                        + ", "
                                        + Osoby.GetById.Get(politici[1]).FullNameWithYear()
                                        + " a "
                                        + Osoby.GetById.Get(politici[2]).FullNameWithYear();
                        }
                        else if (politici.Count > 3)
                        {
                            sPolitici = sPolitici
                                        + ", "
                                        + Osoby.GetById.Get(politici[1]).FullNameWithYear()
                                        + ", "
                                        + Osoby.GetById.Get(politici[2]).FullNameWithYear()
                                        + " a další";
                        }

                        f.Add(new InfoFact($"V dodavateli {Firmy.GetJmeno(ss.ICO)} se "
                                           + Devmasters.Lang.CS.Plural.Get(politici.Count()
                                               , " angažuje jedna politicky angažovaná osoba - "
                                               , " angažují {0} politicky angažované osoby - "
                                               , " angažuje {0} politicky angažovaných osob - ")
                                           + sPolitici + "."
                            , Fact.ImportanceLevel.Medium)
                        );
                    }
                }
            }

            var infofacts = f.OrderByDescending(o => o.Level).ToArray();

            return infofacts;
        }

        
    }
}