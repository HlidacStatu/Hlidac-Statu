using Devmasters;
using Devmasters.Enums;
using Devmasters.Lang.CS;
using HlidacStatu.Entities;
using HlidacStatu.Entities.Facts;
using HlidacStatu.Lib.Analytics;
using HlidacStatu.Repositories;
using HlidacStatu.Repositories.Statistics;
using HlidacStatu.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using HlidacStatu.DS.Api.Osoba;
using HlidacStatu.Repositories.Cache;

namespace HlidacStatu.Extensions
{
    public static class OsobaExtension
    {

        public static string CurrentPoliticalParty(this Osoba osoba)
        {
            var (organizace, ico) = osoba.Events(ev =>
                    ev.Type == (int)OsobaEvent.Types.PolitickaStrana
                    && (!ev.DatumDo.HasValue
                        || ev.DatumDo >= DateTime.Now)
                )
                .OrderByDescending(ev => ev.DatumOd)
                .Select(ev => (ev.Organizace, ev.Ico))
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(ico))
                return organizace;

            var zkratka = ZkratkaStranyRepo.NazevStranyForIco(ico);

            if (!string.IsNullOrWhiteSpace(zkratka))
                return zkratka;

            return FirmaRepo.NameFromIco(ico);
        }


        public static HlidacStatu.DS.Api.Osoba.ListItem ToApiOsobaListItem(this Osoba osoba)
        {
            if (osoba == null)
                return null;

            var res = new HlidacStatu.DS.Api.Osoba.ListItem
            {
                Person_Id = osoba.NameId,
                Name = osoba.Jmeno,
                Surname = osoba.Prijmeni,
                Year_Of_Birth = osoba.Narozeni.HasValue ? osoba.Narozeni.Value.Year.ToString() : null,
                Political_Involvement = osoba.StatusOsoby().ToNiceDisplayName(),
                Photo_Url = osoba.HasPhoto() ? osoba.GetPhotoUrl(false) : null,
                Current_Political_Party = osoba.CurrentPoliticalParty() ?? "None",
                Have_More_Details = true,
                //TODO zvazit pridani vazeb urednich
                Involved_In_Companies_Count = osoba.AktualniVazby( DS.Graphs.Relation.CharakterVazbyEnum.VlastnictviKontrola, DS.Graphs.Relation.AktualnostType.Nedavny)
                        .Where(v => v.To != null && v.To.Type == HlidacStatu.DS.Graphs.Graph.Node.NodeType.Company)
                        .GroupBy(f => f.To.Id, v => v, (ico, v) => new
                        {
                            ICO = ico,
                            FirmaName = v.First().To.PrintName(),//HlidacStatu.Lib.Data.External.FirmyDB.NameFromIco(ico, true),
                        }).Count(),
            };
            return res;
        }

        public static async Task<Detail> ToApiOsobaDetailAsync(this Osoba osoba, DateTime? historyLimit = null)
        {
            historyLimit ??= DateTime.Now.AddYears(-100);
            if (osoba == null)
                return null;

            var res = new HlidacStatu.DS.Api.Osoba.Detail
            {
                Person_Id = osoba.NameId,
                Name = osoba.Jmeno,
                Surname = osoba.Prijmeni,
                Year_Of_Birth = osoba.Narozeni.HasValue ? osoba.Narozeni.Value.Year.ToString() : null,
                Political_Involvement = osoba.StatusOsoby().ToNiceDisplayName(),
                Photo_Url = osoba.HasPhoto() ? osoba.GetPhotoUrl(false) : null,
                Current_Political_Party = osoba.CurrentPoliticalParty() ?? "None",

                Recent_Public_Activities_Description = osoba.Description(false, m => m.DatumDo == null || m.DatumDo > historyLimit, 5, itemDelimeter: ", "),
                //TODO zvazit pridani vazeb urednich
                Involved_In_Companies = osoba.AktualniVazby( DS.Graphs.Relation.CharakterVazbyEnum.VlastnictviKontrola, DS.Graphs.Relation.AktualnostType.Nedavny)
                    .Where(v => v.To != null && v.To.Type == HlidacStatu.DS.Graphs.Graph.Node.NodeType.Company)
                    .GroupBy(f => f.To.Id, v => v, (ico, v) => new
                    {
                        ICO = ico,
                        FirmaName = v.First().To.PrintName(),//HlidacStatu.Lib.Data.External.FirmyDB.NameFromIco(ico, true),
                    })
                    .Select(m => new HlidacStatu.DS.Api.Osoba.Detail.Subject
                    {
                        Ico = m.ICO,
                        Name = m.FirmaName
                    }).ToArray(),

                Business_Contracts_With_Government = (await osoba.StatistikaRegistrSmluvAsync())
                .SmlouvyStat_SoukromeFirmySummary()
                    .Select(m => new HlidacStatu.DS.Api.Osoba.Detail.Stats
                    {
                        Year = m.Year,
                        Number_Of_Contracts = m.Value.PocetSmluv,
                        Total_Contract_Value = m.Value.CelkovaHodnotaSmluv
                    }).ToArray()

            };
            return res;
        }



        public static bool IsSponzor(this Osoba osoba)
        {
            return osoba.Sponzoring().Any();
        }

        public static IEnumerable<Sponzoring> Sponzoring(this Osoba osoba)
        {
            return osoba.Sponzoring(m => true);
        }


        public static string SponzoringToHtml(this Osoba osoba, int take = int.MaxValue, string template = "{0}",
            string itemTemplate = "{0}", string itemDelimeter = "<br/>",
            Expression<Func<Sponzoring, bool>> sponzoringFilter = null, bool withCompany = true)
        {
            Expression<Func<Sponzoring, bool>> all = e => true;

            sponzoringFilter = sponzoringFilter ?? all;
            return
                string.Format(template, string.Join(itemDelimeter,
                    osoba.Sponzoring(sponzoringFilter, withCompany: withCompany).AsQueryable()
                        .OrderByDescending(s => s.DarovanoDne)
                        .Select(s => s.ToHtml(itemTemplate))
                        .Take(take))
                );
        }

        public static IEnumerable<OsobaEvent> Events(this Osoba osoba)
        {
            return osoba.Events(m => true);
        }

        public static IEnumerable<SocialContact> GetSocialContacts(this Osoba osoba)
        {
            return osoba.Events(oe => oe.Type == (int)OsobaEvent.Types.SocialniSite)
                .Select(oe => new SocialContact
                {
                    Network = Enum.TryParse<OsobaEvent.SocialNetwork>(oe.Organizace, true, out var socialNetwork)
                        ? socialNetwork
                        : (OsobaEvent.SocialNetwork?)null,
                    NetworkText = oe.Organizace,
                    Contact = oe.AddInfo
                });
        }

        public static IQueryable<OsobaEvent> NoFilteredEvents(this Osoba osoba)
        {
            return osoba.NoFilteredEvents(m => true);
        }

        public static IQueryable<OsobaEvent> NoFilteredEvents(this Osoba osoba,
            Expression<Func<OsobaEvent, bool>> predicate)
        {
            if (osoba.InternalId == 0)
                return new List<OsobaEvent>().AsQueryable();

            IQueryable<OsobaEvent> oe = Osoby.CachedEvents.Get(osoba.InternalId)
                .AsQueryable();
            return oe.Where(predicate);
        }

        public static IEnumerable<OsobaEvent> Events(this Osoba osoba, Expression<Func<OsobaEvent, bool>> predicate)
        {
            List<OsobaEvent> events = osoba.NoFilteredEvents()
                .Where(predicate)
                .ToList();

            return events;
        }

        public static IEnumerable<OsobaEvent> MergedEvents(this Osoba osoba,
            Expression<Func<OsobaEvent, bool>> predicate)
        {
            var events = osoba.NoFilteredEvents()
                .Where(predicate)
                .ToArray();

            for (int currentIndex = 0; currentIndex < events.Length; currentIndex++)
            {
                for (int compareTo = currentIndex + 1; compareTo < events.Length; compareTo++)
                {
                    // nebudeme porovnávat sám se sebou

                    if (events[currentIndex].IsOverlaping(events[compareTo], out var mergedEvent))
                    {
                        events[compareTo] = mergedEvent;
                        events[currentIndex] = null; // je potřeba zahodit zmergovaný
                        break; // pokud někam uložíme, můžeme pokračovat dalším
                    }
                }
            }

            //odstranit prázdné
            return events.Where(e => e != null);
        }

        public static IEnumerable<OsobaEvent> Events_VerejnopravniUdalosti(this Osoba osoba)
        {
            return osoba.Events_VerejnopravniUdalosti(e => true);
        }

        public static IEnumerable<OsobaEvent> Events_VerejnopravniUdalosti(this Osoba osoba,
            Expression<Func<OsobaEvent, bool>> predicate)
        {
            return osoba.Events(predicate)
                .Where(e => Osoba.VerejnopravniUdalosti.Contains(e.Type));
        }

        public static Sponzoring AddSponsoring(this Osoba osoba, Sponzoring sponzoring, string user)
        {
            sponzoring.OsobaIdDarce = osoba.InternalId;
            var result = SponzoringRepo.Create(sponzoring, user);
            return result;
        }

        public static string Description(this Osoba osoba, bool html, Expression<Func<OsobaEvent, bool>> predicate,
            int numOfRecords = int.MaxValue, string template = "{0}",
            string itemTemplate = "{0}", string itemDelimeter = "<br/>",
            bool withSponzoring = false)
        {
            var fixedOrder = new List<int>()
            {
                (int)OsobaEvent.Types.VolenaFunkce,
                (int)OsobaEvent.Types.PolitickaExekutivni,
                (int)OsobaEvent.Types.Politicka,
                (int)OsobaEvent.Types.VerejnaSpravaJine,
                (int)OsobaEvent.Types.VerejnaSpravaExekutivni,
                (int)OsobaEvent.Types.Osobni,
                (int)OsobaEvent.Types.Jine
            };

            var events = osoba.MergedEvents(predicate).ToArray();

            List<string> evs = events
                .OrderBy(o =>
                {
                    var index = fixedOrder.IndexOf(o.Type);
                    return index == -1 ? int.MaxValue : index;
                })
                .ThenByDescending(o => o.DatumOd)
                .Take(numOfRecords)
                .Select(e => html ? e.RenderHtml(", ") : e.RenderText(" "))
                .Select(s => string.Format(itemTemplate, s))
                .ToList();

            if (withSponzoring && html)
            {
                var numOfSponzoring = numOfRecords - evs.Count;
                var sponzoringList = osoba.Sponzoring()
                    .OrderByDescending(s => s.DarovanoDne)
                    .Take(numOfSponzoring)
                    .Select(s => s.ToHtml());
                evs.AddRange(sponzoringList);
            }

            if (!evs.Any())
                return string.Empty;


            return string.Format(template,
                string.Join(itemDelimeter, evs)
            );
        }


        //tohle do repositories
        public static async Task<Osoba.Statistics.RegistrSmluv> StatistikaRegistrSmluvAsync(this Osoba osoba,
            int? obor = null, bool forceUpdateCache = false)
        {
            var ret = await OsobaStatistics.CachedStatistics_SmlouvyAsync(osoba, obor, forceUpdateCache);
            if (ret == null)
                return null;
            foreach (var k in ret.SoukromeFirmy.Keys)
                if (ret.SoukromeFirmy[k] == null)
                    ret.SoukromeFirmy[k] = new StatisticsSubjectPerYear<Smlouva.Statistics.Data>();
            foreach (var k in ret.StatniFirmy.Keys)
                if (ret.StatniFirmy[k] == null)
                    ret.StatniFirmy[k] = new StatisticsSubjectPerYear<Smlouva.Statistics.Data>();

            return ret;
        }
        public static async Task<Osoba.Statistics.Dotace> StatistikaDotaceAsync(this Osoba osoba,
            bool forceUpdateCache = false)
        {

            Osoba.Statistics.Dotace ret = await OsobaStatistics.CachedStatistics_DotaceAsync(osoba, forceUpdateCache);

            return ret;
        }
        
        
        public static async Task<InfoFact[]> InfoFactsCachedAsync(this Osoba osoba, bool forceUpdateCache = false)
        {
            if (forceUpdateCache)
                await OsobaCache.InvalidateInfoFactsAsync(osoba);
            
            return await OsobaCache.GetInfoFactsAsync(osoba);
        }

        public static async Task<InfoFact[]> InfoFactsAsync(this Osoba osoba,
            HashSet<InfoFact.ImportanceLevel> excludedImportanceLevels = null)
        {
            int[] types =
            {
                (int)OsobaEvent.Types.VolenaFunkce,
                (int)OsobaEvent.Types.PolitickaExekutivni,
                (int)OsobaEvent.Types.Politicka,
                (int)OsobaEvent.Types.VerejnaSpravaJine,
                (int)OsobaEvent.Types.VerejnaSpravaExekutivni,
                (int)OsobaEvent.Types.Osobni,
                (int)OsobaEvent.Types.Jine
            };

            List<InfoFact> f = new List<InfoFact>();
            var stat = await osoba.StatistikaRegistrSmluvAsync();
            StatisticsPerYear<Smlouva.Statistics.Data> soukrStat = stat.SoukromeFirmy.Values
                .AggregateStats();

            int rok = DateTime.Now.Year;
            if (DateTime.Now.Month <= 2)
                rok = rok - 1;
            var kdoje = osoba.Description(false,
                m => types.Contains(m.Type),
                2, itemDelimeter: ", ");

            if (excludedImportanceLevels is null ||
                !excludedImportanceLevels.Contains(InfoFact.ImportanceLevel.Summary))
            {
                var descr = "";
                if (osoba.StatusOsoby() == Osoba.StatusOsobyEnum.NeniPolitik)
                    descr = $"<b>{osoba.FullName()}</b>";
                else
                    descr = $"<b>{osoba.FullNameWithYear()}</b>";
                if (!string.IsNullOrEmpty(kdoje))
                    descr += ", " + kdoje + (kdoje.EndsWith(". ") ? "" : ". ");
                f.Add(new InfoFact(descr, InfoFact.ImportanceLevel.Summary));
            }

            if (excludedImportanceLevels is null ||
                !excludedImportanceLevels.Contains(InfoFact.ImportanceLevel.Stat))
            {
                var statDesc = "";
                if (stat.StatniFirmy.Count > 0)
                    statDesc +=
                        $"Angažoval se v {Plural.Get(stat.StatniFirmy.Count, "jedné státní firmě", "{0} státních firmách", "{0} státních firmách")}. ";
                //neziskovky
                if (stat.SoukromeFirmy.Count > 0)
                {
                    //ostatni
                    statDesc += $"Angažoval se {(stat.StatniFirmy.Count > 0 ? "také" : "")} v <b>";
                    if (stat.SmlouvyStat_NeziskovkyCount() > 0 && stat.SmlouvyStat_KomercniFirmyCount() == 0)
                    {
                        statDesc +=
                            $"{Plural.Get(stat.SmlouvyStat_NeziskovkyCount(), "jedné neziskové organizaci", "{0} neziskových organizacích", "{0} neziskových organizacích")}";
                    }
                    else if (stat.SmlouvyStat_NeziskovkyCount() > 0)
                    {
                        statDesc +=
                            $"{Plural.Get(stat.SmlouvyStat_NeziskovkyCount(), "jedné neziskové organizaci", "{0} neziskových organizacích", "{0} neziskových organizacích")}";
                        statDesc +=
                            $" a {Plural.Get(stat.SmlouvyStat_KomercniFirmyCount(), "jedné soukr.firmě", "{0} soukr.firmách", "{0} soukr.firmách")}";
                    }
                    else
                    {
                        statDesc +=
                            $"{Plural.Get(stat.SoukromeFirmy.Count, "jedné soukr.firmě", "{0} soukr.firmách", "{0} soukr.firmách")}";
                    }


                    statDesc += $"</b>. Tyto subjekty mají se státem od 2016 celkem "
                                + Plural.Get(soukrStat.Sum(m => m.PocetSmluv), "jednu smlouvu",
                                    "{0} smlouvy", "{0} smluv")
                                + " v celkové výši <b>" + Smlouva.NicePrice(soukrStat.Sum(m => m.CelkovaHodnotaSmluv),
                                    html: true, shortFormat: true)
                                + "</b>. ";
                }

                if (statDesc.Length > 0)
                    f.Add(new InfoFact(statDesc, InfoFact.ImportanceLevel.Stat));
            }

            if (excludedImportanceLevels is null ||
                !excludedImportanceLevels.Contains(InfoFact.ImportanceLevel.Salary))
            {
                var year = PpRepo.DefaultYear;
                var prijmy = await PpRepo.GetPrijmyPolitikaAsync(osoba.NameId, year);
                if (prijmy.Any())
                    f.Add(new InfoFact(
                        $"Příjem od státu v {year} celkem <b>{HlidacStatu.Util.RenderData.ShortNicePrice(prijmy.Sum(m=>m.CelkoveRocniNakladyNaPolitika), showDecimal: RenderData.ShowDecimalVal.Show)}</b> od <b> {Devmasters.Lang.CS.Plural.Get(prijmy.Select(m => m.IdOrganizace).Distinct().Count(),"jedné</b> organizace", "{0}</b> organizací", "{0}</b> organizací")}.",
                         Fact.ImportanceLevel.Salary
                        )); 
            }

            if (excludedImportanceLevels is null ||
            !excludedImportanceLevels.Contains(InfoFact.ImportanceLevel.Medium))
            {
                DateTime datumOd = new DateTime(DateTime.Now.Year - 10, 1, 1);
                var sponzoringPrimy = osoba.Sponzoring(s => s.IcoPrijemce != null
                                                            && s.DarovanoDne >= datumOd
                                                            && s.Typ != (int)Entities.Sponzoring.TypDaru.DarFirmy)
                    .ToList();
                if (sponzoringPrimy != null && sponzoringPrimy.Count() > 0)
                {
                    string[] strany = sponzoringPrimy.Select(m => m.IcoPrijemce).Distinct().ToArray();
                    int[] roky = sponzoringPrimy.Select(m => m.DarovanoDne.Value.Year).Distinct().OrderBy(y => y)
                        .ToArray();
                    decimal celkem = sponzoringPrimy.Sum(m => m.Hodnota) ?? 0;
                    decimal top = sponzoringPrimy.Max(m => m.Hodnota) ?? 0;
                    //todo: přidat tabulku politických stran a změnit zde na název strany
                    string prvniStrana = (await FirmaRepo.FromIcoAsync(strany[0])).Jmeno; 

                    f.Add(new InfoFact($"{osoba.FullName()} "
                                       + Plural.Get(roky.Count(), "v roce " + roky[0],
                                           $"mezi roky {roky.First()} - {roky.Last() - 2000}",
                                           $"mezi roky {roky.First()} - {roky.Last() - 2000}")
                                       + $" sponzoroval{(osoba.Muz() ? "" : "a")} " +
                                       Plural.Get(strany.Length, "stranu " + prvniStrana,
                                           "{0} polit. strany", "{0} polit. stran")
                                       + $" v&nbsp;celkové výši <b>{RenderData.ShortNicePrice(celkem, html: true)}</b>. "
                                       + $"Nejvyšší sponzorský dar byl ve výši {RenderData.ShortNicePrice(top, html: true)}. "
                        , InfoFact.ImportanceLevel.Medium)
                    );
                }

                var sponzoringPresFirmu = osoba.Sponzoring(s => s.IcoPrijemce != null
                                                                && s.DarovanoDne >= datumOd
                                                                && s.Typ == (int)Entities.Sponzoring.TypDaru.DarFirmy)
                    .ToList();
                if (sponzoringPresFirmu != null && sponzoringPresFirmu.Count() > 0)
                {
                    string[] strany = sponzoringPresFirmu.Select(m => m.IcoPrijemce).Distinct().ToArray();
                    int[] roky = sponzoringPresFirmu.Select(m => m.DarovanoDne.Value.Year).Distinct().OrderBy(y => y)
                        .ToArray();
                    decimal celkem = sponzoringPresFirmu.Sum(m => m.Hodnota) ?? 0;
                    decimal top = sponzoringPresFirmu.Max(m => m.Hodnota) ?? 0;
                    string prvniStrana = (await FirmaRepo.FromIcoAsync(strany[0])).Jmeno;

                    f.Add(new InfoFact($"{osoba.FullName()} byl{(osoba.Muz() ? "" : "a")}"
                                       + $" členem statutárního orgánu společnosti, která "
                                       + Plural.Get(roky.Count(), "v roce " + roky[0],
                                           $"mezi roky {roky.First()} - {roky.Last() - 2000}",
                                           $"mezi roky {roky.First()} - {roky.Last() - 2000}")
                                       + $" sponzorovala "
                                       + Plural.Get(strany.Length, "stranu " + prvniStrana,
                                           "{0} polit. strany", "{0} polit. stran")
                                       + $" v&nbsp;celkové výši <b>{RenderData.ShortNicePrice(celkem, html: true)}</b>. "
                                       + $"Nejvyšší sponzorský dar byl ve výši {RenderData.ShortNicePrice(top, html: true)}. "
                        , InfoFact.ImportanceLevel.Medium)
                    );
                }

                if (soukrStat.Sum(m => m.PocetSmluv) > 0)
                {
                    if (soukrStat[rok].PocetSmluv > 0)
                    {
                        string ss = "";

                        if (soukrStat[rok].CelkovaHodnotaSmluv == 0)
                            ss = Plural.Get(
                                     stat.SoukromeFirmy.Count(m => m.Value != null && m.Value[rok]?.PocetSmluv > 0),
                                     $"Jeden subjekt, ve kterém se angažoval{(osoba.Muz() ? "" : "a")}, v&nbsp;roce {rok} uzavřel",
                                     $"{{0}} subjekty, ve kterých se angažoval{(osoba.Muz() ? "" : "a")}, v&nbsp;roce {rok} uzavřely",
                                     $"{{0}} subjektů, ve kterých se angažuval{(osoba.Muz() ? "" : "a")}, v&nbsp;roce {rok} uzavřely"
                                 )
                                 + $" smlouvy v neznámé výši, protože <b>hodnota všech smluv byla utajena</b>. ";
                        else
                            ss = Plural.Get(
                                     stat.SoukromeFirmy.Count(m => m.Value != null && m.Value[rok]?.PocetSmluv > 0),
                                     $"Jeden subjekt, ve které se angažoval{(osoba.Muz() ? "" : "a")}, v&nbsp;roce {rok} uzavřel",
                                     $"{{0}} subjekty, ve kterých se angažoval{(osoba.Muz() ? "" : "a")}, v&nbsp;roce {rok} uzavřely",
                                     $"{{0}} subjektů, ve kterých se angažoval{(osoba.Muz() ? "" : "a")}, v&nbsp;roce {rok} uzavřely"
                                 )
                                 + $" " +
                                 Plural.Get(soukrStat[rok].PocetSmluv, " jednu smlouvu.", " {0} smlouvy",
                                     " {0} smluv")
                                 + "</b>. ";

                        f.Add(new InfoFact(ss, InfoFact.ImportanceLevel.Medium));
                    }
                    else if (soukrStat[rok - 1].CelkovaHodnotaSmluv == 0)
                    {
                        string ss = "";
                        if (soukrStat[rok].CelkovaHodnotaSmluv == 0)
                            ss = $"Je angažován{(osoba.Muz() ? "" : "a")} v&nbsp;" +
                                 Plural.Get(
                                     stat.SoukromeFirmy.Count(m => m.Value != null && m.Value[rok]?.PocetSmluv > 0),
                                     $"jednom subjektu, která v&nbsp;roce {rok - 1} uzavřela",
                                     $"{{0}} subjektech, které v&nbsp;roce {rok} uzavřely",
                                     $"{{0}} subjektech, které v&nbsp;roce {rok - 1} uzavřely"
                                 )
                                 + $" smlouvy v neznámé výši, protože <b>hodnota všech smluv byla utajena</b>. ";
                        else
                            ss = Plural.Get(
                                     stat.SoukromeFirmy.Count(m => m.Value != null && m.Value[rok - 1]?.PocetSmluv > 0),
                                     $"Jeden subjekt, ve které se angažoval{(osoba.Muz() ? "" : "a")}, v&nbsp;roce {rok} uzavřel",
                                     $"{{0}} subjekty, ve kterých se angažoval{(osoba.Muz() ? "" : "a")}, v&nbsp;roce {rok} uzavřely",
                                     $"{{0}} subjektů, ve kterých se angažoval{(osoba.Muz() ? "" : "a")}, v&nbsp;roce {rok} uzavřely"
                                 )
                                 + $" " +
                                 Plural.Get(soukrStat[rok - 1].PocetSmluv, " jednu smlouvu.",
                                     " {0} smlouvy", " {0} smluv")
                                 + "</b>. ";

                        f.Add(new InfoFact(ss, InfoFact.ImportanceLevel.Medium)
                        );
                    }
                }
            }

            var infoFacts = f.OrderByDescending(o => o.Level).ToArray();

            return infoFacts;
        }


        public static string SocialInfoTitle(this Osoba osoba)
        {
            return TextUtil.ShortenText(osoba.FullName(), 70);
        }

        public static string SocialInfoSubTitle(this Osoba osoba)
        {
            return osoba.NarozeniYear(true) + ", " + osoba.StatusOsoby().ToNiceDisplayName();
        }

        public static async Task<string> SocialInfoBodyAsync(this Osoba osoba)
        {
            return "<ul>"
                   + (await osoba.InfoFactsCachedAsync()).RenderFacts(
                       4, true, true, "", "<li>{0}</li>", true)
                   + "</ul>";
        }

        public static string SocialInfoFooter(this Osoba osoba)
        {
            return "Údaje k " + DateTime.Now.ToString("d. M. yyyy");
        }

        public static string SocialInfoImageUrl(this Osoba osoba)
        {
            return osoba.GetPhotoUrl();
        }

        public static IOrderedEnumerable<Osoba> OrderPoliticiByImportance(this IEnumerable<Osoba> source)
        {
            return source.OrderBy(o =>
                    {
                        var index = OsobaRepo.Searching.PolitikStatusImportanceOrder.IndexOf(o.Status);
                        return index == -1 ? int.MaxValue : index;
                    }
                )
                //podle posledni politicke funkce
                .ThenByDescending(o =>
                    o.Events(e => OsobaRepo.Searching.PolitikImportanceEventTypes.Contains(e.Type)).Max(e => e.DatumOd))
                //podle poctu event
                .ThenByDescending(o => o.Events_VerejnopravniUdalosti().Count());
        }

        public static IEnumerable<Sponzoring> Sponzoring(this Osoba osoba, Expression<Func<Sponzoring, bool>> predicate,
            bool withCompany = true)
        {
            return SponzoringRepo.GetByDarce(osoba.InternalId, predicate, withCompany);
        }
    }
}