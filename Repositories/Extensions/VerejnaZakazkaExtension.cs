using HlidacStatu.Entities.VZ;
using HlidacStatu.Repositories;
using HlidacStatu.Util;

using System;
using System.Collections.Generic;
using System.Linq;


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
            if ((verejnaZakazka.CPV?.Length ?? 0) == 0)
            {
                return "";
            }
            else if (verejnaZakazka.CPV?.Length == 1)
            {
                return $"{verejnaZakazka.CPVText(verejnaZakazka.CPV[0])} ({verejnaZakazka.CPV[0]})";
            }
            else if (verejnaZakazka.CPV?.Length == 2)
            {
                return $"{verejnaZakazka.CPVText(verejnaZakazka.CPV[0])} ({verejnaZakazka.CPV[0]})"
                       + $"{verejnaZakazka.CPVText(verejnaZakazka.CPV[1])} ({verejnaZakazka.CPV[1]})";
            }
            else
                return $"{verejnaZakazka.CPVText(verejnaZakazka.CPV[0])} ({verejnaZakazka.CPV[0]})"
                       + $"{verejnaZakazka.CPVText(verejnaZakazka.CPV[1])} ({verejnaZakazka.CPV[1]}) "
                       + Devmasters.Lang.CS.Plural.Get(verejnaZakazka.CPV.Length - 2, "a další obor", "+ {0} obory",
                           "+ {0} oborů")
                    ;
        }


        public static string SocialInfoBody(this VerejnaZakazka verejnaZakazka)
        {
            return "<ul>" +
                   InfoFact.RenderInfoFacts(verejnaZakazka.InfoFacts(), 4, true, true, "", "<li>{0}</li>", true)
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

        public static InfoFact[] InfoFacts(this VerejnaZakazka verejnaZakazka)
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

            f.Add(new InfoFact(hlavni, InfoFact.ImportanceLevel.Summary));

            //sponzori
            foreach (var subj in verejnaZakazka.Dodavatele.Union(
                new VerejnaZakazka.Subject[] { verejnaZakazka.Zadavatel }))
            {
                if (subj != null)
                {
                    var firma = Firmy.Get(subj.ICO);
                    if (firma.Valid && firma.IsSponzor() && firma.JsemSoukromaFirma())
                    {
                        f.Add(new InfoFact(
                            $"{firma.Jmeno}: " +
                            string.Join("<br />",
                                firma.Sponzoring()
                                    .OrderByDescending(s => s.DarovanoDne)
                                    .Select(s => s.ToHtml())
                                    .Take(2)),
                            InfoFact.ImportanceLevel.Medium)
                        );
                    }
                }
            }


            //politici
            foreach (var ss in verejnaZakazka.Dodavatele)
            {
                if (!string.IsNullOrEmpty(ss.ICO) && StaticData.FirmySVazbamiNaPolitiky_nedavne_Cache.Get()
                    .SoukromeFirmy.ContainsKey(ss.ICO))
                {
                    var politici = StaticData.FirmySVazbamiNaPolitiky_nedavne_Cache.Get().SoukromeFirmy[ss.ICO];
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
                            , InfoFact.ImportanceLevel.Medium)
                        );
                    }
                }
            }

            var infofacts = f.OrderByDescending(o => o.Level).ToArray();

            return infofacts;
        }

        public static VerejnaZakazka.ZakazkaSource ZdrojZakazkyUrl(this VerejnaZakazka verejnaZakazka)
        {
            //2006 https://old.vestnikverejnychzakazek.cz/cs/Searching/SearchContractNumber?cococode=847422
            //2016 https://www.vestnikverejnychzakazek.cz/SearchForm/SearchContract?contractNumber=
            string searchUrl = null;
            if (!string.IsNullOrEmpty(verejnaZakazka.EvidencniCisloZakazky))
            {
                string profilUrl = "";
                if (verejnaZakazka.Dataset == VerejnaZakazka.Post2016Dataset)
                    profilUrl = ProfilZadavateleRepo.GetByIdAsync(verejnaZakazka.ZakazkaNaProfiluId)?.Url?.Trim();
                else if (verejnaZakazka.Dataset == VerejnaZakazka.Pre2016Dataset)
                    profilUrl = ProfilZadavateleRepo.GetByIdAsync(verejnaZakazka.ZakazkaNaProfiluId)?.Url?.Trim();
                else if (!verejnaZakazka.Dataset.StartsWith("DatLab-"))
                    profilUrl = verejnaZakazka.Dataset ??
                                ProfilZadavateleRepo.GetByIdAsync(verejnaZakazka.ZakazkaNaProfiluId)?.Url?.Trim();
                if (Uri.TryCreate(profilUrl, UriKind.Absolute, out var profilUri))
                {
                    string googlQ = verejnaZakazka.EvidencniCisloZakazky + " site:" + profilUri.Host;
                    searchUrl =
                        $"https://www.google.cz/search?client=safari&rls=en&q={(System.Net.WebUtility.UrlEncode(googlQ))}&ie=UTF-8&oe=UTF-8";
                }
            }

            if (!string.IsNullOrEmpty(verejnaZakazka.EvidencniCisloZakazky))
            {
                if (verejnaZakazka.Dataset == VerejnaZakazka.Post2016Dataset)
                    return
                        new VerejnaZakazka.ZakazkaSource()
                        {
                            ZakazkaURL =
                                $"https://www.vestnikverejnychzakazek.cz/SearchForm/SearchContract?contractNumber={verejnaZakazka.EvidencniCisloZakazky}",
                            ProfilZadavatelUrl = ProfilZadavateleRepo.GetByIdAsync(verejnaZakazka.ZakazkaNaProfiluId)?.Url
                                ?.Trim(),
                            SearchZakazkaUrl = searchUrl
                        };
                else if (verejnaZakazka.Dataset == VerejnaZakazka.Pre2016Dataset)
                    return new VerejnaZakazka.ZakazkaSource()
                    {
                        ZakazkaURL =
                            $"https://old.vestnikverejnychzakazek.cz/cs/Searching/SearchContractNumber?cococode={verejnaZakazka.EvidencniCisloZakazky}",
                        ProfilZadavatelUrl =
                            ProfilZadavateleRepo.GetByIdAsync(verejnaZakazka.ZakazkaNaProfiluId)?.Url?.Trim(),
                        SearchZakazkaUrl = searchUrl
                    };
                else if (!verejnaZakazka.Dataset.StartsWith("DatLab-"))
                {
                    return new VerejnaZakazka.ZakazkaSource()
                    {
                        SearchZakazkaUrl = searchUrl,
                        ProfilZadavatelUrl = verejnaZakazka.Dataset ??
                                             ProfilZadavateleRepo.GetByIdAsync(verejnaZakazka.ZakazkaNaProfiluId)?.Url
                                                 ?.Trim()
                    };
                }
            }

            return null;
        }

        public static VerejnaZakazka.ExportedVZ.SubjectExport SubjectExport(
            VerejnaZakazka.Subject s)
        {
            VerejnaZakazka.ExportedVZ.SubjectExport subjectExport = new();
            if (s != null)
            {
                subjectExport.ICO = s.ICO;
                subjectExport.Jmeno = s.Jmeno;
            }
            if (!string.IsNullOrEmpty(s?.ICO))
            {
                var f = FirmaRepo.FromIco(s.ICO);
                if (f != null && f.Valid)
                {
                    subjectExport.KrajId = f.KrajId;
                    subjectExport.OkresId = f.OkresId;
                }
            }

            return subjectExport;
        }
    }
}