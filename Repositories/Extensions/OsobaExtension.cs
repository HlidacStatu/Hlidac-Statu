using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

using Devmasters;
using Devmasters.Enums;
using Devmasters.Lang.CS;

using HlidacStatu.Datastructures.Graphs;
using HlidacStatu.Entities;
using HlidacStatu.Lib.Analytics;
using HlidacStatu.Repositories;
using HlidacStatu.Repositories.Statistics;
using HlidacStatu.Util;

namespace HlidacStatu.Extensions
{
    public static class OsobaExtension
    {
        private static DateTime minLookBack = new DateTime(DateTime.Now.Year - 5, 1, 1);

        public static async Task<bool> MaVztahySeStatemAsync(this Osoba osoba)
        {
            //todo: jde zlepšit tak, že zavoláme všechny dotazy naráz a pokud jeden z nich něco najde,
            // tak nastavíme cancellation token
            return osoba.IsSponzor()
               || (await SmlouvaRepo.Searching.SimpleSearchAsync("osobaid:" + osoba.NameId, 1, 1, 0)).Total > 0
               || (await VerejnaZakazkaRepo.Searching.SimpleSearchAsync("osobaid:" + osoba.NameId, null, 1, 1, "0")).Total > 0
               || (await DotaceRepo.Searching.SimpleSearchAsync("osobaid:" + osoba.NameId, 1, 1, "0")).Total > 0;
        }

        public static bool IsPolitikBasedOnEvents(this Osoba osoba)
        {
            var ret = osoba.Events(ev =>
                    ev.Type == (int)OsobaEvent.Types.Politicka
                    || ev.Type == (int)OsobaEvent.Types.PolitickaPracovni
                    || ev.Type == (int)OsobaEvent.Types.VolenaFunkce
                )
                .Where(ev =>
                    (ev.DatumDo.HasValue && ev.DatumDo > minLookBack) ||
                    (ev.DatumDo.HasValue == false && ev.DatumOd > minLookBack.AddYears(-2)))
                .ToArray();

            return ret.Count() > 0;
        }


        public static string CurrentPoliticalParty(this Osoba osoba)
        {
            return osoba.Events(ev =>
                    ev.Type == (int)OsobaEvent.Types.Politicka
                    && (ev.AddInfo == "člen strany"
                        || ev.AddInfo == "předseda strany"
                        || ev.AddInfo == "předsedkyně strany"
                        || ev.AddInfo == "místopředseda strany"
                        || ev.AddInfo == "místopředsedkyně strany")
                    && (!ev.DatumDo.HasValue
                        || ev.DatumDo >= DateTime.Now)
                )
                .OrderByDescending(ev => ev.DatumOd)
                .Select(ev => ev.Organizace)
                .FirstOrDefault();
        }

        /// <summary>
        /// returns true if changed
        /// </summary>
        public static async Task<bool> RecalculateStatusAsync(this Osoba osoba)
        {
            switch (osoba.StatusOsoby())
            {
                case Osoba.StatusOsobyEnum.NeniPolitik:
                    if (osoba.IsPolitikBasedOnEvents())
                    {
                        osoba.Status = (int)Osoba.StatusOsobyEnum.Politik;
                        return true;
                    }

                    //TODO zkontroluj, ze neni politik podle eventu
                    break;

                case Osoba.StatusOsobyEnum.VazbyNaPolitiky:
                case Osoba.StatusOsobyEnum.ByvalyPolitik:
                case Osoba.StatusOsobyEnum.Sponzor:
                    if (osoba.IsPolitikBasedOnEvents())
                    {
                        osoba.Status = (int)Osoba.StatusOsobyEnum.Politik;
                        return true;
                    }

                    if (osoba.IsSponzor() == false && await osoba.MaVztahySeStatemAsync() == false)
                    {
                        osoba.Status = (int)Osoba.StatusOsobyEnum.NeniPolitik;
                        return true;
                    }

                    break;
                case Osoba.StatusOsobyEnum.Politik:
                    bool chgnd = false;
                    if (osoba.IsPolitikBasedOnEvents() == false)
                    {
                        osoba.Status = (int)Osoba.StatusOsobyEnum.NeniPolitik;
                        chgnd = true;
                    }

                    if (chgnd && osoba.IsSponzor() == false && await osoba.MaVztahySeStatemAsync() == false)
                    {
                        osoba.Status = (int)Osoba.StatusOsobyEnum.NeniPolitik;
                        chgnd = true;
                    }
                    else
                    {
                        osoba.Status = (int)Osoba.StatusOsobyEnum.Politik;
                        chgnd = false;
                    }

                    return chgnd;
                default:
                    break;
            }

            return false;
        }

        public static async Task<bool> NotInterestingToShowAsync(this Osoba osoba)
        {
            var showIt = osoba.StatusOsoby() == Osoba.StatusOsobyEnum.Politik
                || osoba.StatusOsoby() == Osoba.StatusOsobyEnum.ByvalyPolitik
                || osoba.StatusOsoby() == Osoba.StatusOsobyEnum.Sponzor
                || osoba.StatusOsoby() == Osoba.StatusOsobyEnum.VysokyUrednik
                || osoba.StatusOsoby() == Osoba.StatusOsobyEnum.VazbyNaPolitiky
                ;
            if (showIt)
                return !showIt;

            showIt = showIt || await osoba.MaVztahySeStatemAsync();
            if (showIt)
                return !showIt;

            showIt = showIt || osoba.IsSponzor();
            if (showIt)
                return !showIt;

            showIt = showIt || osoba.StatistikaRegistrSmluv(Relation.AktualnostType.Nedavny).SoukromeFirmySummary().Summary().PocetSmluv > 0;
            if (showIt)
                return !showIt;


            if (osoba.NameId == "radek-jonke")
                return true;

            return !showIt;
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
            string itemTemplate = "{0}", string itemDelimeter = "<br/>", Expression<Func<Sponzoring, bool>> sponzoringFilter = null)
        {
            Expression<Func<Sponzoring, bool>> all = e => true;

            sponzoringFilter = sponzoringFilter ?? all;
            return
                string.Format(template, string.Join(itemDelimeter,
                    osoba.Sponzoring().AsQueryable()
                        .Where(sponzoringFilter)
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

        public static IEnumerable<OsobaEvent> MergedEvents(this Osoba osoba, Expression<Func<OsobaEvent, bool>> predicate)
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


        public static OsobaEvent AddDescription(this Osoba osoba, string text, string strana, string zdroj, string user,
            bool deletePrevious = false)
        {
            if (deletePrevious)
            {
                var oes = osoba.Events(m => m.Type == (int)OsobaEvent.Types.Specialni);
                foreach (var o in oes)
                {
                    OsobaEventRepo.Delete(o, user);
                }
            }

            OsobaEvent oe = new OsobaEvent(osoba.InternalId, "", text, OsobaEvent.Types.Specialni);
            oe.Organizace = ParseTools.NormalizaceStranaShortName(strana);
            oe.Zdroj = zdroj;
            return osoba.AddOrUpdateEvent(oe, user);
        }

        public static OsobaEvent AddFunkce(this Osoba osoba, string pozice, string strana, int rokOd, int? rokDo,
            string zdroj, string user)
        {
            OsobaEvent oe = new OsobaEvent(osoba.InternalId, string.Format("{0}", pozice), "",
                OsobaEvent.Types.PolitickaPracovni);
            oe.Organizace = ParseTools.NormalizaceStranaShortName(strana);
            oe.Zdroj = zdroj;
            oe.DatumOd = new DateTime(rokOd, 1, 1, 0, 0, 0, DateTimeKind.Local);
            oe.DatumDo = rokDo == null
                ? (DateTime?)null
                : new DateTime(rokDo.Value, 12, 31, 0, 0, 0, DateTimeKind.Local);
            return osoba.AddOrUpdateEvent(oe, user);
        }

        public static OsobaEvent AddClenStrany(this Osoba osoba, string strana, int rokOd, int? rokDo, string zdroj,
            string user)
        {
            OsobaEvent oe = new OsobaEvent(osoba.InternalId, string.Format("Člen strany {0}", strana), "",
                OsobaEvent.Types.Politicka);
            oe.Organizace = ParseTools.NormalizaceStranaShortName(strana);
            oe.AddInfo = "člen strany";
            oe.Zdroj = zdroj;
            oe.DatumOd = new DateTime(rokOd, 1, 1, 0, 0, 0, DateTimeKind.Local);
            oe.DatumDo = rokDo == null
                ? (DateTime?)null
                : new DateTime(rokDo.Value, 12, 31, 0, 0, 0, DateTimeKind.Local);
            return osoba.AddOrUpdateEvent(oe, user);
        }

        public static Sponzoring AddSponsoring(this Osoba osoba, Sponzoring sponzoring, string user)
        {
            sponzoring.OsobaIdDarce = osoba.InternalId;
            var result = SponzoringRepo.Create(sponzoring, user);
            return result;
        }

        public static string Description(this Osoba osoba, bool html, string template = "{0}",
            string itemTemplate = "{0}",
            string itemDelimeter = "<br/>")
        {
            return osoba.Description(html, m => true, int.MaxValue, template, itemTemplate, itemDelimeter);
        }

        public static string Description(this Osoba osoba, bool html, Expression<Func<OsobaEvent, bool>> predicate,
            int numOfRecords = int.MaxValue, string template = "{0}",
            string itemTemplate = "{0}", string itemDelimeter = "<br/>",
            bool withSponzoring = false)
        {
            var fixedOrder = new List<int>()
            {
                (int) OsobaEvent.Types.VolenaFunkce,
                (int) OsobaEvent.Types.PolitickaPracovni,
                (int) OsobaEvent.Types.Politicka,
                (int) OsobaEvent.Types.VerejnaSpravaJine,
                (int) OsobaEvent.Types.VerejnaSpravaPracovni,
                (int) OsobaEvent.Types.Osobni,
                (int) OsobaEvent.Types.Jine
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

            if (html)
            {
                return string.Format(template,
                    evs.Aggregate((f, s) => f + itemDelimeter + s)
                );
            }
            else
            {
                return string.Format(template,
                    evs.Aggregate((f, s) => f + itemDelimeter + s)
                );
            }
        }


        //tohle do repositories
        public static Osoba.Statistics.RegistrSmluv StatistikaRegistrSmluv(this Osoba osoba,
            Relation.AktualnostType minAktualnost, int? obor = null)
        {
            return OsobaStatistics.CachedStatistics(osoba, minAktualnost, obor);
        }

        static Devmasters.Cache.LocalMemory.Manager<InfoFact[], Osoba> _cacheInfoFacts = 
            Devmasters.Cache.LocalMemory.Manager<InfoFact[], Osoba>
            .GetSafeInstance("Osoba_InfoFacts_v1_",
                (obj) => InfoFactsAsync(obj).GetAwaiter().GetResult(),
                TimeSpan.FromHours(12),
                obj => $"_infofacts_{obj.NameId}");

        public static async Task<InfoFact[]> InfoFactsCachedAsync(this Osoba osoba)
        {
            var _infof = _cacheInfoFacts.Get(osoba);
            return _infof;
        }
        
        public static async Task<InfoFact[]> InfoFactsAsync(this Osoba osoba,
            HashSet<InfoFact.ImportanceLevel> excludedImportanceLevels = null)
        {
            int[] types =
            {
                (int) OsobaEvent.Types.VolenaFunkce,
                (int) OsobaEvent.Types.PolitickaPracovni,
                (int) OsobaEvent.Types.Politicka,
                (int) OsobaEvent.Types.VerejnaSpravaJine,
                (int) OsobaEvent.Types.VerejnaSpravaPracovni,
                (int) OsobaEvent.Types.Osobni,
                (int) OsobaEvent.Types.Jine
            };

            List<InfoFact> f = new List<InfoFact>();
            var stat = osoba.StatistikaRegistrSmluv(Relation.AktualnostType.Nedavny);
            StatisticsPerYear<Smlouva.Statistics.Data> soukrStat = stat.SoukromeFirmy.Values
                    .AggregateStats(); //StatisticsSubjectPerYear<Smlouva.Statistics.Data>.

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
                if (await osoba.NotInterestingToShowAsync())
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
                    if (stat.NeziskovkyCount() > 0 && stat.KomercniFirmyCount() == 0)
                    {
                        statDesc +=
                            $"{Plural.Get(stat.NeziskovkyCount(), "jedné neziskové organizaci", "{0} neziskových organizacích", "{0} neziskových organizacích")}";
                    }
                    else if (stat.NeziskovkyCount() > 0)
                    {
                        statDesc +=
                            $"{Plural.Get(stat.NeziskovkyCount(), "jedné neziskové organizaci", "{0} neziskových organizacích", "{0} neziskových organizacích")}";
                        statDesc +=
                            $" a {Plural.Get(stat.KomercniFirmyCount(), "jedné soukr.firmě", "{0} soukr.firmách", "{0} soukr.firmách")}";
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
                !excludedImportanceLevels.Contains(InfoFact.ImportanceLevel.Medium))
            {
                DateTime datumOd = new DateTime(DateTime.Now.Year - 10, 1, 1);
                var sponzoring = osoba.Sponzoring(s => s.IcoPrijemce != null && s.DarovanoDne >= datumOd).ToList();
                if (sponzoring != null && sponzoring.Count() > 0)
                {
                    string[] strany = sponzoring.Select(m => m.IcoPrijemce).Distinct().ToArray();
                    int[] roky = sponzoring.Select(m => m.DarovanoDne.Value.Year).Distinct().OrderBy(y => y).ToArray();
                    decimal celkem = sponzoring.Sum(m => m.Hodnota) ?? 0;
                    decimal top = sponzoring.Max(m => m.Hodnota) ?? 0;
                    string
                        prvniStrana =
                            FirmaRepo.FromIco(strany[0])
                                .Jmeno; //todo: přidat tabulku politických stran a změnit zde na název strany

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

        public static string SocialInfoBody(this Osoba osoba)
        {
            return "<ul>"
                   + InfoFact.RenderInfoFacts(osoba.InfoFactsCachedAsync().ConfigureAwait(false).GetAwaiter().GetResult(),
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
                        var index = OsobaRepo.Searching.PolitikImportanceOrder.IndexOf(o.Status);
                        return index == -1 ? int.MaxValue : index;
                    }
                )
                //podle posledni politicke funkce
                .ThenByDescending(o =>
                    o.Events(e => OsobaRepo.Searching.PolitikImportanceEventTypes.Contains(e.Type)).Max(e => e.DatumOd))
                //podle poctu event
                .ThenByDescending(o => o.Events_VerejnopravniUdalosti().Count());
        }

        public static IEnumerable<Sponzoring> Sponzoring(this Osoba osoba, Expression<Func<Sponzoring, bool>> predicate)
        {
            return SponzoringRepo.GetByDarce(osoba.InternalId, predicate);
        }
    }
}