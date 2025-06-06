using Devmasters;
using Devmasters.Collections;
using HlidacStatu.Connectors;
using HlidacStatu.DS.Graphs;
using HlidacStatu.Entities;
using HlidacStatu.Entities.Facts;
using HlidacStatu.Entities.KIndex;
using HlidacStatu.Entities.Views;
using HlidacStatu.Lib.Data.External.RPP;
using HlidacStatu.Repositories;
using HlidacStatu.Repositories.Analysis.KorupcniRiziko;
using HlidacStatu.Repositories.Statistics;
using HlidacStatu.Util;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace HlidacStatu.Extensions
{
    public static class FirmaExtension
    {

        public static async Task<PuPlatStat> GetPlatyStatAsync(this Firma firma, int rok = PuRepo.DefaultYear)
        {
            var org = await PuRepo.GetFullDetailAsync(firma.DatovaSchranka);
            if (org == null)
                return null;
            var platyStat = new PuPlatStat(org.Platy);

            return platyStat;
        }

        public static async Task<string> GetPlatyUrlAsync(this Firma firma)
        {
            var org = await PuRepo.GetFullDetailAsync(firma.DatovaSchranka);
            if (org == null)
                return null;
            return org.GetUrl(false);
        }


        public static IEnumerable<SocialContact> GetSocialContacts(this Firma firma)
        {
            return firma.Events(oe => oe.Type == (int)OsobaEvent.Types.SocialniSite)
                .Select(oe => new SocialContact
                {
                    Network = Enum.TryParse<OsobaEvent.SocialNetwork>(oe.Organizace, true, out var socialNetwork)
                        ? socialNetwork
                        : (OsobaEvent.SocialNetwork?)null,
                    NetworkText = oe.Organizace,
                    Contact = oe.AddInfo
                });
        }

        public static async Task<bool> MaVztahySeStatemAsync(this Firma firma)
        {
            return firma.IsSponzor()
                   || firma.StatistikaRegistruSmluv().Sum(s => s.PocetSmluv) > 0
                   || (await VerejnaZakazkaRepo.Searching.SimpleSearchAsync("ico:" + firma.ICO, null, 1, 1, "0")).Total > 0
                   || (await DotaceRepo.Searching.SimpleSearchAsync("ico:" + firma.ICO, 1, 1, "0")).Total > 0;
        }

        public static bool MaVazbyNaPolitikyPred(this Firma firma, DateTime date)
        {
            if (firma.MaVazbyNaPolitiky())
            {
                var osoby = firma.VazbyNaPolitiky();
                foreach (var o in osoby)
                {
                    var found = o.Sponzoring().Any(m => m.DarovanoDne < date);
                    if (found)
                        return true;
                }
            }

            return false;
        }

        public static async Task<List<NapojenaOsoba>> NapojeneOsobyMinisterstvaAsync(this Firma firma)
        {
            await using var db = new DbEntities();

            var eventTypes = new[]
            {
                (int)OsobaEvent.Types.PolitickaExekutivni
            };

            var r = from oe in db.OsobaEvent
                    join os in db.Osoba on oe.OsobaId equals os.InternalId
                    where eventTypes.Any(et => et == oe.Type)
                    where oe.Ico == firma.ICO
                    orderby oe.DatumOd descending, oe.DatumDo descending
                    select new NapojenaOsoba()
                    {
                        Osoba = os,
                        OsobaEvent = oe
                    };


            return await r.ToListAsync();

        }

        static string[] vyjimky_v_RS_ = new string[] { "00006572", "63839407", "48136000", "48513687", "49370227", "70836981", "05553539" }; //§ 3 odst. 2 f) ... Poslanecká sněmovna, Senát, Kancelář prezidenta republiky, Ústavní soud, Nejvyšší kontrolní úřad, Kancelář veřejného ochránce práv a Úřad Národní rozpočtové rady
        public static async Task<bool> MusiPublikovatDoRSAsync(this Firma firma)
        {
            if (vyjimky_v_RS_.Contains(firma.ICO))
                return false;
            bool musi = firma.JsemOVM() || firma.JsemStatniFirma();

            if (firma.JsemOVM()) //Obec co neni v kategorii Obce s rozšířenou působností
            {
                var ovmCat = await firma.KategorieOVMAsync();
                if (ovmCat.Any(m => m.id == 14) == true && //je obec
                        ovmCat.Any(m => m.id == 11) == false //neni v kategorii Obce s rozšířenou působností
                   )
                {
                    musi = false;
                }
            }
            else if (firma.JsemStatniFirma())
            {
                var parentOVM = firma.ParentFirmy(Relation.AktualnostType.Aktualni)
                    .ToArray();

                musi = parentOVM
                    .Any(m => m.JsemOVM() && m.KategorieOVMAsync().ConfigureAwait(false).GetAwaiter().GetResult().Any(k => k.id == 11));
            }
            return musi;
        }



        public static async Task<KategorieOVM[]> KategorieOVMAsync(this Firma firma)
        {
            Lib.Data.External.RPP.KategorieOVM[] _kategorieOVM = null;

            var client = Manager.GetESClient_RPP_Kategorie();
            var res = await client.SearchAsync<Lib.Data.External.RPP.KategorieOVM>(
                s => s
                    .Query(q => q.QueryString(qs => qs.Query($"oVM_v_kategorii.kodOvm:{firma.ICO}")))
                    .Source(so => so.Excludes(ex => ex.Field("oVM_v_kategorii")))
                    .Size(150)
            );

            if (res.IsValid)
                _kategorieOVM = res.Hits
                    .Select(m => m.Source)
                    .OrderByDescending(m => m.hlidac_preferred ? 1 : 0)
                    .ThenBy(m => m.nazev)
                    .ToArray();
            else
                _kategorieOVM = new Lib.Data.External.RPP.KategorieOVM[] { };


            return _kategorieOVM;
        }

        public static Lib.Analytics.StatisticsSubjectPerYear<Smlouva.Statistics.Data> StatistikaRegistruSmluv(
            this Firma firma, int? iclassif = null, bool forceUpdateCache = false)
        {
            Lib.Analytics.StatisticsSubjectPerYear<Smlouva.Statistics.Data> ret = null;
            ret = FirmaStatistics.GetStatistics(firma, iclassif, forceUpdateCache);
            return ret ?? new Lib.Analytics.StatisticsSubjectPerYear<Smlouva.Statistics.Data>();
        }

        public static Lib.Analytics.StatisticsSubjectPerYear<Firma.Statistics.Dotace> StatistikaDotaci(
            this Firma firma, bool forceUpdateCache = false)
        {
            return FirmaStatistics.CachedStatisticsDotace(firma, forceUpdateCache) ??
                new Lib.Analytics.StatisticsSubjectPerYear<Firma.Statistics.Dotace>() { ICO = firma.ICO };

        }
        public static Lib.Analytics.StatisticsSubjectPerYear<Firma.Statistics.Dotace> HoldingStatistikaDotaci(
            this Firma firma,
            Relation.AktualnostType aktualnost, bool forceUpdateCache = false)
        {
            //STAT FIX
            //return new();

            return FirmaStatistics.CachedHoldingStatisticsDotace(firma, aktualnost, forceUpdateCache) ??
                new Lib.Analytics.StatisticsSubjectPerYear<Firma.Statistics.Dotace>() { ICO = firma.ICO }; ;
        }

        public static Lib.Analytics.StatisticsSubjectPerYear<Firma.Statistics.VZ> StatistikaVerejneZakazky(
            this Firma firma, bool forceUpdateCache = false)
        {
            //STAT FIX
            //return new ();

            return FirmaStatistics.CachedStatisticsVZ(firma, forceUpdateCache) ??
                new Lib.Analytics.StatisticsSubjectPerYear<Firma.Statistics.VZ>() { ICO = firma.ICO };
        }
        public static Lib.Analytics.StatisticsSubjectPerYear<Firma.Statistics.VZ> HoldingStatistikaVerejneZakazky(
            this Firma firma,
            Relation.AktualnostType aktualnost, bool forceUpdateCache = false)
        {
            //STAT FIX
            //return new();

            return FirmaStatistics.CachedHoldingStatisticsVZ(firma, aktualnost, forceUpdateCache) ??
                new Lib.Analytics.StatisticsSubjectPerYear<Firma.Statistics.VZ>() { ICO = firma.ICO };
        }

        public static Task<KIndexData> KindexAsync(this Firma firma, bool useTemp = false)
        {
            return KIndex.GetAsync(firma.ICO, useTemp);
        }

        public static bool MaVazbyNaPolitiky(this Firma firma)
        {
            return StaticData.FirmySVazbamiNaPolitiky_nedavne_Cache.Get().SoukromeFirmy.ContainsKey(firma.ICO);
        }

        public static Osoba[] VazbyNaPolitiky(this Firma firma)
        {
            return
                StaticData.FirmySVazbamiNaPolitiky_nedavne_Cache.Get()
                    .SoukromeFirmy[firma.ICO]
                    .Select(pid => OsobaRepo.PolitickyAktivni.Get().Where(m => m.InternalId == pid).FirstOrDefault())
                    .Where(p => p != null)
                    .OrderBy(p => p.Prijmeni)
                    .ToArray();
        }

        public static bool IsNespolehlivyPlatceDPH(this Firma firma)
        {
            return firma.NespolehlivyPlatceDPH() != null;
        }

        public static NespolehlivyPlatceDPH NespolehlivyPlatceDPH(this Firma firma)
        {
            if (StaticData.NespolehlivyPlatciDPH.Get().ContainsKey(firma.ICO))
                return StaticData.NespolehlivyPlatciDPH.Get()[firma.ICO];
            else
                return null;
        }

        public static async Task<bool> NotInterestingToShowAsync(this Firma firma)
        {
            return (await firma.MaVztahySeStatemAsync() == false)
                   && (firma.IsNespolehlivyPlatceDPH() == false)
                   && (firma.MaVazbyNaPolitiky() == false);
        }



        public static string SocialInfoTitle(this Firma firma)
        {
            return TextUtil.ShortenText(firma.Jmeno, 50);
        }

        public static string SocialInfoSubTitle(this Firma firma)
        {
            return firma.JsemOVM()
                ? "Úřad"
                : (firma.JsemStatniFirma() ? "Firma (spolu)vlastněná státem" : "Soukromá firma");
        }

        public static string SocialInfoBody(this Firma firma)
        {
            return "<ul>" +
                   firma.InfoFacts().RenderFacts(4, true, true, "", "<li>{0}</li>", true)
                   + "</ul>";
        }

        public static string SocialInfoFooter(this Firma firma)
        {
            return "Údaje k " + DateTime.Now.ToString("d. M. yyyy");
        }

        public static string SocialInfoImageUrl(this Firma firma)
        {
            return string.Empty;
        }

        public static (Osoba o, OsobaVazby ov)[] Osoby_v_OR(this Firma firma, Relation.AktualnostType aktualnost)
        {
            DateTime toDate = (DateTime)System.Data.SqlTypes.SqlDateTime.MinValue;
            if (aktualnost == Relation.AktualnostType.Aktualni)
                toDate = DateTime.Now.Date;
            else if (aktualnost == Relation.AktualnostType.Nedavny)
                toDate = DateTime.Now.Date.Add(-1 * Relation.NedavnyVztahDelka);

            using (DbEntities db = new DbEntities())
            {
                var osv = db.OsobaVazby.AsQueryable()
                    .Where(m => m.VazbakIco == firma.ICO)
                    .Where(m => m.DatumDo == null || m.DatumDo >= toDate);

                var res = db.Osoba
                    .AsQueryable()
                    .Join(osv, o => o.InternalId, ov => ov.OsobaId, (o, ov) => new Tuple<Osoba, OsobaVazby>(o, ov))
                    .Select<Tuple<Osoba, OsobaVazby>, (Osoba o, OsobaVazby ov)>(m => new(m.Item1, m.Item2));
                //var res = from os in db.Person
                //          join ov in db.OsobaVazby on new { id = os.InternalId }  equals new { id = ov.OsobaId }
                //          select new(os, ov);
                //var sql = res.ToQueryString();

                return res.ToArray();
            }
        }
        public static async Task<(string jmeno, string prijmeni, DateTime? poslednizmena)[]> CeosFromRPPAsync(this Firma firma)
        {
            List<(string jmeno, string prijmeni, DateTime? poslednizmena)> osoby = new List<(string jmeno, string prijmeni, DateTime? poslednizmena)>();

            if (firma.Valid == false || string.IsNullOrEmpty(firma.ICO))
                return osoby.ToArray();

            var client = Manager.GetESClient_RPP_OVM();
            var rppReq = await client.GetAsync<HlidacStatu.Lib.Data.External.RPP.OVMFull>(firma.ICO);
            if (rppReq.Found && rppReq.Source.angazovaneOsoby?.Count() > 0)
            {
                var rppOs = rppReq.Source.angazovaneOsoby;
                foreach (var os in rppOs)
                {
                    if (os?.fyzickaOsoba != null)
                    {
                        osoby.Add(new(os.fyzickaOsoba.jmeno, os.fyzickaOsoba.prijmeni, os.fyzickaOsoba.datumPosledniZmeny));
                    }
                }

            }
            return osoby.ToArray();

        }
        public static async Task<OVMFull.Osoba[]> CeosFromRPP_FullAsync(this Firma firma)
        {
            List<(string jmeno, string prijmeni, DateTime? poslednizmena)> osoby = new List<(string jmeno, string prijmeni, DateTime? poslednizmena)>();
            var client = Manager.GetESClient_RPP_OVM();
            var rppReq = await client.GetAsync<HlidacStatu.Lib.Data.External.RPP.OVMFull>(firma.ICO);
            if (rppReq.Found && rppReq.Source.angazovaneOsoby?.Count() > 0)
            {
                Lib.Data.External.RPP.OVMFull.Osoba[] rppOs = rppReq.Source.angazovaneOsoby;
                return rppReq.Source.angazovaneOsoby ?? new HlidacStatu.Lib.Data.External.RPP.OVMFull.Osoba[] { };

            }
            return new HlidacStatu.Lib.Data.External.RPP.OVMFull.Osoba[] { };

        }


        [Obsolete("Use OsobaEventRepo.GetCeos or FirmaExtension.Ceos and work it from there. This one might be faulty")]
        public static (Osoba Osoba, DateTime? From, string Role) Ceo(this Firma firma)
        {
            using (DbEntities db = new DbEntities())
            {
                var ceoEvent = db.OsobaEvent.AsQueryable()
                    .Where(oe => oe.Ceo == 1 && oe.Ico == firma.ICO)
                    .Where(oe => oe.DatumDo == null || oe.DatumDo >= DateTime.Now)
                    .Where(oe => oe.DatumOd != null && oe.DatumOd <= DateTime.Now)
                    .OrderByDescending(oe => oe.DatumOd)
                    .FirstOrDefault();

                if (ceoEvent is null)
                    return (null, null, null);

                var lastCeo = OsobaRepo.GetByInternalId(ceoEvent.OsobaId);
                if (lastCeo is null || !lastCeo.IsValid())
                    return (null, null, null);

                return (lastCeo, ceoEvent.DatumOd, ceoEvent.AddInfo);
            }
        }
        public static (Osoba Osoba, DateTime? From, DateTime? To, string Role)[] Ceos(this Firma firma, DateTime? fromDate, DateTime? toDate)
        {
            return OsobaEventRepo.GetCeos(firma.ICO, fromDate, toDate);
        }

        public static IEnumerable<string> IcosInHolding(this Firma firma, Relation.AktualnostType aktualnost)
        {
            return firma.AktualniVazby(aktualnost)
                .Where(v => !string.IsNullOrEmpty(v.To?.UniqId)
                            && v.To.Type == HlidacStatu.DS.Graphs.Graph.Node.NodeType.Company)
                .Select(v => v.To)
                .Distinct(new HlidacStatu.DS.Graphs.Graph.NodeComparer())
                .Select(m => m.Id);
        }

        /// <summary>
        /// Vrací firmy z holdingu ! KROMĚ mateřské firmy!
        /// </summary>
        /// <param name="aktualnost"></param>
        /// <returns></returns>
        public static IEnumerable<Firma> Holding(this Firma firma, Relation.AktualnostType aktualnost)
        {
            var icos = firma.IcosInHolding(aktualnost);

            return icos.Select(ico => Firmy.Get(ico));
        }


        public static Lib.Analytics.StatisticsSubjectPerYear<Smlouva.Statistics.Data> HoldingStatisticsRegistrSmluv(
            this Firma firma,
            Relation.AktualnostType aktualnost, int? obor = null, bool forceUpdateCache = false)
        {

            var ret = FirmaStatistics.CachedHoldingStatisticsSmlouvy(firma, aktualnost, obor, forceUpdateCache) ??
                new Lib.Analytics.StatisticsSubjectPerYear<Smlouva.Statistics.Data>() { ICO = firma.ICO };

            return ret;
        }

        public static bool PatrimStatuAlespon25procent(this Firma firma) => (firma.TypSubjektu == Firma.TypSubjektuEnum.PatrimStatuAlespon25perc);

        internal static bool _patrimStatuAlespon25procent(this Firma firma)
        {
            if (firma.JsemOVM())
                return true;

            if (
                (firma.Kod_PF != null && Firma.StatniFirmy_BasedKodPF.Contains(firma.Kod_PF.Value))
                || (FirmaRepo.VsechnyStatniMestskeFirmy25percs.Contains(firma.ICO))
            )
            {
                return true;
            }
            else
                return false;
        }
        public static string Kod_PF_Popis(this Firma firma)
        {
            var kod_pf = (firma.Kod_PF?.ToString() ?? "");
            using (DbEntities db = new DbEntities())
            {
                return db.PravniFormaOvm
                    .AsNoTracking()
                    .Where(zs => zs.Id == kod_pf)
                    .FirstOrDefault()?
                    .Text;
            }

        }

        public static bool JsemSoukromaFirma(this Firma firma)
        {
            return firma.JsemOVM() == false && firma.JsemStatniFirma() == false;
        }

        static int[] Neziskovky_KOD_PF = new int[] { 116, 117, 118, 141, 161, 422, 423, 671, 701, 706, 736 };

        public static bool JsemNeziskovka(this Firma firma)
        {
            if (firma.JsemSoukromaFirma() == false)
                return false;
            else if (firma.Kod_PF.HasValue == false)
                return false;
            else
            {
                return Neziskovky_KOD_PF.Contains(firma.Kod_PF.Value);
            }
        }


        public static async Task SetTypAsync(this Firma firma)
        {
            bool obec = false;
            if (firma._jsemOVM()) //Obec co neni v kategorii Obce s rozšířenou působností
            {
                if ((await firma.KategorieOVMAsync()).Any(m => m.id == 14) == true //je obec
                   )
                {
                    obec = true;
                }
            }

            if (obec)
                firma.TypSubjektu = Firma.TypSubjektuEnum.Obec;
            else if (firma._jsemOVM())
                firma.TypSubjektu = Firma.TypSubjektuEnum.Ovm;
            else if (firma._patrimStatuAlespon25procent())
            {
                firma.TypSubjektu = Firma.TypSubjektuEnum.PatrimStatuAlespon25perc;
                if (firma._jsemStatniFirma())
                    firma.TypSubjektu = Firma.TypSubjektuEnum.PatrimStatu;
            }
            else if (firma._jsemStatniFirma())
                firma.TypSubjektu = Firma.TypSubjektuEnum.PatrimStatu;
            else
                firma.TypSubjektu = Firma.TypSubjektuEnum.Soukromy;
        }

        public static bool PatrimStatu(this Firma firma)
        {
            return firma.JsemOVM() || firma.JsemStatniFirma();
        }

        /// <summary>
        /// Orgán veřejné moci
        /// </summary>
        /// <returns></returns>
        public static bool JsemOVM(this Firma firma) => firma.TypSubjektu == Firma.TypSubjektuEnum.Ovm || firma.TypSubjektu == Firma.TypSubjektuEnum.Obec;
        public const int PolitickaStrana_kodPF = 711;
        public static bool JsemPolitickaStrana(this Firma firma) => firma.Kod_PF == PolitickaStrana_kodPF;

        internal static bool _jsemOVM(this Firma firma)
        {
            return FirmaRepo.Urady_OVM.Contains(firma.ICO);
        }

        public static bool JsemStatniFirma(this Firma firma) => firma.TypSubjektu == Firma.TypSubjektuEnum.PatrimStatu || firma.TypSubjektu == Firma.TypSubjektuEnum.PatrimStatuAlespon25perc;
        internal static bool _jsemStatniFirma(this Firma firma)
        {
            if (
                (firma.Kod_PF != null && Firma.StatniFirmy_BasedKodPF.Contains(firma.Kod_PF.Value))
                || (FirmaRepo.VsechnyStatniMestskeFirmy.Contains(firma.ICO))
            )
            {
                return true;
            }
            else
                return false;
        }


        public static bool IsSponzor(this Firma firma)
        {
            return firma.Sponzoring().Any();
        }

        public static IEnumerable<OsobaEvent> Events(this Firma firma)
        {
            return firma.Events(m => true);
        }

        public static IEnumerable<Sponzoring> Sponzoring(this Firma firma)
        {
            return firma.Sponzoring(m => true);
        }

        public static string SponzoringToHtml(this Firma firma, int take = int.MaxValue)
        {
            return string.Join("<br />",
                firma.Sponzoring()
                    .OrderByDescending(s => s.DarovanoDne)
                    .Select(s => s.ToHtml())
                    .Take(take));
        }

        public static IEnumerable<OsobaEvent> Events(this Firma firma, Expression<Func<OsobaEvent, bool>> predicate)
        {
            using (DbEntities db = new DbEntities())
            {
                return db.OsobaEvent
                    .AsNoTracking()
                    .Where(predicate)
                    .Where(m => m.Ico == firma.ICO)
                    .ToArray();
            }
        }

        public static IEnumerable<Sponzoring> Sponzoring(this Firma firma, Expression<Func<Sponzoring, bool>> predicate)
        {
            return SponzoringRepo.GetByDarce(firma.ICO, predicate);
        }

        public static Sponzoring AddSponsoring(this Firma firma, Sponzoring sponzoring, string user)
        {
            sponzoring.IcoDarce = firma.ICO;
            var result = SponzoringRepo.Create(sponzoring, user);
            return result;
        }

        public static string Description(this Firma firma, bool html, Expression<Func<OsobaEvent, bool>> predicate,
            string template = "{0}", string itemTemplate = "{0}",
            string itemDelimeter = "<br/>", int numOfRecords = int.MaxValue)
        {
            var events = firma.Events(predicate);
            if (events.Count() == 0)
                return string.Empty;
            else
            {
                List<string> evs = events
                    .OrderBy(e => e.DatumOd)
                    .Select(e => html ? e.RenderHtml(", ") : e.RenderText(", "))
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .Take(numOfRecords)
                    .ToList();

                if (html && evs.Count > 0)
                {
                    return string.Format(template, string.Join(itemDelimeter, evs));
                }
                else if (evs.Count > 0)
                {
                    return string.Format(template,
                        string.Join("\n", evs)
                    );
                }
                else return string.Empty;
            }
        }


        const string uradName = "Úřad";
        const string statniName = "Státní firma";
        const string firmaName = "Firma";

        public static string ObecneJmeno(this Firma firma)
        {
            return firma.JsemOVM() ? uradName : (firma.JsemStatniFirma() ? statniName : firmaName);
        }

        public static bool IsSponzorBefore(this Firma firma, DateTime date)
        {
            if (firma.JsemOVM())
                return false;
            if (firma.JsemStatniFirma())
                return false;
            return StaticData.SponzorujiciFirmy_Vsechny
                .Get() //todo: info je z cache
                .Where(m => m.IcoDarce == firma.ICO && m.DarovanoDne < date)
                .Any();
        }

        static Devmasters.Cache.Redis.Manager<InfoFact[], Firma> _infoFactsCache()
        {
            var cache = Devmasters.Cache.Redis.Manager<InfoFact[], Firma>
                .GetSafeInstance("Firma_InfoFacts",
                    (firma) => GetDirectInfoFactsAsync(firma).ConfigureAwait(false).GetAwaiter().GetResult(),
                    TimeSpan.FromDays(2),
                    Devmasters.Config.GetWebConfigValue("RedisServerUrls").Split(';'),
                    Devmasters.Config.GetWebConfigValue("RedisBucketName"),
                    Devmasters.Config.GetWebConfigValue("RedisUsername"),
                    Devmasters.Config.GetWebConfigValue("RedisCachePassword"),
                    keyValueSelector: f => f.ICO);

            return cache;
        }


        static Devmasters.Cache.Redis.Manager<Riziko[], (Firma f, int rok)> _rizikoCache()
        {
            var cache = Devmasters.Cache.Redis.Manager<Riziko[], (Firma f, int rok)>
                .GetSafeInstance("Firma_Rizika",
                    (o) => getDirectRiziko(o.f, o.rok),
                    TimeSpan.FromDays(2),
                    Devmasters.Config.GetWebConfigValue("RedisServerUrls").Split(';'),
                    Devmasters.Config.GetWebConfigValue("RedisBucketName"),
                    Devmasters.Config.GetWebConfigValue("RedisUsername"),
                    Devmasters.Config.GetWebConfigValue("RedisCachePassword"),
                    keyValueSelector: o => $"{o.rok}-{o.f.ICO}");

            return cache;
        }

        private static Riziko[] getDirectRiziko(Firma firma, int rok)
        {
            List<Riziko> res = new List<Riziko>();
            var statistics = firma.StatistikaRegistruSmluv().StatisticsForYear(rok);
            var kindex = firma.KindexAsync().ConfigureAwait(false).GetAwaiter().GetResult()?.ForYear(rok);


            if (statistics.PercentSmluvBezCeny > 0)
            {
                string query = System.Net.WebUtility.UrlEncode($"ico:{firma.ICO} AND ( hint.skrytaCena:1 ) AND datumUzavreni:[{rok}-01-01 TO {rok + 1}-01-01}}"); //
                string html = @$"
                    <p>
                        <i class=""fas fa-exclamation-circle"" style=""color:{HlidacStatu.Entities.Analysis.Riziko.RizikoColor(KIndexData.DetailInfo.KIndexLabelForPart(KIndexData.KIndexParts.PercentBezCeny, statistics.PercentSmluvBezCeny).AsRiziko())};padding-right:20px;""></i>
                        <b>Počet uzavřených smluv bez uvedené ceny</b>
                        <a href=""/HledatSmlouvy?Q={query}"">
                            <i class='fas fa-link'></i>
                        </a>
                        <br/>
                        <span style=""padding-left:40px;"">
                            {HlidacStatu.Entities.Analysis.Riziko.ToHtml(
                                    KIndexData.DetailInfo.KIndexLabelForPart(KIndexData.KIndexParts.PercentBezCeny, statistics.PercentSmluvBezCeny, statistics.PocetSmluv).AsRiziko()
                                    )},
                            celkem
                            <a href=""/HledatSmlouvy?Q={query}"">
                                <b>{HlidacStatu.Util.RenderData.Vysledky.PocetSmluv(statistics.PocetSmluvBezCeny, HlidacStatu.Util.RenderData.CapitalizationStyle.FirstLetterUpperCap)}</b>
                            </a>
                            bez uvedené ceny.
                            <span>Tj. {HlidacStatu.Util.RenderData.NicePercent(statistics.PercentSmluvBezCeny)} z celkového počtu.</span>
                        </span>
                    </p>";
                string text = $"celkem {HlidacStatu.Util.RenderData.NicePercent(statistics.PercentSmluvBezCeny)} smluv bez uvedené ceny,"
                    + $" {HlidacStatu.Entities.Analysis.Riziko.RizikoText(
                    KIndexData.DetailInfo.KIndexLabelForPart(KIndexData.KIndexParts.PercentBezCeny, statistics.PercentSmluvBezCeny, statistics.PocetSmluv).AsRiziko()
                    )} riziko";
                res.Add(new Riziko(html, text, Fact.ImportanceLevel.Medium ));
            }

            if(statistics.PocetSmluvULimitu > 0)
            {
                string query = System.Net.WebUtility.UrlEncode($"ico:{firma.ICO} AND ( hint.smlouvaULimitu:>0 ) AND datumUzavreni:[{rok}-01-01 TO {rok + 1}-01-01}}"); 
                                                                                                                                                                             
                string html = @$"
                    <p>
                        <i class=""fas fa-exclamation-circle"" style=""color:{Entities.Analysis.Riziko.RizikoColor(KIndexData.DetailInfo.KIndexLabelForPart(KIndexData.KIndexParts.PercSmluvUlimitu, statistics.PercentSmluvULimitu).AsRiziko())};padding-right:20px;""></i> <b>Smlouvy s cenou těsně pod limitem pro veřejné zakázky</b>
                        <a href=""/HledatSmlouvy?Q={query}"">
                            <i class='fas fa-link'></i>
                        </a>
                        <br/>
                        <span style=""padding-left:40px;"">
                            {
                                HlidacStatu.Entities.Analysis.Riziko.ToHtml(
                                    KIndexData.DetailInfo.KIndexLabelForPart(KIndexData.KIndexParts.PercSmluvUlimitu, statistics.PercentSmluvULimitu, statistics.PocetSmluv).AsRiziko()
                                    )
                                },
                            celkem
                            <a href=""/HledatSmlouvy?Q={query}"">
                                <b>{HlidacStatu.Util.RenderData.Vysledky.PocetSmluv(statistics.PocetSmluvULimitu, HlidacStatu.Util.RenderData.CapitalizationStyle.FirstLetterUpperCap)}</b>
                            </a>
                            s cenou těsně pod limitem pro veřejné zakázky.
                            <span>Tj. {HlidacStatu.Util.RenderData.NicePercent(statistics.PercentSmluvULimitu)} z celkového počtu.</span>
                        </span>
                    </p>
                ";
                string text = $"celkem {HlidacStatu.Util.RenderData.NicePercent(statistics.PercentSmluvULimitu)} smluv bez uvedené ceny,"
                    + $" {HlidacStatu.Entities.Analysis.Riziko.RizikoText(
                    KIndexData.DetailInfo.KIndexLabelForPart(KIndexData.KIndexParts.PercSmluvUlimitu, statistics.PercentSmluvULimitu, statistics.PocetSmluv).AsRiziko()
                    )} riziko";
                res.Add(new Riziko(html, text, Fact.ImportanceLevel.Medium));
            }

            if (statistics.PocetSmluvSeZasadnimNedostatkem > 0)
            {
                string query = System.Net.WebUtility.UrlEncode($"ico:{firma.ICO} AND ( chyby:zasadni ) AND datumUzavreni:[{rok}-01-01 TO {rok + 1}-01-01}}");

                string html = @$"
                    <p>
                        <i class=""fas fa-exclamation-circle"" style=""color:{Entities.Analysis.Riziko.RizikoColor(KIndexData.DetailInfo.KIndexLabelForPart(KIndexData.KIndexParts.PercSeZasadnimNedostatkem, statistics.PercentSmluvSeZasadnimNedostatkem).AsRiziko())};padding-right:20px;""></i> <b>Smlouvy se zásadními nedostatky</b>
                        <a href=""/HledatSmlouvy?Q={query}"">
                            <i class='fas fa-link'></i>
                        </a>
                        <br/>
                        <span style=""padding-left:40px;"">
                            @{Entities.Analysis.Riziko.ToHtml(
                                    KIndexData.DetailInfo.KIndexLabelForPart(KIndexData.KIndexParts.PercSmluvUlimitu, statistics.PercentSmluvULimitu, statistics.PocetSmluv).AsRiziko()
                                    )
                                },
                            celkem
                            <a href=""/HledatSmlouvy?Q={query}"">
                                <b>{HlidacStatu.Util.RenderData.Vysledky.PocetSmluv(statistics.PocetSmluvSeZasadnimNedostatkem, HlidacStatu.Util.RenderData.CapitalizationStyle.FirstLetterUpperCap)}</b>
                            </a>
                            bez uvedené ceny.
                            <span>Tj. {HlidacStatu.Util.RenderData.NicePercent(statistics.PercentSmluvSeZasadnimNedostatkem)} z celkového počtu.</span>
                        </span>
                    </p>

                ";
                string text = $"celkem {HlidacStatu.Util.RenderData.NicePercent(statistics.PercentSmluvSeZasadnimNedostatkem)} smluv se zasadním nedostatkem,"
                    + $" {HlidacStatu.Entities.Analysis.Riziko.RizikoText(
                    KIndexData.DetailInfo.KIndexLabelForPart(KIndexData.KIndexParts.PercSeZasadnimNedostatkem, statistics.PercentSmluvSeZasadnimNedostatkem, statistics.PocetSmluv).AsRiziko()
                    )} riziko";
                res.Add(new Riziko(html, text, Fact.ImportanceLevel.Medium));
            }
                if (kindex != null && kindex?.CelkovaKoncentraceDodavatelu != null)
                {
                    var value = kindex?.CelkovaKoncentraceDodavatelu.Herfindahl_Hirschman_Modified;
                    var lbl = KIndexData.DetailInfo.KIndexLabelForPart(KIndexData.KIndexParts.CelkovaKoncentraceDodavatelu, value).AsRiziko();
                    var lblText = "";
                    switch (lbl)
                    {
                        case Entities.Analysis.Riziko.RizikoValues.A:
                            lblText = "smlouvy se nekoncentrují u u žádných smluvních partnerů.";
                            break;
                        case Entities.Analysis.Riziko.RizikoValues.B:
                        case Entities.Analysis.Riziko.RizikoValues.C:
                            lblText = "žádný smluvní partner nedominuje nad ostatními.";
                            break;
                        case Entities.Analysis.Riziko.RizikoValues.D:
                        case Entities.Analysis.Riziko.RizikoValues.E:
                            lblText = "smlouvy se koncentrují u malého počtu partnerů.";
                            break;
                        case Entities.Analysis.Riziko.RizikoValues.F:
                            lblText = $"většina smluv dle počtu či objemu je uzavřena s {Devmasters.Lang.CS.Plural.Get(kindex.CelkovaKoncentraceDodavatelu.TopDodavatele().Count(), "jedním smluvním partnerem;s {0} smluvními partnery;s {0} smluvními partnery")}.";
                            break;
                        default:
                            lblText = "";
                            break;
                    }

                string html = @$"
                    <p>
                        <i class=""fas fa-exclamation-circle"" style=""color:{Entities.Analysis.Riziko.RizikoColor(lbl)};padding-right:20px;""></i> <b>Koncentrace smluvních partnerů</b>
                        <a href=""/kindex/detail/{firma.ICO}#detail_CelkovaKoncentraceDodavatelu"">
                            <i class='fas fa-link'></i>
                        </a>
                        <br/>
                        <span style=""padding-left:40px;"">
                            {Entities.Analysis.Riziko.ToHtml(lbl)},
                            {lblText}
                        </span>
                    </p>";
                string text = $"{lblText} smluv se zasadním nedostatkem,"
                    + $" {HlidacStatu.Entities.Analysis.Riziko.RizikoText(lbl)} riziko";
                res.Add(new Riziko(html, text, Fact.ImportanceLevel.Medium));
            }
            return res.ToArray();
        }

        public static void InfoFactsInvalidate(this Firma firma) //otázka jestli tohle nebrat z cachce
        {
            _infoFactsCache().Delete(firma);

        }


        public static Riziko[] Rizika(this Firma firma, int rok, bool forceUpdateCache = false) //otázka jestli tohle nebrat z cachce
        {
            //STAT FIX
            //return Array.Empty<InfoFact>();
            if (forceUpdateCache)
                _rizikoCache().Delete((firma,rok));
            var inf = _rizikoCache().Get((firma, rok));
            return inf;
        }

        public static InfoFact[] InfoFacts(this Firma firma, bool forceUpdateCache = false) //otázka jestli tohle nebrat z cachce
        {
            //STAT FIX
            //return Array.Empty<InfoFact>();

            if (forceUpdateCache)
                firma.InfoFactsInvalidate();
            var inf = _infoFactsCache().Get(firma);
            return inf;
        }
        public static async Task<InfoFact[]> GetDirectInfoFactsAsync(Firma firma)
        {
            var sName = firma.ObecneJmeno();
            bool sMuzsky = sName == uradName;

            List<InfoFact> f = new List<InfoFact>();
            //var stat = new HlidacStatu.Lib.Analysis.SubjectStatistic(this);
            var stat = firma.StatistikaRegistruSmluv();
            var statHolding = firma.HoldingStatisticsRegistrSmluv(Relation.AktualnostType.Aktualni);
            int rok = DateTime.Now.Year;
            if (DateTime.Now.Month < 3)
                rok = rok - 1;

            if (stat.Sum(stat.YearsAfter2016(), s => s.PocetSmluv) == 0)
            {
                f.Add(new InfoFact($"{sName} nemá žádné smluvní vztahy evidované s&nbsp;veřejnou správou. ",
                    InfoFact.ImportanceLevel.Medium));
                f.Add(new InfoFact(
                    $"{(sMuzsky ? "Byl založen" : "Byla založena")} <b>{firma.Datum_Zapisu_OR?.ToString("d. M. yyyy")}</b>. ",
                    InfoFact.ImportanceLevel.Medium));
            }
            else
            {
                if (stat[rok].PocetSmluv < statHolding[rok].PocetSmluv)
                {
                    f.Add(new InfoFact($"V roce <b>{rok}</b> uzavřel{(sMuzsky ? "" : "a")} {sName.ToLower()} " +
                                       Devmasters.Lang.CS.Plural.Get(stat[rok].PocetSmluv,
                                           "jednu smlouvu",
                                           "{0} smlouvy",
                                           "celkem {0} smluv")
                                       + $" za <b>{RenderData.ShortNicePrice(stat[rok].CelkovaHodnotaSmluv, html: true)}</b>, "
                                       + "celý holding "
                                       + Devmasters.Lang.CS.Plural.Get(statHolding[rok].PocetSmluv,
                                           "jednu smlouvu",
                                           "{0} smlouvy",
                                           "celkem {0} smluv")
                                       + $" za <b>{RenderData.ShortNicePrice(statHolding[rok].CelkovaHodnotaSmluv, html: true)}</b>, "

                        , InfoFact.ImportanceLevel.Summary)
                    );
                }
                else
                    f.Add(new InfoFact($"V roce <b>{rok}</b> uzavřel{(sMuzsky ? "" : "a")} {sName.ToLower()} " +
                                       Devmasters.Lang.CS.Plural.Get(stat[rok].PocetSmluv,
                                           "jednu smlouvu s&nbsp;veřejnou správou",
                                           "{0} smlouvy s&nbsp;veřejnou správou",
                                           "celkem {0} smluv s&nbsp;veřejnou správou")
                                       + $" za <b>{RenderData.ShortNicePrice(stat[rok].CelkovaHodnotaSmluv, html: true)}</b>. "
                        , InfoFact.ImportanceLevel.Summary)
                    );

                (decimal zmena, decimal? procentniZmena) =
                    stat.ChangeBetweenYears(rok - 1, rok, s => s.CelkovaHodnotaSmluv);
                if (procentniZmena.HasValue)
                {
                    string text = $"Mezi lety <b>{rok - 1}-{rok - 2000}</b> ";
                    switch (zmena)
                    {
                        case decimal n when n > 0:
                            text += $"došlo k <b>nárůstu státních zakázek o&nbsp;{procentniZmena:P2}</b> v&nbsp;Kč. ";
                            break;
                        case decimal n when n < 0:
                            text += $"došlo k <b>poklesu státních zakázek o&nbsp;{procentniZmena:P2}</b> v&nbsp;Kč . ";
                            break;
                        default:
                            text += " nedošlo ke změně objemu státních zakázek. ";
                            break;
                    }

                    f.Add(new InfoFact(text, InfoFact.ImportanceLevel.Medium));
                }

                if (stat[rok].PocetSmluvBezCeny > 0)
                {
                    f.Add(new InfoFact(
                        $"V <b>{rok} neuvedl{(sMuzsky ? "" : "a")}</b> hodnotu smlouvy " +
                        Devmasters.Lang.CS.Plural.Get(stat[rok].PocetSmluvBezCeny, "u&nbsp;jedné smlouvy",
                            "u&nbsp;{0} smluv", "u&nbsp;{0} smluv")
                        + $", což je celkem <b>{stat[rok].PercentSmluvBezCeny.ToString("P2")}</b> ze všech. ",
                        InfoFact.ImportanceLevel.Medium)
                    );
                }
                else if (stat[rok - 1].PocetSmluvBezCeny > 0)
                {
                    f.Add(new InfoFact(
                        $"V <b>{rok - 1} neuvedl{(sMuzsky ? "" : "a")}</b> hodnotu smlouvy " +
                        Devmasters.Lang.CS.Plural.Get(stat[rok - 1].PocetSmluvBezCeny, "u&nbsp;jedné smlouvy",
                            "u&nbsp;{0} smluv", "u&nbsp;{0} smluv")
                        + $", což je celkem <b>{stat[rok - 1].PercentSmluvBezCeny.ToString("P2")}</b> ze všech. "
                        , InfoFact.ImportanceLevel.Medium)
                    );
                }

                long numFatalIssue = (await SmlouvaRepo.Searching.SimpleSearchAsync($"ico:{firma.ICO} AND chyby:zasadni", 0, 0,
                    SmlouvaRepo.Searching.OrderResult.FastestForScroll, exactNumOfResults: true)).Total;
                long numVazneIssue = (await SmlouvaRepo.Searching.SimpleSearchAsync($"ico:{firma.ICO} AND chyby:vazne", 0, 0,
                    SmlouvaRepo.Searching.OrderResult.FastestForScroll, exactNumOfResults: true)).Total;

                if (numFatalIssue > 0)
                {
                    f.Add(new InfoFact($@"Má v registru smluv
                                    <b>{Devmasters.Lang.CS.Plural.GetWithZero((int)numFatalIssue, "0 smluv", "jednu smlouvu obsahující", "{0} smlouvy obsahující", "{0:### ### ##0} smluv obsahujících ")}
                                        tak závažné nedostatky v rozporu se zákonem,
                                    </b>že jsou velmi pravděpodobně neplatné. ", InfoFact.ImportanceLevel.High));
                }

                if (numVazneIssue > 0)
                {
                    f.Add(new InfoFact($@"Má v registru smluv
                                    <b>{Devmasters.Lang.CS.Plural.GetWithZero((int)numFatalIssue, "0 smluv", "jednu smlouvu obsahující", "{0} smlouvy obsahující", "{0:### ### ##0} smluv obsahujících ")}</b>
                                        vážné nedostatky. "
                        , InfoFact.ImportanceLevel.Medium)
                    );
                }

                if (firma.PatrimStatu() == false)
                {
                    DateTime datumOd = new DateTime(DateTime.Now.Year - 10, 1, 1);
                    var sponzoring =
                        firma.Sponzoring(s =>
                            s.IcoPrijemce != null && s.DarovanoDne >= datumOd); // sponzoring pol. stran
                    if (sponzoring != null && sponzoring.Count() > 0)
                    {
                        string[] strany = sponzoring.Select(m => m.IcoPrijemce).Distinct().ToArray();
                        int[] roky = sponzoring.Select(m => m.DarovanoDne.Value.Year).Distinct().OrderBy(y => y)
                            .ToArray();
                        decimal celkem = sponzoring.Sum(m => m.Hodnota) ?? 0;
                        decimal top = sponzoring.Max(m => m.Hodnota) ?? 0;
                        string prvniStrana =
                            FirmaRepo.FromIco(strany[0])
                                .Jmeno; //todo: přidat tabulku politických stran a změnit zde na název strany

                        f.Add(new InfoFact($"{sName} "
                                           + Devmasters.Lang.CS.Plural.Get(roky.Count(), "v roce " + roky[0],
                                               $"mezi roky {roky.First()}-{roky.Last() - 2000}",
                                               $"mezi roky {roky.First()}-{roky.Last() - 2000}")
                                           + $" sponzoroval{(sMuzsky ? "" : "a")} " +
                                           Devmasters.Lang.CS.Plural.Get(strany.Length, prvniStrana,
                                               "{0} polit.strany", "{0} polit.stran")
                                           + $" v&nbsp;celkové výši <b>{RenderData.ShortNicePrice(celkem, html: true)}</b>. "
                                           + $"Nejvyšší sponzorský dar byl ve výši {RenderData.ShortNicePrice(top, html: true)}. "
                            , InfoFact.ImportanceLevel.Medium)
                        );
                    }
                }

                if (//TODO rozlisit prime a neprime angazovani
                    firma.PatrimStatu() == false
                    && StaticData.FirmySVazbamiNaPolitiky_nedavne_Cache.Get().SoukromeFirmy.ContainsKey(firma.ICO)
                )
                {
                    var politici = StaticData.FirmySVazbamiNaPolitiky_nedavne_Cache.Get().SoukromeFirmy[firma.ICO];
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

                        f.Add(new InfoFact(
                            $"Ve firmě se "
                            + Devmasters.Lang.CS.Plural.Get(politici.Count()
                                , " angažuje jedna politicky angažovaná osoba - "
                                , " angažují {0} politicky angažované osoby - "
                                , " angažuje {0} politicky angažovaných osob - ")
                            + sPolitici + ". "
                            , InfoFact.ImportanceLevel.Medium)
                        );
                    }
                }

                if (firma.PatrimStatu() && stat[rok].PocetSmluvSponzorujiciFirmy > 0)
                {
                    f.Add(new InfoFact($"V <b>{rok}</b> uzavřel{(sMuzsky ? "" : "a")} {sName.ToLower()} " +
                                       Devmasters.Lang.CS.Plural.Get(stat[rok].PocetSmluvSponzorujiciFirmy,
                                           "jednu smlouvu; {0} smlouvy;{0} smluv")
                                       + $" s firmama s vazbou na politiky za celkem <b>{RenderData.ShortNicePrice(stat[rok].SumKcSmluvSponzorujiciFirmy, html: true)}</b> "
                                       + $" (tj. {stat[rok].PercentKcSmluvPolitiky.ToString("P2")}). "
                        , InfoFact.ImportanceLevel.Medium)
                    );
                }
                else if (firma.PatrimStatu() && stat[rok - 1].PocetSmluvSponzorujiciFirmy > 0)
                {
                    f.Add(new InfoFact($"V <b>{rok - 1}</b> uzavřel{(sMuzsky ? "" : "a")} {sName.ToLower()} " +
                                       Devmasters.Lang.CS.Plural.Get(stat[rok - 1].PocetSmluvSponzorujiciFirmy,
                                           "jednu smlouvu; {0} smlouvy;{0} smluv")
                                       + $" s firmama s vazbou na politiky za celkem <b>{RenderData.ShortNicePrice(stat[rok - 1].SumKcSmluvSponzorujiciFirmy, html: true)}</b> "
                                       + $" (tj. {stat[rok].PercentKcSmluvPolitiky.ToString("P2")}). "
                        , InfoFact.ImportanceLevel.Medium)
                    );
                }

                f.Add(new InfoFact($"Od roku <b>2016</b> uzavřel{(sMuzsky ? "" : "a")} {sName.ToLower()} " +
                                   Devmasters.Lang.CS.Plural.Get(
                                       stat.Sum(stat.YearsAfter2016(), s => s.PocetSmluv),
                                       "jednu smlouvu v&nbsp;registru smluv",
                                       "{0} smlouvy v&nbsp;registru smluv",
                                       "celkem {0} smluv v&nbsp;registru smluv")
                                   + $" za <b>{RenderData.ShortNicePrice(stat.Sum(stat.YearsAfter2016(), s => s.CelkovaHodnotaSmluv), html: true)}</b>. "
                    , InfoFact.ImportanceLevel.Low)
                );
            }

            if (firma.PatrimStatu() == false && firma.IcosInHolding(Relation.AktualnostType.Aktualni).Count() > 2)
            {
                if (statHolding[rok].PocetSmluv > 3)
                {
                    f.Add(new InfoFact($"V roce <b>{rok}</b> uzavřel celý holding " +
                                       Devmasters.Lang.CS.Plural.Get(statHolding[rok].PocetSmluv,
                                           "jednu smlouvu v&nbsp;registru smluv",
                                           "{0} smlouvy v&nbsp;registru smluv",
                                           "celkem {0} smluv v&nbspregistru smluv")
                                       + $" za <b>{RenderData.ShortNicePrice(statHolding[rok].CelkovaHodnotaSmluv, html: true)}</b>. "
                        , InfoFact.ImportanceLevel.Low)
                    );

                    string text = $"Mezi lety <b>{rok - 1}-{rok - 2000}</b> ";
                    (decimal zmena, decimal? procentniZmena) =
                        statHolding.ChangeBetweenYears(rok - 1, rok, s => s.CelkovaHodnotaSmluv);

                    if (procentniZmena.HasValue)
                    {
                        switch (zmena)
                        {
                            case decimal n when n > 0:
                                text +=
                                    $"celému holdingu narostla hodnota smluv o&nbsp;<b>{procentniZmena:P2}</b>. ";
                                break;
                            case decimal n when n < 0:
                                text +=
                                    $"celému holdingu poklesla hodnota smluv o&nbsp;<b>{procentniZmena:P2}</b>. ";
                                break;
                            default:
                                text += "nedošlo pro celý holding ke změně hodnoty smluv. ";
                                break;
                        }

                        f.Add(new InfoFact(text, InfoFact.ImportanceLevel.Low));
                    }
                }
            }

            var infofacts = f.OrderByDescending(o => o.Level).ToArray();

            return infofacts;
        }



    }
}