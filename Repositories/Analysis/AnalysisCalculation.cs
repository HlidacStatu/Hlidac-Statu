using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Devmasters.Batch;
using Devmasters.Enums;
using HlidacStatu.Caching;
using HlidacStatu.DS.Graphs;
using HlidacStatu.Entities;
using HlidacStatu.Entities.Analysis;
using HlidacStatu.Extensions;
using HlidacStatu.Lib.Analytics;
using HlidacStatu.Repositories.Cache;
using Nest;
using Serilog;
using ZiggyCreatures.Caching.Fusion;
using Manager = HlidacStatu.Connectors.Manager;

namespace HlidacStatu.Repositories.Analysis
{
    public class AnalysisCalculation
    {
        private static readonly ILogger _logger = Log.ForContext(typeof(AnalysisCalculation));

        private static IFusionCache PermanentCache =>
            HlidacStatu.Caching.CacheFactory.CreateNew(CacheFactory.CacheType.PermanentStore,
                nameof(AnalysisCalculation));

        public class VazbyFiremNaPolitiky
        {
            public Dictionary<string, List<int>> SoukromeFirmy { get; set; } = new Dictionary<string, List<int>>();
            public Dictionary<string, List<int>> StatniFirmy { get; set; } = new Dictionary<string, List<int>>();
        }

        public class VazbyFiremNaUradyStat
        {
            public IEnumerable<BasicDataForSubject<List<BasicData<string>>>> SoukromeFirmy { get; set; } =
                new List<BasicDataForSubject<List<BasicData<string>>>>();

            public IEnumerable<BasicDataForSubject<List<BasicData<string>>>> StatniFirmy { get; set; } =
                new List<BasicDataForSubject<List<BasicData<string>>>>();
        }

        public class IcoSmlouvaMinMax
        {
            public string ico { get; set; }
            public string jmeno { get; set; }
            DateTime? _minUzavreni = null;

            public DateTime? minUzavreni
            {
                get { return _minUzavreni; }
                set
                {
                    _minUzavreni = value;
                    setDays();
                }
            }

            public DateTime? maxUzavreni { get; set; }

            DateTime? _vznikIco = null;

            public DateTime? vznikIco
            {
                get { return _vznikIco; }
                set
                {
                    _vznikIco = value;
                    setDays();
                }
            }

            double? _days = null;

            public double? days
            {
                get { return _days; }
            }

            private void setDays()
            {
                if (minUzavreni.HasValue && vznikIco.HasValue)
                    _days = (minUzavreni.Value - vznikIco.Value).TotalDays;
                else
                    _days = null;
            }
        }


        private static async Task<List<Smlouva>> SimpleSmlouvyForIcoAsync(string ico, DateTime? from, DateTime? to)
        {
            Func<int, int, Task<ISearchResponse<Smlouva>>> searchFunc = async (size, page) =>
            {
                var sdate = "";
                if (from.HasValue)
                    sdate =
                        $" AND podepsano:[{from?.ToString("yyyy-MM-dd") ?? "*"} TO {from?.ToString("yyyy-MM-dd") ?? DateTime.Now.AddDays(1).ToString("yyyy-MM-dd")}]"; //podepsano:[2016-01-01 TO 2016-12-31]

                var client = Manager.GetESClient();
                var sq = await SmlouvaRepo.Searching.GetSimpleQueryAsync("ico:" + ico + sdate);
                return await client.SearchAsync<Smlouva>(a => a
                    .TrackTotalHits(page * size == 0)
                    .Size(size)
                    .From(page * size)
                    .Source(m => m.Excludes(e => e.Field(o => o.Prilohy)))
                    .Query(q => sq)
                    .Scroll("1m")
                );
            };

            List<Smlouva> smlouvy = new List<Smlouva>();
            await Repositories.Searching.Tools.DoActionForQueryAsync<Smlouva>(Manager.GetESClient(), searchFunc,
                (hit, o) =>
                {
                    smlouvy.Add(hit.Source);
                    return Task.FromResult(new ActionOutputData());
                }, null,
                null, null, false);


            return smlouvy;
        }

        public static async Task<Tuple<VazbyFiremNaUradyStat, VazbyFiremNaUradyStat>>
            UradyObchodujiciSFirmami_NespolehlivymiPlatciDPHAsync(bool showProgress = false)
        {
            var nespolehliveFirmy = StaticData.NespolehlivyPlatciDPH.Get();

            Dictionary<string, BasicDataForSubject<List<BasicData<string>>>> uradyData = new();

            Dictionary<string, BasicDataForSubject<List<BasicData<string>>>> nespolehliveFirmyKontrakty = new();

            var lockObj = new object();

            await Devmasters.Batch.Manager.DoActionForAllAsync<NespolehlivyPlatceDPH>(nespolehliveFirmy.Values,
                async (nf) =>
                {
                    var ico = nf.Ico;
                    var smlouvy = await SimpleSmlouvyForIcoAsync(ico, nf.FromDate, nf.ToDate);
                    foreach (var s in smlouvy)
                    {
                        var allIco = new List<string>(
                            s.Prijemce.Select(m => m.ico).Where(p => !string.IsNullOrEmpty(p))
                        );
                        allIco.Add(s.Platce.ico);
                        var urady = allIco.Select(i => Firmy.GetAsync(i)).Where(f => f.PatrimStatu());

                        foreach (var urad in urady)
                        {
                            lock (lockObj)
                            {
                                if (!uradyData.ContainsKey(urad.ICO))
                                {
                                    uradyData.Add(urad.ICO,
                                        new BasicDataForSubject<List<BasicData<string>>>() { Item = urad.ICO });
                                }

                                uradyData[urad.ICO].Add(1, s.CalculatedPriceWithVATinCZK);
                                if (!uradyData[urad.ICO].Detail.Any(m => m.Item == ico))
                                {
                                    uradyData[urad.ICO].Detail.Add(new BasicData<string>()
                                        { Item = ico, CelkemCena = s.CalculatedPriceWithVATinCZK, Pocet = 1 });
                                }
                                else
                                {
                                    var item = uradyData[urad.ICO].Detail.First(m => m.Item == ico);
                                    item.Pocet++;
                                    item.CelkemCena += s.CalculatedPriceWithVATinCZK;
                                }

                                //--------------
                                if (!nespolehliveFirmyKontrakty.ContainsKey(ico))
                                {
                                    nespolehliveFirmyKontrakty.Add(ico,
                                        new BasicDataForSubject<List<BasicData<string>>>());
                                    nespolehliveFirmyKontrakty[ico].Ico = ico;
                                }

                                nespolehliveFirmyKontrakty[ico].Add(1, s.CalculatedPriceWithVATinCZK);
                                if (!nespolehliveFirmyKontrakty[ico].Detail.Any(m => m.Item == urad.ICO))
                                {
                                    nespolehliveFirmyKontrakty[ico].Detail.Add(new BasicData<string>()
                                        { Item = urad.ICO, CelkemCena = s.CalculatedPriceWithVATinCZK, Pocet = 1 });
                                }
                                else
                                {
                                    var item = nespolehliveFirmyKontrakty[ico].Detail.First(m => m.Item == urad.ICO);
                                    item.Add(1, s.CalculatedPriceWithVATinCZK);
                                }
                            }
                        }
                    }

                    return new ActionOutputData();
                },
                showProgress ? Devmasters.Batch.Manager.DefaultOutputWriter : (Action<string>)null,
                showProgress ? new ActionProgressWriter() : (Devmasters.Batch.IProgressWriter )null,
                !System.Diagnostics.Debugger.IsAttached, maxDegreeOfParallelism: 5,
                prefix: "UradyObchodujiciSFirmami_NespolehlivymiPlatciDPH ", monitor: new MonitoredTaskRepo.ForBatch());

            VazbyFiremNaUradyStat ret = new VazbyFiremNaUradyStat();
            ret.StatniFirmy = uradyData
                .Where(m => m.Value.Pocet > 0)
                .Select(kv => kv.Value)
                .OrderByDescending(o => o.Pocet)
                .ToList();

            VazbyFiremNaUradyStat retNespolehliveFirmy = new VazbyFiremNaUradyStat();
            retNespolehliveFirmy.SoukromeFirmy = nespolehliveFirmyKontrakty
                .Where(m => m.Value.Pocet > 0)
                .Select(kv => kv.Value)
                .OrderByDescending(o => o.Pocet)
                .ToList();

            return new Tuple<VazbyFiremNaUradyStat, VazbyFiremNaUradyStat>(ret, retNespolehliveFirmy);
        }


        public static async Task<VazbyFiremNaUradyStat> UradyObchodujiciSFirmami_s_vazbouNaPolitikyAsync(
            Relation.AktualnostType aktualnost, bool showProgress = false)
        {
            VazbyFiremNaPolitiky vazbyNaPolitiky = null;
            List<Sponzoring> sponzorujiciFirmy = null;

            QueryContainer qc = null;

            switch (aktualnost)
            {
                case Relation.AktualnostType.Aktualni:
                    vazbyNaPolitiky = await MaterializedViewsCache.FirmySVazbamiNaPolitiky_AktualniAsync();
                    qc = new QueryContainerDescriptor<Smlouva>().Term(t =>
                        t.Field(f => f.SVazbouNaPolitikyAktualni).Value(true));
                    sponzorujiciFirmy = StaticData.SponzorujiciFirmy_Nedavne.Get();
                    break;
                case Relation.AktualnostType.Nedavny:
                    vazbyNaPolitiky = await MaterializedViewsCache.FirmySVazbamiNaPolitiky_NedavneAsync();
                    qc = new QueryContainerDescriptor<Smlouva>().Term(t =>
                        t.Field(f => f.SVazbouNaPolitikyNedavne).Value(true));
                    sponzorujiciFirmy = StaticData.SponzorujiciFirmy_Nedavne.Get();
                    break;
                case Relation.AktualnostType.Neaktualni:
                case Relation.AktualnostType.Libovolny:
                    vazbyNaPolitiky = await MaterializedViewsCache.FirmySVazbamiNaPolitiky_VsechnyAsync();
                    qc = new QueryContainerDescriptor<Smlouva>().Term(t =>
                        t.Field(f => f.SVazbouNaPolitiky).Value(true));
                    sponzorujiciFirmy = StaticData.SponzorujiciFirmy_Vsechny.Get();
                    break;
            }


            Func<int, int, Task<ISearchResponse<Smlouva>>> searchFunc = null;
            searchFunc = async (size, page) =>
            {
                var client = Manager.GetESClient();
                return await client.SearchAsync<Smlouva>(a => a
                    .TrackTotalHits(page * size == 0)
                    .Size(size)
                    .From(page * size)
                    .Source(m => m.Excludes(e => e.Field(o => o.Prilohy)))
                    .Query(q => qc)
                    .Scroll("1m")
                );
            };


            //TODO predelat z projeti vsech smluv na hledani pres vsechna ICO  v RS, vybrani statnich firem, 
            //a dohlednai jejich statistiky vuci jednotlivym ostatnim firmam v RS
            Dictionary<string, BasicDataForSubject<List<BasicData<string>>>> uradyStatni =
                new Dictionary<string, BasicDataForSubject<List<BasicData<string>>>>();
            Dictionary<string, BasicDataForSubject<List<BasicData<string>>>> uradySoukr =
                new Dictionary<string, BasicDataForSubject<List<BasicData<string>>>>();
            object lockObj = new object();
            await Repositories.Searching.Tools.DoActionForQueryAsync<Smlouva>(Manager.GetESClient(), searchFunc, async (hit, param) =>
                {
                    Smlouva s = hit.Source;
                    List<string> icos = new List<string>();
                    try
                    {
                        var objednatelIco = s.Platce.ico;
                        if (!string.IsNullOrEmpty(objednatelIco))
                        {
                            Firma ff = await Firmy.GetAsync(objednatelIco);
                            if (!ff.Valid || !ff.PatrimStatu())
                                goto end;

                            //vsichni prijemci smlouvy statniho subjektu
                            icos.AddRange(s.Prijemce.Select(m => m.ico).Where(m => !string.IsNullOrEmpty(m))
                                .Distinct());

                            lock (lockObj)
                            {
                                foreach (var ico in icos)
                                {
                                    if (vazbyNaPolitiky.SoukromeFirmy.ContainsKey(ico) ||
                                        sponzorujiciFirmy.Any(m => m.IcoDarce == ico))
                                    {
                                        if (!uradySoukr.ContainsKey(objednatelIco))
                                        {
                                            uradySoukr.Add(objednatelIco,
                                                new BasicDataForSubject<List<BasicData<string>>>());
                                            uradySoukr[objednatelIco].Ico = objednatelIco;
                                        }

                                        uradySoukr[objednatelIco].Add(1, s.CalculatedPriceWithVATinCZK);
                                        if (!uradySoukr[objednatelIco].Detail.Any(m => m.Item == ico))
                                        {
                                            uradySoukr[objednatelIco].Detail.Add(new BasicData<string>()
                                                { Item = ico, CelkemCena = s.CalculatedPriceWithVATinCZK, Pocet = 1 });
                                        }
                                        else
                                        {
                                            var item = uradySoukr[objednatelIco].Detail.First(m => m.Item == ico);
                                            item.Add(1, s.CalculatedPriceWithVATinCZK);
                                        }
                                    }
                                    else if (vazbyNaPolitiky.StatniFirmy.ContainsKey(ico))
                                    {
                                        if (!uradyStatni.ContainsKey(objednatelIco))
                                        {
                                            uradyStatni.Add(objednatelIco,
                                                new BasicDataForSubject<List<BasicData<string>>>());
                                            uradyStatni[objednatelIco].Ico = objednatelIco;
                                        }

                                        uradyStatni[objednatelIco].Add(1, s.CalculatedPriceWithVATinCZK);
                                        if (!uradyStatni[objednatelIco].Detail.Any(m => m.Item == ico))
                                        {
                                            uradyStatni[objednatelIco].Detail.Add(new BasicData<string>()
                                                { Item = ico, CelkemCena = s.CalculatedPriceWithVATinCZK, Pocet = 1 });
                                        }
                                        else
                                        {
                                            var item = uradyStatni[objednatelIco].Detail.First(m => m.Item == ico);
                                            item.Add(1, s.CalculatedPriceWithVATinCZK);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.Error(e, "ERROR UradyObchodujiciSFirmami_s_vazbouNaPolitiky");
                    }

                    end:
                    return new ActionOutputData() { CancelRunning = false, Log = null };
                }, null,
                showProgress ? Devmasters.Batch.Manager.DefaultOutputWriter : (Action<string>)null,
                showProgress ? new ActionProgressWriter() : (Devmasters.Batch.IProgressWriter )null
                , true
                , prefix: "UradyObchodujiciSFirmami_s_vazbouNaPolitiky " + aktualnost.ToNiceDisplayName()
                , monitor: new MonitoredTaskRepo.ForBatch()
            );


            VazbyFiremNaUradyStat ret = new VazbyFiremNaUradyStat();
            ret.SoukromeFirmy = uradySoukr
                .Where(m => m.Value.Pocet > 0)
                .Select(kv => kv.Value)
                .OrderByDescending(o => o.Pocet);

            return ret;
        }

        public static async Task<VazbyFiremNaPolitiky> LoadFirmySVazbamiNaPolitikyAsync(Relation.AktualnostType aktualnostVztahu,
            bool showProgress = false)
        {
            object psvLock = new();
            Dictionary<string, List<int>> pol_SVazbami = new Dictionary<string, List<int>>();
            object psvSfLock = new();
            Dictionary<string, List<int>> pol_SVazbami_StatniFirmy = new Dictionary<string, List<int>>();

            await Devmasters.Batch.Manager.DoActionForAllAsync<Osoba>(OsobaRepo.PolitickyAktivni.Get(), async (p) =>
                {
                    try
                    {
                        var vazbyVlastnictviTask = p.AktualniVazbyAsync(Relation.CharakterVazbyEnum.VlastnictviKontrola,
                            aktualnostVztahu);
                        var vazbyUredniTask =
                            p.AktualniVazbyAsync(Relation.CharakterVazbyEnum.Uredni, aktualnostVztahu);
                        
                        var vazby = (await vazbyVlastnictviTask).Union(await vazbyUredniTask);
                        if (vazby != null && vazby.Any())
                        {
                            foreach (var v in vazby)
                            {
                                if (!string.IsNullOrEmpty(v.To.Id))
                                {
                                    //check if it's GovType company
                                    Firma f = await Firmy.GetAsync(v.To.Id);

                                    if (f == null || f.Valid == false)
                                        continue; //unknown company, skip
                                    if (f.PatrimStatu())
                                    {
                                        lock (psvSfLock)
                                        {
                                            //Gov Company
                                            if (pol_SVazbami_StatniFirmy.TryGetValue(v.To.Id, out var pol))
                                            {
                                                if (pol.All(m => m != p.InternalId))
                                                    pol.Add(p.InternalId);
                                            }
                                            else
                                            {
                                                pol_SVazbami_StatniFirmy.Add(v.To.Id, new List<int>());
                                                pol_SVazbami_StatniFirmy[v.To.Id].Add(p.InternalId);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        lock (psvLock)
                                        {
                                            //private company
                                            if (pol_SVazbami.TryGetValue(v.To.Id, out var pol))
                                            {
                                                if (pol.All(m => m != p.InternalId))
                                                    pol.Add(p.InternalId);
                                            }
                                            else
                                            {
                                                pol_SVazbami.Add(v.To.Id, new List<int>());
                                                pol_SVazbami[v.To.Id].Add(p.InternalId);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.Error(e, "LoadFirmySVazbamiNaPolitiky error for {osoba}", p?.NameId);
                    }

                    return new ActionOutputData() { CancelRunning = false, Log = null };
                },
                showProgress ? Devmasters.Batch.Manager.DefaultOutputWriter : (Action<string>)null,
                showProgress ? new ActionProgressWriter() : (Devmasters.Batch.IProgressWriter )null,
                parallel: true
                , prefix: "LoadFirmySVazbamiNaPolitiky " + aktualnostVztahu.ToNiceDisplayName()
                , monitor: showProgress ? new MonitoredTaskRepo.ForBatch() : null,
                maxDegreeOfParallelism: 5
            );

            return new VazbyFiremNaPolitiky() { SoukromeFirmy = pol_SVazbami, StatniFirmy = pol_SVazbami_StatniFirmy };
        }


        public static async Task<string[]> SmlouvyIdSPolitikyAsync(Dictionary<string, List<int>> politicisVazbami,
            List<OsobaEvent> sponzorujiciFirmy, bool showProgress = false)
        {
            HashSet<string> allIco = new HashSet<string>(
                politicisVazbami.Select(m => m.Key).Union(sponzorujiciFirmy.Select(m => m.Ico)).Distinct()
            );
            //smlouvy s politikama
            Func<int, int, Task<ISearchResponse<Smlouva>>> searchFunc = async (size, page) =>
            {
                var client = Manager.GetESClient();
                return await client.SearchAsync<Smlouva>(a => a
                    .TrackTotalHits(page * size == 0)
                    .Size(size)
                    .From(page * size)
                    .Source(m => m.Excludes(e => e.Field(o => o.Prilohy)))
                    .Query(q => q.MatchAll())
                    .Scroll("1m")
                );
            };


            List<string> smlouvyIds = new List<string>();
            await Repositories.Searching.Tools.DoActionForQueryAsync<Smlouva>(Manager.GetESClient(), searchFunc,
                (hit, param) =>
                {
                    Smlouva s = hit.Source;
                    if (s.platnyZaznam)
                    {
                        if (!string.IsNullOrEmpty(s.Platce.ico) && allIco.Contains(s.Platce.ico))
                        {
                            smlouvyIds.Add(s.Id);
                        }
                        else
                            foreach (var ss in s.Prijemce)
                            {
                                if (!string.IsNullOrEmpty(ss.ico) && allIco.Contains(ss.ico))
                                {
                                    smlouvyIds.Add(s.Id);
                                    break;
                                }
                            }
                    }

                    return Task.FromResult(new ActionOutputData() { CancelRunning = false, Log = null });
                }, null,
                showProgress ? Devmasters.Batch.Manager.DefaultOutputWriter : (Action<string>)null,
                showProgress ? new ActionProgressWriter() : (Devmasters.Batch.IProgressWriter )null
                , false
                , prefix: "SmlouvyIdSPolitiky "
                , monitor: new MonitoredTaskRepo.ForBatch()
            );

            return smlouvyIds.ToArray();
        }

        public static async Task<IEnumerable<IcoSmlouvaMinMax>> GetFirmyCasovePodezreleZalozeneAsync(
            Action<string> logOutputFunc = null, IProgressWriter progressOutputFunc = null)
        {
            _logger.Debug("GetFirmyCasovePodezreleZalozene - getting all ico");
            var allIcos = await FirmaRepo.AllIcoInRSAsync();
            Dictionary<string, IcoSmlouvaMinMax> firmy = new Dictionary<string, IcoSmlouvaMinMax>();
            object lockFirmy = new object();

            AggregationContainerDescriptor<Smlouva> aggs = new AggregationContainerDescriptor<Smlouva>()
                .Min("minDate", m => m
                    .Field(f => f.datumUzavreni)
                );


            _logger.Debug("GetFirmyCasovePodezreleZalozene - getting first smlouva for all ico from ES");
            await Devmasters.Batch.Manager.DoActionForAllAsync<string, object>(allIcos, async (ico, param) =>
                {
                    Firma ff = await Firmy.GetAsync(ico);
                    if (ff?.Valid == true)
                    {
                        if (ff.PatrimStatu()) //statni firmy tam nechci
                        {
                            return new ActionOutputData() { CancelRunning = false, Log = null };
                        }
                        else
                        {
                            var res = await SmlouvaRepo.Searching.SimpleSearchAsync("ico:" + ico, 0, 0,
                                SmlouvaRepo.Searching.OrderResult.FastestForScroll, aggs, exactNumOfResults: true);
                            if (res.ElasticResults.Aggregations.Count > 0)
                            {
                                var epoch = ((ValueAggregate)res.ElasticResults.Aggregations.First().Value).Value;
                                if (epoch.HasValue)
                                {
                                    var mindate = Devmasters.DT.Util.FromEpochTimeToUTC((long)epoch / 1000);

                                    lock (lockFirmy)
                                    {
                                        if (firmy.ContainsKey(ico))
                                        {
                                            if (firmy[ico].minUzavreni.HasValue == false)
                                                firmy[ico].minUzavreni = mindate;
                                            else if (firmy[ico].minUzavreni.Value > mindate)
                                                firmy[ico].minUzavreni = mindate;
                                        }
                                        else
                                        {
                                            firmy.Add(ico, new IcoSmlouvaMinMax()
                                            {
                                                ico = ico,
                                                minUzavreni = Devmasters.DT.Util.FromEpochTimeToUTC((long)epoch / 1000)
                                            });
                                        }

                                        if (ff.Datum_Zapisu_OR.HasValue)
                                        {
                                            firmy[ico].vznikIco = ff.Datum_Zapisu_OR.Value;
                                            firmy[ico].jmeno = ff.Jmeno;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    return new ActionOutputData() { CancelRunning = false, Log = null };
                },
                null,
                logOutputFunc ?? Devmasters.Batch.Manager.DefaultOutputWriter,
                progressOutputFunc ?? new ActionProgressWriter(0.1f),
                true, prefix: "GetFirmyCasovePodezreleZalozene ", monitor: new MonitoredTaskRepo.ForBatch()
            );

            _logger.Debug("GetFirmyCasovePodezreleZalozene - filter with close dates");

            DateTime minDate = new DateTime(1990, 01, 01);
            var badF = firmy
                .Select(m => m.Value)
                .Where(f => f.minUzavreni > minDate)
                .Where(f => f.days.HasValue && f.days.Value < 60)
                .OrderBy(f => f.days.Value)
                .ToArray();
            //.Take(100)

            _logger.Debug($"GetFirmyCasovePodezreleZalozene - returning {badF.Count()} records.");

            return badF;
        }

        public static async IAsyncEnumerable<(string idDotace, string ico, int ageInYears)>
            CompanyAgeDuringDotaceAsync()
        {
            await foreach (var dotace in DotaceRepo.GetAllAsync(null))
            {
                bool missingEssentialData = string.IsNullOrWhiteSpace(dotace.Recipient.Ico)
                                            || !dotace.ApprovedYear.HasValue;

                if (missingEssentialData)
                    continue;

                Firma firma = await Firmy.GetAsync(dotace.Recipient.Ico);

                if (firma.PatrimStatu()) //nechceme státní firmy
                    continue;

                if (!firma.Datum_Zapisu_OR.HasValue) //nechceme firmy s chybějící hodnotou data zapisu do OR
                    continue;


                var companyAgeInDays = (dotace.ApprovedYear.Value - firma.Datum_Zapisu_OR.Value.Year);

                yield return (idDotace: dotace.Id, ico: firma.ICO, ageInYears: companyAgeInDays);
            }
        }

        public static ValueTask<HlidacStatu.Lib.Analytics.StatisticsSubjectPerYear<Smlouva.Statistics.Data>[]>
            GetAllStatisticsDataAsync() =>
            PermanentCache.GetOrSetAsync($"_AllStatisticsSmlouvyDataCacheFile", async _ =>
            {
                var allicos = await FirmaRepo.AllIcoInRSAsync();
                var getData =
                    new System.Collections.Concurrent.ConcurrentBag<
                        HlidacStatu.Lib.Analytics.StatisticsSubjectPerYear<Smlouva.Statistics.Data>>();
                await Devmasters.Batch.Manager.DoActionForAllAsync(allicos, async ico =>
                    {
                        var firma = await Firmy.GetAsync(ico);
                        if (firma != null)
                        {
                            var stat = await firma.StatistikaRegistruSmluvAsync();
                            if (stat.Summary().PocetSmluv > 10 || stat.Summary().CelkovaHodnotaSmluv > 999_999)
                                getData.Add(stat);
                        }
                        return new ActionOutputData();
                    },
                    Devmasters.Batch.Manager.DefaultOutputWriter,
                    new ActionProgressWriter(),
                    true, maxDegreeOfParallelism: 10,
                    prefix: "NarustySmluv getAll stats ",
                    monitor: new MonitoredTaskRepo.ForBatch()
                );
                return getData.ToArray();
            });
        

        public static ValueTask<CalculatedChangeBetweenYears<string>[]>
            GetNarustySmluvOdNulyTop100_AbsolutPrice_2020_24Async(int? obor) =>
            PermanentCache.GetOrSetAsync($"_NarustySmluvOdNulyTop100_AbsolutPrice_2020_24_{obor}",
                async _ => BetweenYearsCalculatedChanges(await GetAllStatisticsDataAsync(), obor, 2018, 2021));

        public static ValueTask<CalculatedChangeBetweenYears<string>[]>
            GetNarustySmluvOdNulyTop100_Percent_2020_24Async(int? obor) =>
            PermanentCache.GetOrSetAsync($"_NarustySmluvOdNulyTop100_Percent_2020_24_{obor}",
                async _ => BetweenYearsCalculatedChanges_Percent(await GetAllStatisticsDataAsync(), obor, 2018, 2021));

        public static ValueTask<CalculatedChangeBetweenYears<string>[]>
            GetNarustySmluvTop100_AbsolutPrice_Vlada2018Async(int? obor) =>
            PermanentCache.GetOrSetAsync($"_NarustySmluvTop100_AbsolutPrice_Vlada2018_{obor}",
                async _ => BetweenYearsCalculatedChanges(await GetAllStatisticsDataAsync(), obor, 2018, 2021));

        public static ValueTask<CalculatedChangeBetweenYears<string>[]>
            GetNarustySmluvTop100_Percent_Vlada2018Async(int? obor) =>
            PermanentCache.GetOrSetAsync($"_NarustySmluvTop100_Percent_Vlada2018_{obor}", async _ =>
                BetweenYearsCalculatedChanges_Percent(await GetAllStatisticsDataAsync(), obor, 2018, 2021)
            );
     
        public static ValueTask<CalculatedChangeBetweenYears<string>[]>
            GetNarustySmluvTop100_AbsolutPrice_Vlada2022Async(int? obor) =>
            PermanentCache.GetOrSetAsync($"_NarustySmluvTop100_AbsolutPrice_Vlada2022_{obor}", async _ =>
                BetweenYearsCalculatedChanges(await GetAllStatisticsDataAsync(), obor, 2021, 2025)
            );

        public static ValueTask<CalculatedChangeBetweenYears<string>[]>
            GetNarustySmluvTop100_Percent_Vlada2022Async(int? obor) =>
            PermanentCache.GetOrSetAsync($"_NarustySmluvTop100_Percent_Vlada2022_{obor}", async _ =>
                BetweenYearsCalculatedChanges_Percent(await GetAllStatisticsDataAsync(), obor, 2022, 2025)
            );
        
        public static ValueTask<CalculatedChangeBetweenYears<string>[]>
            GetNarustySmluvTop100_AbsolutPrice_2020_24Async(int? obor) =>
            PermanentCache.GetOrSetAsync($"_NarustySmluvTop100_AbsolutPrice_2020_24_{obor}", async _ =>
                BetweenYearsCalculatedChanges(await GetAllStatisticsDataAsync(), obor, 2020, 2024)
            );

        public static ValueTask<CalculatedChangeBetweenYears<string>[]>
            GetNarustySmluvTop100_Percent_2020_24Async(int? obor) =>
            PermanentCache.GetOrSetAsync($"_NarustySmluvTop100_Percent_2020_24_{obor}", async _ =>
                BetweenYearsCalculatedChanges_Percent(await GetAllStatisticsDataAsync(), obor, 2020, 2024)
            );

        public static ValueTask<CalculatedChangeBetweenYears<string>[]>
            GetNarustySmluvTop100_AbsolutPrice_MaxInYearAsync(int? obor) =>
            PermanentCache.GetOrSetAsync($"_NarustySmluvTop100_AbsolutPrice_MaxInYear_{obor}", async _ =>
            {
                List<CalculatedChangeBetweenYears<string>> data = new();
                for (int i = 2018; i <= DateTime.Now.Year - 1; i++)
                    data.AddRange(BetweenYearsCalculatedChanges(await GetAllStatisticsDataAsync(), obor, i, i));
                return data
                    .OrderByDescending(o => o.change.ValueChange)
                    .Take(100)
                    .ToArray();
            });

        public static ValueTask<CalculatedChangeBetweenYears<string>[]>
            GetNarustySmluvTop100_Percent_MaxInYearAsync(int? obor) =>
            PermanentCache.GetOrSetAsync($"_NarustySmluvTop100_Percent_MaxInYear_{obor}", async _ =>
            {
                List<CalculatedChangeBetweenYears<string>> data = new();
                for (int i = 2018; i <= DateTime.Now.Year - 1; i++)
                    data.AddRange(BetweenYearsCalculatedChanges_Percent(await GetAllStatisticsDataAsync(), obor, i, i));
                return data
                    .OrderByDescending(o => (o.change.PercentChange ?? int.MaxValue))
                    .ThenByDescending(o => o.change.ValueChange)
                    .Take(100)
                    .ToArray();
            });
        

        public static CalculatedChangeBetweenYears<string>[] BetweenYearsCalculatedChanges(
            StatisticsSubjectPerYear<Smlouva.Statistics.Data>[] allData,
            int? obor,
            int yStart,
            int yEnd,
            decimal minChange = 1_000_000,
            decimal? minStartValue = null //2_000_000
        )
        {
            int yPreStart = yStart - (yEnd - yStart) - 1; //start predchoziho obdobi

            CalculatedChangeBetweenYears<string>[] res = null;

            var _items = allData
                .Select(m => new
                {
                    item = m,
                    change = obor.HasValue
                        ? m.ChangeBetweenIntervals(yStart, yEnd, m => m.PoOblastechHierarchicky(obor.Value)?.CelkemCena ?? 0m)
                        : m.ChangeBetweenIntervals(yStart, yEnd, m => m.CelkovaHodnotaSmluv)
                });
            if (minStartValue.HasValue)
            {
                if (obor.HasValue)
                    _items = _items.Where(m =>
                        m.item.Summary(Enumerable.Range(yPreStart, yStart - yPreStart).ToArray())
                            .PoOblastechHierarchicky(obor.Value).CelkemCena <= minStartValue.Value
                    );
                else
                    _items = _items.Where(m =>
                        m.item.Summary(Enumerable.Range(yPreStart, yStart - yPreStart).ToArray()).CelkovaHodnotaSmluv <=
                        minStartValue.Value
                    );
            }

            res = _items
                    .Select(m => new CalculatedChangeBetweenYears<string>()
                    {
                        data = m.item.ICO,
                        change = m.change,
                        firstYear = (yStart == yEnd) ? yStart - 1 : yStart,
                        lastYear = yEnd,
                    })
                    .Where(m => m.change.ValueChange > 999_999)
                    //.OrderByDescending(o => (o.percChange ?? int.MaxValue))
                    .OrderByDescending(o => o.change.ValueChange)
                    .Take(1000)
                    .Where(m => Firmy.GetAsync(m.data).JsemSoukromaFirma())
                    .Take(100)
                    .ToArray()
                ;


            return res;
        }


        public static CalculatedChangeBetweenYears<string>[] BetweenYearsCalculatedChanges_Percent(
            StatisticsSubjectPerYear<Smlouva.Statistics.Data>[] allData,
            int? obor,
            int yStart,
            int yEnd,
            decimal minChange = 1_000_000,
            decimal? minStartValue = null //2_000_000
        )
        {
            int yPreStart = yStart - (yEnd - yStart) - 1; //start predchoziho obdobi
            CalculatedChangeBetweenYears<string>[] res = null;
            var _items = allData
                .Select(m => new
                {
                    item = m,
                    change = obor.HasValue
                        ? m.ChangeBetweenIntervals(yStart, yEnd, m => m.PoOblastechHierarchicky(obor.Value).CelkemCena)
                        : m.ChangeBetweenIntervals(yStart, yEnd, m => m.CelkovaHodnotaSmluv)
                });
            if (minStartValue.HasValue)
                _items = _items.Where(m =>
                    m.item.Summary(Enumerable.Range(yPreStart, yStart - yPreStart).ToArray()).CelkovaHodnotaSmluv <=
                    minStartValue.Value
                );

            res = _items
                    .Where(m => m.change.ValueChange > 10_000_000)
                    .OrderByDescending(o => (o.change.PercentChange ?? int.MaxValue))
                    .ThenByDescending(o => o.change.ValueChange)
                    .Select(m => new CalculatedChangeBetweenYears<string>()
                    {
                        data = m.item.ICO,
                        change = m.change,
                        firstYear = yStart,
                        lastYear = yEnd,
                    })
                    .Take(1000)
                    .Where(m => Firmy.GetAsync(m.data).JsemSoukromaFirma())
                    .Take(100)
                    .ToArray()
                ;


            return res;
        }
    }
}