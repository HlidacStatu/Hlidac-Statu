using System.Net;
using HlidacStatu.Entities;
using HlidacStatu.Extensions.DataTables;
using HlidacStatu.Repositories;
using HlidacStatu.Util;
using HlidacStatu.Repositories.Cache;
using ZiggyCreatures.Caching.Fusion;

namespace HlidacStatu.Extensions.Cache;

public static class Platy
{
    
    public static class Politici
    {
        

        public static async Task<List<Views.PoliticiViewData>> GetFullPoliticiViewDataCached(int rok)
        {
            var fullPoliticiViewData = await CacheService.Cache.GetOrSetAsync<List<Views.PoliticiViewData>>(
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
            var fullOrganizaceViewData = await CacheService.Cache.GetOrSetAsync<List<Views.OrganizaceViewData>>(
                $"fullOrganizaceViewData_{rok}",
                factory: async _ =>
                {
                    var orgs = await PpRepo.GetActiveOrganizaceAsync(rok);
                
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