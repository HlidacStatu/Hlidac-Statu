using HlidacStatu.Entities;
using HlidacStatu.Entities.XSD;
using HlidacStatu.Repositories;
using HlidacStatu.Repositories.ES;
using HlidacStatu.Util;

using Nest;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HlidacStatu.Extensions
{
    public static class SmlouvaExtension
    {
        public static InfoFact[] InfoFacts(this Smlouva smlouva)
        {
            List<InfoFact> f = new List<InfoFact>();

            string hlavni = $"Smlouva mezi {Devmasters.TextUtil.ShortenText(smlouva.Platce.nazev, 60)} a "
                            + $"{Devmasters.TextUtil.ShortenText(smlouva.Prijemce.First().nazev, 60)}";
            if (smlouva.Prijemce.Count() == 0)
                hlavni += $".";
            else if (smlouva.Prijemce.Count() == 1)
                hlavni += $" a 1 dalším.";
            else if (smlouva.Prijemce.Count() > 1)
                hlavni += $" a {smlouva.Prijemce.Count() - 1} dalšími.";
            hlavni += (smlouva.CalculatedPriceWithVATinCZK == 0
                ? " Hodnota smlouvy je utajena."
                : " Hodnota smlouvy je " + RenderData
                    .ShortNicePrice(smlouva.CalculatedPriceWithVATinCZK, html: true,
                        showDecimal: RenderData.ShowDecimalVal.Show));

            f.Add(new InfoFact(hlavni, InfoFact.ImportanceLevel.Summary));

            //sponzori
            foreach (var subj in smlouva.Prijemce.Union(new Smlouva.Subjekt[] { smlouva.Platce }))
            {
                var firma = Firmy.Get(subj.ico);
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

            //issues
            if (smlouva.IsPartOfRegistrSmluv() && smlouva.znepristupnenaSmlouva() == false
                                               && smlouva.Issues != null && smlouva.Issues.Any(m =>
                                                   m.Public && m.Public && m.Importance != Entities.Issues
                                                       .ImportanceLevel.NeedHumanReview))
            {
                int count = 0;
                foreach (var iss in smlouva.Issues.Where(m =>
                        m.Public && m.Importance != Entities.Issues.ImportanceLevel.NeedHumanReview)
                    .OrderByDescending(m => m.Importance))
                {
                    if (smlouva.znepristupnenaSmlouva() && iss.IssueTypeId == -1) //vypis pouze info o znepristupneni
                    {
                        count++;
                        f.Add(new InfoFact(
                            $"<b>{iss.Title}</b><br/><small>{iss.TextDescription}</small>"
                            , InfoFact.ImportanceLevel.High)
                        );
                    }
                    else if (iss.Public &&
                             iss.Importance != Entities.Issues.ImportanceLevel.NeedHumanReview)
                    {
                        count++;
                        string importance = "";
                        switch (iss.Importance)
                        {
                            case Entities.Issues.ImportanceLevel.NeedHumanReview:
                            case Entities.Issues.ImportanceLevel.Ok:
                            case Entities.Issues.ImportanceLevel.Formal:
                                importance = "";
                                break;
                            case Entities.Issues.ImportanceLevel.Minor:
                            case Entities.Issues.ImportanceLevel.Major:
                                importance = "Nedostatek: ";
                                break;
                            case Entities.Issues.ImportanceLevel.Fatal:
                                importance = "Vážný nedostatek: ";
                                break;
                            default:
                                break;
                        }

                        f.Add(
                            new InfoFact(
                                $"<b>{importance}{(string.IsNullOrEmpty(importance) ? iss.Title : iss.Title.ToLower())}</b><br/><small>{iss.TextDescription}</small>"
                                , InfoFact.ImportanceLevel.Medium)
                        );
                    }

                    if (count >= 2)
                        break;
                }
            }
            else
                f.Add(new InfoFact("Žádné nedostatky u smlouvy jsme nenalezli.", InfoFact.ImportanceLevel.Low));


            //politici
            foreach (var ss in smlouva.Prijemce)
            {
                if (!string.IsNullOrEmpty(ss.ico) && StaticData.FirmySVazbamiNaPolitiky_nedavne_Cache.Get()?
                    .SoukromeFirmy?.ContainsKey(ss.ico)==true)
                {
                    var politici = StaticData.FirmySVazbamiNaPolitiky_nedavne_Cache.Get().SoukromeFirmy[ss.ico];
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

                        f.Add(new InfoFact($"V dodavateli {Firmy.GetJmeno(ss.ico)} se "
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

            var _infofacts = f.OrderByDescending(o => o.Level).ToArray();

            return _infofacts;
        }



        public static async Task<Smlouva[]> OtherVersionsAsync(this Smlouva smlouva)
        {
            var result = new List<Smlouva>();


            var res = await SmlouvaRepo.Searching.SimpleSearchAsync("identifikator.idSmlouvy:" + smlouva.identifikator.idSmlouvy,
                1, 50, SmlouvaRepo.Searching.OrderResult.DateAddedDesc, null
            );
            var resNeplatne = await SmlouvaRepo.Searching.SimpleSearchAsync("identifikator.idSmlouvy:" + smlouva.identifikator.idSmlouvy,
                1, 50, SmlouvaRepo.Searching.OrderResult.DateAddedDesc, null, platnyZaznam: false
            );

            if (res.IsValid == false)
                Manager.LogQueryError<Smlouva>(res.ElasticResults);
            else
                result.AddRange(res.ElasticResults.Hits.Select(m => m.Source).Where(m => m.Id != smlouva.Id));

            if (resNeplatne.IsValid == false)
                Manager.LogQueryError<Smlouva>(resNeplatne.ElasticResults);
            else
                result.AddRange(resNeplatne.ElasticResults.Hits.Select(m => m.Source).Where(m => m.Id != smlouva.Id));

            var otherVersions = result.ToArray();

            List<QueryContainer> mustQs = new List<QueryContainer>(smlouva.SameContractSides());
            mustQs.AddRange(new QueryContainer[]
            {
                    new QueryContainerDescriptor<Smlouva>().Match(qm => qm.Field(f => f.predmet).Query(smlouva.predmet)),
                    new QueryContainerDescriptor<Smlouva>().Term(
                        t => t.Field(f => f.datumUzavreni).Value(smlouva.datumUzavreni)),
                    new QueryContainerDescriptor<Smlouva>().Term(t =>
                        t.Field(f => f.CalculatedPriceWithVATinCZK).Value(smlouva.CalculatedPriceWithVATinCZK)),
            });
            List<QueryContainer> optionalQs = new List<QueryContainer>();
            if (!string.IsNullOrEmpty(smlouva.cisloSmlouvy))
                optionalQs.Add(
                    new QueryContainerDescriptor<Smlouva>().Term(t =>
                        t.Field(f => f.cisloSmlouvy).Value(smlouva.cisloSmlouvy)));

            otherVersions = otherVersions
                .Union(await SmlouvaRepo.GetPodobneSmlouvyAsync(smlouva.Id, mustQs, optionalQs, otherVersions.Select(m => m.Id)))
                .ToArray();

            return otherVersions;
        }

        public static async Task<Smlouva[]> PodobneSmlouvy(this Smlouva smlouva)
        {
            QueryContainer[] niceToHaveQs = new QueryContainer[]
            {
                    new QueryContainerDescriptor<Smlouva>().Term(
                        t => t.Field(f => f.datumUzavreni).Value(smlouva.datumUzavreni)),
                    new QueryContainerDescriptor<Smlouva>().Term(t =>
                        t.Field(f => f.CalculatedPriceWithVATinCZK).Value(smlouva.CalculatedPriceWithVATinCZK)),
            };

            var exceptContracts = await smlouva.OtherVersionsAsync(); 
            var podobneSmlouvy = await SmlouvaRepo.GetPodobneSmlouvyAsync(smlouva.Id, smlouva.SameContractSides(), niceToHaveQs,
                exceptContracts.Select(m => m.Id), 10);

            return podobneSmlouvy;
        }

        public static string SocialInfoBody(this Smlouva smlouva)
        {
            return "<ul>" +
                   InfoFact.RenderInfoFacts(smlouva.InfoFacts(), 4, true, true, "", "<li>{0}</li>", true)
                   + "</ul>";
        }

        public static string SocialInfoFooter(this Smlouva smlouva)
        {
            return
                $"Smlouva byla podepsána {smlouva.datumUzavreni.ToString("d.M.yyyy")}, zveřejněna {smlouva.casZverejneni.ToString("d.M.yyyy")}";
        }

        public static string SocialInfoImageUrl(this Smlouva smlouva)
        {
            return string.Empty;
        }

        public static string SocialInfoSubTitle(this Smlouva smlouva)
        {
            if (smlouva.Issues.Any(m => m.Importance == Entities.Issues.ImportanceLevel.Fatal))
                return "Smlouva je formálně platná, ale <b>obsahuje závažné nedostatky v rozporu se zákonem!</b>";
            else
                return (smlouva.znepristupnenaSmlouva() ? "Zneplatněná smlouva." : "Platná smlouva.");
        }

        public static string SocialInfoTitle(this Smlouva smlouva)
        {
            return Devmasters.TextUtil.ShortenText(smlouva.predmet, 45, "...");
        }


        public static bool? PlatnostZaznamuVRS(this Smlouva smlouva)
        {
            if (smlouva.IsPartOfRegistrSmluv())
            {
                try
                {
                    string html = "";
                    using (Devmasters.Net.HttpClient.URLContent net = new Devmasters.Net.HttpClient.URLContent(smlouva.odkaz))
                    {
                        net.Timeout = 3000;
                        html = net.GetContent().Text;
                    }

                    bool existuje = html?.Contains("Smlouva byla znepřístupněna") == false;
                    if (existuje)
                    {
                        //download xml and check
                        //https://smlouvy.gov.cz/smlouva/9569679/xml/registr_smluv_smlouva_9569679.xml
                        //https://smlouvy.gov.cz/smlouva/13515796/xml/registr_smluv_smlouva_13515796.xml
                        var xmlUrl =
                            $"https://smlouvy.gov.cz/smlouva/{smlouva.identifikator.idVerze}/xml/registr_smluv_smlouva_{smlouva.identifikator.idVerze}.xml";
                        using (Devmasters.Net.HttpClient.URLContent net =
                            new Devmasters.Net.HttpClient.URLContent(xmlUrl))
                        {
                            net.Timeout = 3000;
                            html = net.GetContent().Text;
                            using (StringReader sr = new StringReader(html))
                            {
                                try
                                {
                                    System.Xml.Serialization.XmlSerializer xmlsZaznam =
                                        new System.Xml.Serialization.XmlSerializer(typeof(zaznam));
                                    var zaznam = xmlsZaznam.Deserialize(sr) as zaznam;
                                    if (zaznam != null)
                                        return zaznam.data.platnyZaznam == 1;
                                    else
                                        return null;
                                }
                                catch
                                {
                                    return null;
                                }
                            }
                        }
                    }
                    else
                        return false;
                }
                catch (Exception e)
                {
                    Consts.Logger.Error($"checking StavSmlouvyVRS id:{smlouva.Id} {smlouva.odkaz}", e);
                    return null;
                }
            }
            else //pokud nespada pod RS, pak je vzdy platna
                return true;
        }


        private static QueryContainer[] SameContractSides(this Smlouva smlouva)
        {
            QueryContainer[] mustQs = new QueryContainer[]
            {
                new QueryContainerDescriptor<Smlouva>().Term(t => t.Field("platce.ico").Value(smlouva.Platce.ico)),
            };
            mustQs = mustQs.Concat(smlouva.Prijemce
                .Where(m => !string.IsNullOrEmpty(m.ico))
                .Select(m =>
                    new QueryContainerDescriptor<Smlouva>().Term(t => t.Field("prijemce.ico").Value(m.ico))
                )
            ).ToArray();
            return mustQs;
        }



    }
}