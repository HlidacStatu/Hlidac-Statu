using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using HlidacStatu.Caching;
using HlidacStatu.DS.Graphs;
using HlidacStatu.Entities;
using HlidacStatu.Entities.OrgStrukturyStatu;
using HlidacStatu.Lib.Analytics;
using HlidacStatu.Repositories.Analysis;
using Serilog;
using ZiggyCreatures.Caching.Fusion;

namespace HlidacStatu.Repositories.Cache;

public class MaterializedViewsCache
{
    
    private static readonly ILogger _logger = Log.ForContext(typeof(MaterializedViewsCache));
    
    private static IFusionCache PermanentCache =>
        HlidacStatu.Caching.CacheFactory.CreateNew(CacheFactory.CacheType.PermanentStore,
            nameof(MaterializedViewsCache));

    //caches without factory
    public static ValueTask<AnalysisCalculation.VazbyFiremNaUradyStat> NespolehlivyPlatciDPH_ObchodySUradyAsync(
        AnalysisCalculation.VazbyFiremNaUradyStat data = null)
        => PermanentCache.LoadOrSetDataFromPermanentCacheAsync<AnalysisCalculation.VazbyFiremNaUradyStat>(
            "NespolehlivyPlatciDPH_ObchodySUrady",
            options => options.ModifyEntryOptionsDuration(TimeSpan.FromHours(12), TimeSpan.FromDays(10 * 365)),
            data
        );

    public static ValueTask<AnalysisCalculation.VazbyFiremNaUradyStat> UradyObchodujiciSNespolehlivymiPlatciDPHAsync(
        AnalysisCalculation.VazbyFiremNaUradyStat data = null)
        => PermanentCache.LoadOrSetDataFromPermanentCacheAsync<AnalysisCalculation.VazbyFiremNaUradyStat>(
            "UradyObchodujiciSNespolehlivymiPlatciDPH",
            options => options.ModifyEntryOptionsDuration(TimeSpan.FromHours(12), TimeSpan.FromDays(10 * 365)),
            data
        );

    public static ValueTask<AnalysisCalculation.VazbyFiremNaPolitiky> FirmySVazbamiNaPolitiky_AktualniAsync(
        AnalysisCalculation.VazbyFiremNaPolitiky data = null)
        => PermanentCache.LoadOrSetDataFromPermanentCacheAsync<AnalysisCalculation.VazbyFiremNaPolitiky>(
            "FirmySVazbamiNaPolitiky_Aktualni",
            options => options.ModifyEntryOptionsDuration(TimeSpan.FromHours(12), TimeSpan.FromDays(10 * 365)),
            data
        );

    public static ValueTask<AnalysisCalculation.VazbyFiremNaPolitiky> FirmySVazbamiNaPolitiky_NedavneAsync(
        AnalysisCalculation.VazbyFiremNaPolitiky data = null)
        => PermanentCache.LoadOrSetDataFromPermanentCacheAsync<AnalysisCalculation.VazbyFiremNaPolitiky>(
            "FirmySVazbamiNaPolitiky_Nedavne",
            options => options.ModifyEntryOptionsDuration(TimeSpan.FromHours(12), TimeSpan.FromDays(10 * 365)),
            data
        );

    public static ValueTask<AnalysisCalculation.VazbyFiremNaPolitiky> FirmySVazbamiNaPolitiky_VsechnyAsync(
        AnalysisCalculation.VazbyFiremNaPolitiky data = null)
        => PermanentCache.LoadOrSetDataFromPermanentCacheAsync<AnalysisCalculation.VazbyFiremNaPolitiky>(
            "FirmySVazbamiNaPolitiky_Vsechny",
            options => options.ModifyEntryOptionsDuration(TimeSpan.FromHours(12), TimeSpan.FromDays(10 * 365)),
            data
        );

    // dotace reports
    // to rebuild use Tasks.RebuildStatisticsDotaceAsync()
    public static ValueTask<string[]> AllIcosInDotaceCacheAsync(string[] data = null)
        => PermanentCache.LoadOrSetDataFromPermanentCacheAsync<string[]>(
            "_allIcosInDotaceCache",
            options => options.ModifyEntryOptionsDuration(TimeSpan.FromHours(12), TimeSpan.FromDays(10 * 365)),
            data
        );

    public static ValueTask<StatisticsSubjectPerYear<Firma.Statistics.Dotace>[]> TopDotaceHoldingCacheAsync(
        StatisticsSubjectPerYear<Firma.Statistics.Dotace>[] data = null)
        => PermanentCache.LoadOrSetDataFromPermanentCacheAsync<StatisticsSubjectPerYear<Firma.Statistics.Dotace>[]>(
            "_topDotaceHoldingCache",
            options => options.ModifyEntryOptionsDuration(TimeSpan.FromHours(12), TimeSpan.FromDays(10 * 365)),
            data
        );

    public static ValueTask<StatisticsSubjectPerYear<Firma.Statistics.Dotace>[]> TopDotaceHoldingStatniCacheAsync(
        StatisticsSubjectPerYear<Firma.Statistics.Dotace>[] data = null)
        => PermanentCache.LoadOrSetDataFromPermanentCacheAsync<StatisticsSubjectPerYear<Firma.Statistics.Dotace>[]>(
            "_topDotaceHoldingStatniCache",
            options => options.ModifyEntryOptionsDuration(TimeSpan.FromHours(12), TimeSpan.FromDays(10 * 365)),
            data
        );


    //Caches with factory
    public static async Task<AnalysisCalculation.VazbyFiremNaUradyStat>
        GetUradyObchodujiciSFirmami_s_vazbouNaPolitiky_vsechnyAsync(bool forceUpdate = false)
    {
        string key = $"UradyObchodujiciSFirmami_s_vazbouNaPolitiky_vsechny";

        if (forceUpdate)
        {
            await PermanentCache.ExpireAsync(key);
        }

        return await PermanentCache.GetOrSetAsync(key,
            _ => AnalysisCalculation
                .UradyObchodujiciSFirmami_s_vazbouNaPolitikyAsync(Relation.AktualnostType.Libovolny, true),
            options => { options.ModifyEntryOptionsDuration(TimeSpan.FromHours(12), TimeSpan.FromDays(10 * 365)); });
    }

    public static async Task<AnalysisCalculation.VazbyFiremNaUradyStat>
        GetUradyObchodujiciSFirmami_s_vazbouNaPolitiky_nedavneAsync(bool forceUpdate = false)
    {
        const string key = "UradyObchodujiciSFirmami_s_vazbouNaPolitiky_nedavne";

        if (forceUpdate)
            await PermanentCache.ExpireAsync(key);

        return await PermanentCache.GetOrSetAsync(key,
            _ => AnalysisCalculation.UradyObchodujiciSFirmami_s_vazbouNaPolitikyAsync(Relation.AktualnostType.Nedavny,
                true),
            options => { options.ModifyEntryOptionsDuration(TimeSpan.FromHours(12), TimeSpan.FromDays(10 * 365)); });
    }

    public static async Task<AnalysisCalculation.VazbyFiremNaUradyStat>
        GetUradyObchodujiciSFirmami_s_vazbouNaPolitiky_aktualniAsync(bool forceUpdate = false)
    {
        const string key = "UradyObchodujiciSFirmami_s_vazbouNaPolitiky_aktualni";

        if (forceUpdate)
            await PermanentCache.ExpireAsync(key);

        return await PermanentCache.GetOrSetAsync(key,
            _ => AnalysisCalculation.UradyObchodujiciSFirmami_s_vazbouNaPolitikyAsync(Relation.AktualnostType.Aktualni,
                true),
            options => { options.ModifyEntryOptionsDuration(TimeSpan.FromHours(12), TimeSpan.FromDays(10 * 365)); });
    }

    public static async Task<IEnumerable<AnalysisCalculation.IcoSmlouvaMinMax>> GetFirmyCasovePodezreleZalozeneAsync(
        IEnumerable<AnalysisCalculation.IcoSmlouvaMinMax> data = null)
    {
        const string key = "FirmyCasovePodezreleZalozene";

        if (data != null)
        {
            await PermanentCache.SetAsync(key, data,
                options =>
                {
                    options.ModifyEntryOptionsDuration(TimeSpan.FromHours(12), TimeSpan.FromDays(10 * 365));
                });
        }

        return await PermanentCache.GetOrSetAsync(key,
            _ => AnalysisCalculation.GetFirmyCasovePodezreleZalozeneAsync(),
            options => { options.ModifyEntryOptionsDuration(TimeSpan.FromHours(12), TimeSpan.FromDays(10 * 365)); });
    }

    public static async Task<string> GetCzechDictAsync(bool forceUpdate = false)
    {
        const string key = "Czech.3-2-5.dic.txt";

        if (forceUpdate)
            await PermanentCache.ExpireAsync(key);

        return await PermanentCache.GetOrSetAsync(key,
            _ => Devmasters.Net.HttpClient.Simple.GetAsync(
                "https://somedata.hlidacstatu.cz/appdata/Czech.3-2-5.dic.txt"),
            options =>
            {
                options.ModifyEntryOptionsDuration(TimeSpan.FromDays(10 * 365), TimeSpan.FromDays(10 * 365));
            });
    }

    public static string GetCrawlerUserAgents(bool forceUpdate = false)
    {
        const string key = "crawler-user-agents.json";

        if (forceUpdate)
            PermanentCache.Expire(key);

        return PermanentCache.GetOrSet(key,
            _ => Devmasters.Net.HttpClient.Simple.Get(
                "https://somedata.hlidacstatu.cz/appdata/crawler-user-agents.json"),
            options =>
            {
                options.ModifyEntryOptionsDuration(TimeSpan.FromDays(10 * 365), TimeSpan.FromDays(10 * 365));
            });
    }

    public static async Task<OrganizacniStrukturyUradu> GetOrganizacniStrukturyUraduAsync(bool forceUpdate = false)
    {
        const string key = "OrganizacniStrukturyUradu";

        if (forceUpdate)
            await PermanentCache.ExpireAsync(key);

        return await PermanentCache.GetOrSetAsync(key, async _ => await OrganizacniStrukturyUraduFactoryAsync(),
            options => { options.ModifyEntryOptionsDuration(TimeSpan.FromDays(90), TimeSpan.FromDays(90)); });
    }

    private static async Task<OrganizacniStrukturyUradu> OrganizacniStrukturyUraduFactoryAsync()
    {
        OrganizacniStrukturyUradu res = new OrganizacniStrukturyUradu();
        try
        {
            var ossu = await ParseOssuAsync();

            res.PlatneKDatu = ossu.ExportInfo.ExportDatumCas;

            var organizaniStrukturyUradu = new Dictionary<string, List<JednotkaOrganizacni>>();
            try
            {
                foreach (var urad in ossu.UradSluzebniSeznam.SluzebniUrady)
                {
                    var f = await FirmaRepo.FromDSAsync(urad.idDS);
                    if (f is null || !f.Valid)
                    {
                        if (string.IsNullOrEmpty(urad.idNadrizene))
                        {
                            _logger.Error(
                                $"Organizační struktura - nenalezena datová schránka [{urad.idDS}] úřadu [{urad.oznaceni}]");
                            continue;
                        }

                        var nadrizeny = ossu.UradSluzebniSeznam.SluzebniUrady
                            .FirstOrDefault(u => u.id == urad.idNadrizene);

                        if (nadrizeny is null)
                        {
                            _logger.Error(
                                $"Nenalezen nadřízený úřad, ani datová schránka [{urad.idDS}] úřadu [{urad.oznaceni}]");
                            continue;
                        }

                        f = await FirmaRepo.FromDSAsync(nadrizeny.idDS);
                        if (f is null || !f.Valid)
                        {
                            _logger.Error(
                                $"Organizační struktura - nenalezena datová schránka [{nadrizeny.idDS}] nadřízeného úřadu [{nadrizeny.oznaceni}]");
                            continue;
                        }
                    }

                    var sluzebniUrad = ossu.OrganizacniStruktura
                        .FirstOrDefault(os => os.id == urad.id)
                        ?.StrukturaOrganizacni?.HlavniOrganizacniJednotka;

                    if (sluzebniUrad is null)
                    {
                        _logger.Information(
                            $"Služební úřad [{urad.oznaceni}] nemá podřízené organizace.");
                        continue;
                    }

                    if (organizaniStrukturyUradu.TryGetValue(f.ICO, out var sluzebniUrady))
                    {
                        sluzebniUrady.Add(sluzebniUrad);
                    }
                    else
                    {
                        organizaniStrukturyUradu.Add(f.ICO, new List<JednotkaOrganizacni>()
                        {
                            sluzebniUrad
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Chyba záznamu při zpracování struktury úřadů. {ex}");
            }

            res.Urady = organizaniStrukturyUradu;
        }
        catch (Exception e)
        {
            _logger.Error($"Chyba při zpracování struktury úřadů. {e}");
        }
        
        return res;
    }
    
    private static async Task<organizacni_struktura_sluzebnich_uradu> ParseOssuAsync()
    {
        try
        {
            string url = "https://portal.isoss.gov.cz/opendata/ISoSS_Opendata_OSYS_OSSS.xml";
            string xml = await Devmasters.Net.HttpClient.Simple.GetAsync(url);

            var ser = new XmlSerializer(typeof(organizacni_struktura_sluzebnich_uradu));

            using (var streamReader = new StringReader(xml))
            {
                using (var reader = XmlReader.Create(streamReader))
                {
                    return (organizacni_struktura_sluzebnich_uradu)ser.Deserialize(reader);
                }
            }
        }
        catch (Exception e)
        {
            _logger.Error(e, "organizacni_struktura_sluzebnich_uradu ParseOssu");
            return new organizacni_struktura_sluzebnich_uradu();
        }
    }
}