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
using Serilog;

namespace HlidacStatu.Extensions
{
    public static class OsobaExtension
    {
        private static readonly ILogger _logger = Log.ForContext(typeof(OsobaExtension));

        public static async Task<string> CurrentPoliticalPartyAsync(this Osoba osoba)
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

            var zkratka = await ZkratkaStranyRepo.NazevStranyForIcoAsync(ico);

            if (!string.IsNullOrWhiteSpace(zkratka))
                return zkratka;

            return FirmaRepo.NameFromIcoAsync(ico);
        }


        public static async Task<ListItem> ToApiOsobaListItemAsync(this Osoba osoba)
        {
            if (osoba == null)
                return null;

            var party = await osoba.CurrentPoliticalPartyAsync();

            var res = new HlidacStatu.DS.Api.Osoba.ListItem
            {
                Person_Id = osoba.NameId,
                Name = osoba.Jmeno,
                Surname = osoba.Prijmeni,
                Year_Of_Birth = osoba.Narozeni.HasValue ? osoba.Narozeni.Value.Year.ToString() : null,
                Political_Involvement = osoba.StatusOsoby().ToNiceDisplayName(),
                Photo_Url = osoba.HasPhoto() ? osoba.GetPhotoUrl(false) : null,
                Current_Political_Party = party ?? "None",
                Have_More_Details = true,
                Involved_In_Companies_Count = (await osoba.AktualniVazbyAsync(
                        DS.Graphs.Relation.CharakterVazbyEnum.VlastnictviKontrola,
                        DS.Graphs.Relation.AktualnostType.Nedavny))
                    .Where(v => v.To != null && v.To.Type == HlidacStatu.DS.Graphs.Graph.Node.NodeType.Company)
                    .Select(v => v.To.Id)
                    .Distinct()
                    .Count()
            };
            return res;
        }

        public static async Task<Detail> ToApiOsobaDetailAsync(this Osoba osoba, DateTime? historyLimit = null)
        {
            historyLimit ??= DateTime.Now.AddYears(-100);
            if (osoba == null)
                return null;

            var party = await osoba.CurrentPoliticalPartyAsync();

            var res = new HlidacStatu.DS.Api.Osoba.Detail
            {
                Person_Id = osoba.NameId,
                Name = osoba.Jmeno,
                Surname = osoba.Prijmeni,
                Year_Of_Birth = osoba.Narozeni.HasValue ? osoba.Narozeni.Value.Year.ToString() : null,
                Political_Involvement = osoba.StatusOsoby().ToNiceDisplayName(),
                Photo_Url = osoba.HasPhoto() ? osoba.GetPhotoUrl(false) : null,
                Current_Political_Party = party ?? "None",

                Recent_Public_Activities_Description = await osoba.DescriptionAsync(false,
                    m => m.DatumDo == null || m.DatumDo > historyLimit, 5, itemDelimeter: ", "),

                Business_Contracts_With_Government = (await osoba.StatistikaRegistrSmluvAsync())
                    .SmlouvyStat_SoukromeFirmySummary()
                    .Select(m => new HlidacStatu.DS.Api.Osoba.Detail.Stats
                    {
                        Year = m.Year,
                        Number_Of_Contracts = m.Value.PocetSmluv,
                        Total_Contract_Value = m.Value.CelkovaHodnotaSmluv
                    }).ToArray()
            };

            var vazby = await osoba.AktualniVazbyAsync(DS.Graphs.Relation.CharakterVazbyEnum.VlastnictviKontrola,
                DS.Graphs.Relation.AktualnostType.Nedavny);

            var firmy = vazby
                .Where(v => v.To != null && v.To.Type == HlidacStatu.DS.Graphs.Graph.Node.NodeType.Company)
                .GroupBy(f => f.To.Id)
                .ToList();

            var involvedList = new List<HlidacStatu.DS.Api.Osoba.Detail.Subject>();
            foreach (var group in firmy)
            {
                var firmaName = await group.First().To.PrintNameAsync();
                involvedList.Add(new HlidacStatu.DS.Api.Osoba.Detail.Subject
                {
                    Ico = group.Key,
                    Name = firmaName
                });
            }

            res.Involved_In_Companies = involvedList.ToArray();

            return res;
        }


        public static async Task<bool> IsSponzorAsync(this Osoba osoba)
        {
            return (await osoba.SponzoringAsync()).Any();
        }

        public static async Task<List<Sponzoring>> SponzoringAsync(this Osoba osoba)
        {
            return await osoba.SponzoringAsync(m => true);
        }


        public static async Task<string> SponzoringToHtmlAsync(this Osoba osoba, int take = int.MaxValue,
            string template = "{0}",
            string itemTemplate = "{0}", string itemDelimeter = "<br/>",
            Expression<Func<Sponzoring, bool>> sponzoringFilter = null, bool withCompany = true)
        {
            Expression<Func<Sponzoring, bool>> all = e => true;

            sponzoringFilter = sponzoringFilter ?? all;

            var sponsoring = (await osoba.SponzoringAsync(sponzoringFilter, withCompany: withCompany))
                .OrderByDescending(s => s.DarovanoDne)
                .Take(take);

            var htmlItems = new List<string>();
            foreach (var s in sponsoring)
            {
                htmlItems.Add(await s.ToHtmlAsync(itemTemplate));
            }

            return string.Format(template, string.Join(itemDelimeter, htmlItems));
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

        public static async Task<Sponzoring> AddSponsoringAsync(this Osoba osoba, Sponzoring sponzoring, string user)
        {
            sponzoring.OsobaIdDarce = osoba.InternalId;
            var result = await SponzoringRepo.CreateAsync(sponzoring, user);
            return result;
        }

        //při injectnutém db contextu se nesmí dělat paralelní operace
        public static async Task<string> DescriptionAsync(this Osoba osoba, bool html,
            Expression<Func<OsobaEvent, bool>> predicate,
            int numOfRecords = int.MaxValue, string template = "{0}",
            string itemTemplate = "{0}", string itemDelimeter = "<br/>",
            bool withSponzoring = false, DbEntities db = null)
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

            var orderedEvents = events
                .OrderBy(o =>
                {
                    var index = fixedOrder.IndexOf(o.Type);
                    return index == -1 ? int.MaxValue : index;
                })
                .ThenByDescending(o => o.DatumOd)
                .Take(numOfRecords);

            List<string> evs = new List<string>();
            foreach (var e in orderedEvents)
            {
                var s = await (html ? e.RenderHtmlAsync(", ") : e.RenderTextAsync(" "));
                evs.Add(string.Format(itemTemplate, s));
            }

            if (withSponzoring && html)
            {
                var numOfSponzoring = numOfRecords - evs.Count;

                bool ownsContext = db == null;
                if (ownsContext)
                    db = new DbEntities();
                List<Sponzoring> sponzoring;
                try
                {
                    sponzoring = await SponzoringRepo.GetByDarceAsync(osoba.InternalId, m => true, withCompany: true, db);
                }
                finally
                {
                    if (ownsContext && db != null)
                    {
                        await db.DisposeAsync();
                    }
                }

                var topSponzoring = sponzoring
                    .OrderByDescending(s => s.DarovanoDne)
                    .Take(numOfSponzoring);

                var sponzoringList = new List<string>();
                foreach (var s in topSponzoring)
                {
                    sponzoringList.Add(await s.ToHtmlAsync());
                }

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

        //při injectnutém db contextu se nesmí dělat paralelní operace
        public static async Task<InfoFact[]> InfoFactsAsync(this Osoba osoba,
            HashSet<Fact.ImportanceLevel> excludedImportanceLevels = null, DbEntities db = null)
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


            bool ownsContext = db == null;
            if (ownsContext)
                db = new DbEntities();

            try
            {
                var kdoje = await osoba.DescriptionAsync(false,
                    m => types.Contains(m.Type),
                    2, itemDelimeter: ", ", db: db);

                if (excludedImportanceLevels is null ||
                    !excludedImportanceLevels.Contains(Fact.ImportanceLevel.Summary))
                {
                    var descr = "";
                    if (osoba.StatusOsoby() == Osoba.StatusOsobyEnum.NeniPolitik)
                        descr = $"<b>{osoba.FullName()}</b>";
                    else
                        descr = $"<b>{osoba.FullNameWithYear()}</b>";
                    if (!string.IsNullOrEmpty(kdoje))
                        descr += ", " + kdoje + (kdoje.EndsWith(". ") ? "" : ". ");
                    f.Add(new InfoFact(descr, Fact.ImportanceLevel.Summary));
                }

                if (excludedImportanceLevels is null ||
                    !excludedImportanceLevels.Contains(Fact.ImportanceLevel.Stat))
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
                        var neziskovkyCount = stat.SmlouvyStat_NeziskovkyCount();
                        var komercniFirmyCount = stat.SmlouvyStat_KomercniFirmyCount();
                        if (neziskovkyCount > 0 && komercniFirmyCount == 0)
                        {
                            statDesc +=
                                $"{Plural.Get(neziskovkyCount, "jedné neziskové organizaci", "{0} neziskových organizacích", "{0} neziskových organizacích")}";
                        }
                        else if (neziskovkyCount > 0)
                        {
                            statDesc +=
                                $"{Plural.Get(neziskovkyCount, "jedné neziskové organizaci", "{0} neziskových organizacích", "{0} neziskových organizacích")}";
                            statDesc +=
                                $" a {Plural.Get(komercniFirmyCount, "jedné soukr.firmě", "{0} soukr.firmách", "{0} soukr.firmách")}";
                        }
                        else
                        {
                            statDesc +=
                                $"{Plural.Get(stat.SoukromeFirmy.Count, "jedné soukr.firmě", "{0} soukr.firmách", "{0} soukr.firmách")}";
                        }


                        statDesc += $"</b>. Tyto subjekty mají se státem od 2016 celkem "
                                    + Plural.Get(soukrStat.Sum(m => m.PocetSmluv), "jednu smlouvu",
                                        "{0} smlouvy", "{0} smluv")
                                    + " v celkové výši <b>" + Smlouva.NicePrice(
                                        soukrStat.Sum(m => m.CelkovaHodnotaSmluv),
                                        html: true, shortFormat: true)
                                    + "</b>. ";
                    }

                    if (statDesc.Length > 0)
                        f.Add(new InfoFact(statDesc, Fact.ImportanceLevel.Stat));
                }

                if (excludedImportanceLevels is null ||
                    !excludedImportanceLevels.Contains(Fact.ImportanceLevel.Salary))
                {
                    var year = PpRepo.DefaultYear;
                    var prijmy = await PpRepo.GetPrijmyPolitikaAsync(osoba.NameId, year, db: db);
                    if (prijmy.Any())
                        f.Add(new InfoFact(
                            $"Příjem od státu v {year} celkem <b>{RenderData.ShortNicePrice(prijmy.Sum(m => m.CelkoveRocniNakladyNaPolitika), showDecimal: RenderData.ShowDecimalVal.Show)}</b> od <b> {Plural.Get(prijmy.Select(m => m.IdOrganizace).Distinct().Count(), "jedné</b> organizace", "{0}</b> organizací", "{0}</b> organizací")}.",
                            Fact.ImportanceLevel.Salary
                        ));
                }

                if (excludedImportanceLevels is null ||
                    !excludedImportanceLevels.Contains(Fact.ImportanceLevel.Medium))
                {
                    DateTime datumOd = new DateTime(DateTime.Now.Year - 10, 1, 1);
                    var sponzoringPrimy = await SponzoringRepo.GetByDarceAsync(osoba.InternalId, s =>
                            s.IcoPrijemce != null
                            && s.DarovanoDne >= datumOd
                            && s.Typ != (int)Sponzoring.TypDaru
                                .DarFirmy,
                        withCompany: true,
                        db: db);

                    if (sponzoringPrimy != null && sponzoringPrimy.Any())
                    {
                        string[] strany = sponzoringPrimy.Select(m => m.IcoPrijemce).Distinct().ToArray();
                        int?[] roky = sponzoringPrimy.Select(m => m.DarovanoDne?.Year).Where(x => x != null)
                            .Distinct().OrderBy(y => y).ToArray();
                        decimal celkem = sponzoringPrimy.Sum(m => m.Hodnota) ?? 0;
                        decimal top = sponzoringPrimy.Max(m => m.Hodnota) ?? 0;
                        //todo: přidat tabulku politických stran a změnit zde na název strany
                        string prvniStrana = (await FirmaRepo.FromIcoAsync(strany[0], db: db))?.Jmeno;

                        if (!string.IsNullOrWhiteSpace(prvniStrana))
                        {
                            f.Add(new InfoFact($"{osoba.FullName()} "
                                               + Plural.Get(roky.Count(), "v roce " + roky[0],
                                                   $"mezi roky {roky.First()} - {roky.Last() - 2000}",
                                                   $"mezi roky {roky.First()} - {roky.Last() - 2000}")
                                               + $" sponzoroval{(osoba.Muz() ? "" : "a")} " +
                                               Plural.Get(strany.Length, "stranu " + prvniStrana,
                                                   "{0} polit. strany", "{0} polit. stran")
                                               + $" v&nbsp;celkové výši <b>{RenderData.ShortNicePrice(celkem, html: true)}</b>. "
                                               + $"Nejvyšší sponzorský dar byl ve výši {RenderData.ShortNicePrice(top, html: true)}. "
                                , Fact.ImportanceLevel.Medium)
                            );
                        }
                    }

                    var sponzoringPresFirmu = await SponzoringRepo.GetByDarceAsync(osoba.InternalId,
                        s => s.IcoPrijemce != null
                             && s.DarovanoDne >= datumOd
                             && s.Typ == (int)Sponzoring.TypDaru
                                 .DarFirmy,
                        withCompany: true,
                        db: db);

                    if (sponzoringPresFirmu != null && sponzoringPresFirmu.Any())
                    {
                        string[] strany = sponzoringPresFirmu.Select(m => m.IcoPrijemce).Distinct().ToArray();
                        int?[] roky = sponzoringPresFirmu.Select(m => m.DarovanoDne?.Year).Where(x => x != null)
                            .Distinct().OrderBy(y => y).ToArray();
                        decimal celkem = sponzoringPresFirmu.Sum(m => m.Hodnota) ?? 0;
                        decimal top = sponzoringPresFirmu.Max(m => m.Hodnota) ?? 0;
                        string prvniStrana = (await FirmaRepo.FromIcoAsync(strany[0], db: db))?.Jmeno;

                        if (!string.IsNullOrWhiteSpace(prvniStrana))
                        {
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
                                , Fact.ImportanceLevel.Medium)
                            );
                        }
                        
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

                            f.Add(new InfoFact(ss, Fact.ImportanceLevel.Medium));
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
                                         stat.SoukromeFirmy.Count(m =>
                                             m.Value != null && m.Value[rok - 1]?.PocetSmluv > 0),
                                         $"Jeden subjekt, ve které se angažoval{(osoba.Muz() ? "" : "a")}, v&nbsp;roce {rok} uzavřel",
                                         $"{{0}} subjekty, ve kterých se angažoval{(osoba.Muz() ? "" : "a")}, v&nbsp;roce {rok} uzavřely",
                                         $"{{0}} subjektů, ve kterých se angažoval{(osoba.Muz() ? "" : "a")}, v&nbsp;roce {rok} uzavřely"
                                     )
                                     + $" " +
                                     Plural.Get(soukrStat[rok - 1].PocetSmluv, " jednu smlouvu.",
                                         " {0} smlouvy", " {0} smluv")
                                     + "</b>. ";

                            f.Add(new InfoFact(ss, Fact.ImportanceLevel.Medium)
                            );
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Infofacts load for osoba{osoba.InternalId}-{osoba.NameId} failed.");
            }
            finally
            {
                if (ownsContext && db != null)
                {
                    await db.DisposeAsync();
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

        public static async Task<List<Sponzoring>> SponzoringAsync(this Osoba osoba,
            Expression<Func<Sponzoring, bool>> predicate,
            bool withCompany = true)
        {
            return await SponzoringRepo.GetByDarceAsync(osoba.InternalId, predicate, withCompany);
        }
    }
}