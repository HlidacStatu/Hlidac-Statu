using System.Net;
using HlidacStatu.Entities;
using HlidacStatu.Extensions.DataTables;
using HlidacStatu.Repositories;
using HlidacStatu.Util;
using ZiggyCreatures.Caching.Fusion;

namespace HlidacStatu.Extensions.Cache;

public static class Platy
{
    private static readonly IFusionCache _cache = new FusionCache(new FusionCacheOptions()
    {
        DefaultEntryOptions = PredefinedCachingOptions.Default,
        CacheName = FusionCacheOptions.DefaultCacheName
    });

    public static class Politici
    {
        public static async Task<List<PpPrijem>?> GetPrijmyPolitikaCached(string id, int rok)
        {
            var detail = await _cache.GetOrSetAsync<List<PpPrijem>>(
                $"{nameof(PpRepo.GetPrijmyPolitikaAsync)}_{id}_{rok}-politici",
                _ => PpRepo.GetPrijmyPolitikaAsync(id, rok)
            );
            return detail;
        }

        public static async Task<List<Views.PoliticiViewData>> GetFullPoliticiViewDataCached(int rok)
        {
            var fullPoliticiViewData = await _cache.GetOrSetAsync<List<Views.PoliticiViewData>>(
                "fullPoliticiViewData_" + rok.ToString(),
                factory: async _ =>
                {
                    var politiciPlatyGroup =
                        await PpRepo.GetPrijmyGroupedByNameIdAsync(rok, pouzePotvrzene: true,
                            withDetails: true);

                    List<Views.PoliticiViewData> politiciViewData = new List<Views.PoliticiViewData>();

                    foreach (var politikPlatyKvp in politiciPlatyGroup)
                    {
                        var nameid = politikPlatyKvp.Key;
                        var platy = politikPlatyKvp.Value;
                        var osoba = OsobaRepo.GetByNameId(nameid);
                        var celkoveNaklady = platy.Sum(p => p.CelkoveRocniNakladyNaPolitika);

                        politiciViewData.Add(new Views.PoliticiViewData()
                        {
                            CelkoveRocniNaklady = RenderCelkoveRocniNaklady(platy),
                            CelkoveRocniNaklady_Sort = celkoveNaklady,
                            Politik =
                                $"<a href='/politici/politik/{osoba.NameId}'>{WebUtility.HtmlEncode(osoba.FullName())}</a>",
                            Politik_Sort = $"{osoba.Prijmeni}-{osoba.Jmeno}",
                            PocetJobu = platy.Length,
                            Pohlavi = osoba.Pohlavi,
                            PolitickeRoleFilter = osoba.MainRoles(PpRepo.DefaultYear),
                            PolitickaStrana =
                                osoba
                                    .CurrentPoliticalParty(), //todo: změnit na politickou stranu v konkrétním roce (přidat funkčnost)
                            Organizace = RenderOrganizace(platy),
                        });
                    }

                    return politiciViewData;
                }
            );

            return fullPoliticiViewData;
        }

        public static async Task<List<Views.OrganizaceViewData>> GetFullOrganizaceViewDataCached(int rok)
        {
            var fullOrganizaceViewData = await _cache.GetOrSetAsync<List<Views.OrganizaceViewData>>(
                $"fullOrganizaceViewData_{rok}",
                factory: async _ =>
                {
                    var orgs = await PpRepo.GetActiveOrganizaceAsync(rok);
                    // var allEventsPoskytnuto = await PpRepo.GetAllEventsAsync(rok,
                    //     m =>m.DotazovanaInformace == PuEvent.DruhDotazovaneInformace.Politik 
                    //         && m.Typ == PuEvent.TypUdalosti.PoskytnutiInformace || m.Typ == PuEvent.TypUdalosti.PoskytnutiInformace
                    // );

                    return orgs.Select(o =>
                    {
                        var org = new Views.OrganizaceViewData()
                        {
                            NazevOrganizace = $"<a href='/politici/organizace/{o.Ico}'>{o.Nazev}</a>",
                            EventStatus = o.PlatyForYearPoliticiDescriptionHtml(rok),
                            PocetOsob = 0
                        };

                        if (!o.PrijmyPolitiku.Any())
                        {
                            return org;
                        }

                        org.PlatyOd = o.PrijmyPolitiku.Min(p => p.CelkoveRocniNakladyNaPolitika);
                        org.PlatyDo = o.PrijmyPolitiku.Max(p => p.CelkoveRocniNakladyNaPolitika);
                        org.PocetOsob = o.PrijmyPolitiku.Count();
                        org.NazevOrganizace = $"<a href='/politici/organizace/{o.Ico}'>{o.Nazev}</a>";
                        org.EventStatus = o.PlatyForYearPoliticiDescriptionHtml(rok);
                        return org;
                    }).ToList();
                }
            );

            return fullOrganizaceViewData;
        }

        public static List<string> GetPolitickeStranyForFilterCached()
        {
            return _cache.GetOrSet<List<string>>(
                "politickeStranyFilterData",
                _ =>
                {
                    List<string> politickeStranyIca =
                    [
                        "71443339",
                        "00442704",
                        "00496936",
                        "16192656",
                        "71339698",
                        "10742409",
                        "17085438",
                        "00409171",
                        "04134940",
                        "26673908",
                        "00409740",
                        "71339728",
                        "08288909"
                    ];

                    return politickeStranyIca.Select(ZkratkaStranyRepo.NazevStranyForIco).ToList();
                }
            );
        }


        private static string RenderCelkoveRocniNaklady(PpPrijem[] platy)
        {
            var rottenOrgs = platy.Where(p => p.Status == PpPrijem.StatusPlatu.Zjistujeme_zadost_106)
                .Select(n => n.Organizace)
                .Distinct()
                .ToList();
            var celkoveNaklady = platy.Sum(p => p.CelkoveRocniNakladyNaPolitika);
            var nicePrice = RenderData.NicePrice(celkoveNaklady, html: true);

            if (rottenOrgs.Any())
            {
                var result = $"""
                              <span class="help-tooltip" data-bs-toggle="tooltip" data-bs-html="true" title="Částka zahrnuje jen příjmy, které se nám podařilo shromáždit a které nám byly poskytnuty.">
                                {nicePrice}
                              </span> 
                              """;
                return result;
            }

            return nicePrice;
        }

        private static string RenderOrganizace(PpPrijem[] platy)
        {
            var goodOrgs = platy.Where(p => p.Status > PpPrijem.StatusPlatu.Zjistujeme_zadost_106)
                .Select(n => n.Organizace)
                .Distinct()
                .ToList();
            var rottenOrgs = platy.Where(p => p.Status == PpPrijem.StatusPlatu.Zjistujeme_zadost_106)
                .Select(n => n.Organizace)
                .Distinct()
                .ToList();

            var goodOrgsHtmlList = string.Join("", goodOrgs
                .Select(o => $"<li><a href='/politici/organizace/{o?.DS}'>{WebUtility.HtmlEncode(o?.Nazev)}</a></li>"));

            var rottenOrgsHtmlList = string.Join("", rottenOrgs
                .Select(o => $"""
                              <li>
                              <a href="/politici/organizace/{o?.DS}">{WebUtility.HtmlEncode(o?.Nazev)}</a>
                              <i class="text-danger fas fa-exclamation-circle" data-bs-toggle="tooltip" title="Plat či odměna nebyla poskytnuta."></i>
                              </li>
                              """));

            return "<ol>" + goodOrgsHtmlList + rottenOrgsHtmlList + "</ol>";
        }
    }

    public static class Urednici
    {
        public static async Task<int> GetPlatyCountPerYearCached(int year)
        {
            return await _cache.GetOrSetAsync<int>(
                $"platyCount_{year}",
                async _ => (await PuRepo.GetPlatyAsync(PuRepo.DefaultYear)).Count
            );
        }

        public static async Task<List<PuPlat>> GetPoziceDlePlatuCached(int min, int max, int year)
        {
            return await _cache.GetOrSetAsync<List<PuPlat>>(
                $"{nameof(PuRepo.GetPoziceDlePlatuAsync)}_{min}_{max}_{year}",
                async _ => await PuRepo.GetPoziceDlePlatuAsync(min, max, year)
            );
        }

        public static async Task<PuOrganizace> GetFullDetailOrganizaceCached(string datovaSchranka)
        {
            return await _cache.GetOrSetAsync<PuOrganizace>(
                $"{nameof(PuRepo.GetFullDetailAsync)}_{datovaSchranka}",
                async _ => await PuRepo.GetFullDetailAsync(datovaSchranka)
            );
        }
        public static async Task<List<PuOrganizace>> GetOrganizaceForTagCached(string tag)
        {
            return await _cache.GetOrSetAsync<List<PuOrganizace>>(
                $"{nameof(PuRepo.GetActiveOrganizaceForTagAsync)}_{tag}-urednici",
                _ => PuRepo.GetActiveOrganizaceForTagAsync(tag)
            );
        }
        public static async Task<PuPlat> GetPlatCached(int id)
        {
            return await _cache.GetOrSetAsync<PuPlat>(
                $"{nameof(PuRepo.GetPlatAsync)}_{id}-urednici",
                _ => PuRepo.GetPlatAsync(id)
            );
        }
    }

    public static class PredefinedCachingOptions
    {
        // Čerstvá data má v paměti po dobu 10 minut => stará
        // Stará data má v paměti max. 4 hodiny a pokud dojde k výpadku zdroje, použije starou hodnotu a prodlouží dočasnou čerstvost o 1 minutu
        // Při načítání nových dat do cache dá databázi 100ms čas a v případě, že požadavek trvá déle, použije starou cache, zatímco na pozadí načítá data nová
        public static readonly FusionCacheEntryOptions Default = new FusionCacheEntryOptions(
                TimeSpan.FromMinutes(10)
            )
            .SetFailSafe(true, TimeSpan.FromHours(4), TimeSpan.FromMinutes(1))
            .SetFactoryTimeouts(TimeSpan.FromMilliseconds(100)
            );

        public static readonly FusionCacheEntryOptions Long = new FusionCacheEntryOptions(
                TimeSpan.FromHours(10)
            )
            .SetFailSafe(true, TimeSpan.FromHours(40), TimeSpan.FromMinutes(30))
            .SetFactoryTimeouts(TimeSpan.FromSeconds(10)
            );
    }
}

public class Views
{
    public class PoliticiViewData
    {
        public string Politik { get; set; }

        [HtmlTableDefinition.Column(HtmlTableDefinition.ColumnType.Hidden, "PolitikSort")]
        public string Politik_Sort { get; set; }

        [HtmlTableDefinition.Column(HtmlTableDefinition.ColumnType.Text, "Politická strana")]
        public string PolitickaStrana { get; set; }

        [HtmlTableDefinition.Column(HtmlTableDefinition.ColumnType.Text, "Politická role")]
        public string PolitickaRole => string.Join(", ", PolitickeRoleFilter);

        [HtmlTableDefinition.Column(HtmlTableDefinition.ColumnType.Hidden, "PolitRoleFilter")]
        public List<string> PolitickeRoleFilter { get; set; }

        [HtmlTableDefinition.Column(HtmlTableDefinition.ColumnType.Number,
            HlidacStatu.Lib.Web.UI.TagHelpers.CelkovyRocniPrijemTagHelper.Content)]
        public string CelkoveRocniNaklady { get; set; }

        [HtmlTableDefinition.Column(HtmlTableDefinition.ColumnType.Hidden, "Prijem sort")]
        public Decimal CelkoveRocniNaklady_Sort { get; set; }

        [HtmlTableDefinition.Column(HtmlTableDefinition.ColumnType.Number, "Počet jobů")]
        public Decimal PocetJobu { get; set; }

        public string Organizace { get; set; }

        [HtmlTableDefinition.Column(HtmlTableDefinition.ColumnType.Hidden, "Pohlaví")]
        public string Pohlavi { get; set; }
    }

    public class OrganizaceViewData
    {
        [HtmlTableDefinition.Column(HtmlTableDefinition.ColumnType.Text, "Název organizace")]
        public string NazevOrganizace { get; set; }

        [HtmlTableDefinition.Column(HtmlTableDefinition.ColumnType.Text, "Stav")]
        public string EventStatus { get; set; }

        [HtmlTableDefinition.Column(HtmlTableDefinition.ColumnType.Number, "Počet osob")]
        public Decimal PocetOsob { get; set; }

        [HtmlTableDefinition.Column(HtmlTableDefinition.ColumnType.Hidden, "PlatyOd")]
        public Decimal PlatyOd { get; set; }

        [HtmlTableDefinition.Column(HtmlTableDefinition.ColumnType.Hidden, "PlatyDo")]
        public Decimal PlatyDo { get; set; }

        [HtmlTableDefinition.Column(HtmlTableDefinition.ColumnType.Text, "Rozpětí platů")]
        public string Platy =>
            $"{RenderData.NicePrice(PlatyOd).Replace(" ", "&nbsp;")}-{RenderData.NicePrice(PlatyDo).Replace(" ", "&nbsp;")}";

        [HtmlTableDefinition.Column(HtmlTableDefinition.ColumnType.Hidden, "Platy sort")]
        public decimal Platy_Sort => PlatyDo;
    }
}