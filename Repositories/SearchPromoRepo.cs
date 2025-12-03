using Devmasters;
using Devmasters.Batch;
using Devmasters.Collections;
using HlidacStatu.Connectors;
using HlidacStatu.Entities;
using HlidacStatu.Entities.Facts;
using HlidacStatu.Entities.KIndex;
using HlidacStatu.Repositories.Analysis.KorupcniRiziko;
using HlidacStatu.Searching;
using Nest;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HlidacStatu.Extensions;

namespace HlidacStatu.Repositories
{
    public class SearchPromoRepo
    {
        public static string MoreTextDefault = "Více <i class=\"fa-solid fa-hand-point-right\"></i>";

        private static readonly ILogger _logger = Log.ForContext(typeof(SearchPromoRepo));

        public static IRule[] Irules = new IRule[]
        {
            new OsobaId(HlidacStatu.Repositories.OsobaVazbyRepo.Icos_s_VazbouNaOsobu, "osobaid:", "ico:"),
            new Holding(HlidacStatu.Repositories.FirmaVazbyRepo.IcosInHolding, "holdingprijemce:", "ico:"),
            new Holding(HlidacStatu.Repositories.FirmaVazbyRepo.IcosInHolding, "holdingplatce:", "ico:"),
            new Holding(HlidacStatu.Repositories.FirmaVazbyRepo.IcosInHolding, "holdingdodavatel:", "ico:"),
            new Holding(HlidacStatu.Repositories.FirmaVazbyRepo.IcosInHolding, "holdingzadavatel:", "ico:"),
            new Holding(HlidacStatu.Repositories.FirmaVazbyRepo.IcosInHolding, null, "ico:"),
        };

        public static async Task FillDbAsync(bool fullRebuild = false, Action<string> outputWriter = null,
            Action<ActionProgressData> progressWriter = null)
        {
            //pravidla
            //titulek max 19 znaků
            //description tak do 70 znaků  

            _logger.Information(
                $"Starting SearchPromoRepo.FillDbAsync {(fullRebuild ? "with" : "without")} rebuild db");
            var dbSP = HlidacStatu.Connectors.Manager.GetESClient_SearchPromo();
            using var db = new DbEntities();

            // manualni 
            await SaveAsync(new SearchPromo()
            {
                PromoType = "manual",
                Id = "manual-001",
                Icon = "/content/searchpromo/platy-uredniku.png",
                Url = "https://platy.hlidacstatu.cz/",
                Title = "Platy úředníků",
                More = MoreTextDefault,
                Description = "Platy TOP úředníků a zaměstnanců státu<br />Porovnání s platy v soukromé sféře",
                Fulltext = "",
                Priority = 20,
            });
            await SaveAsync(new SearchPromo()
            {
                PromoType = "manual",
                Id = "manual-002",
                Icon = "/content/searchpromo/bulb.png",
                Url = "https://www.hlidacstatu.cz/adresar",
                Title = "Adresář úřadů",
                More = MoreTextDefault,
                Description = "Seznam úřadů a státních firem, přehledně rozdělené",
                Fulltext = "",
                Priority = 20,
            });
            await SaveAsync(new SearchPromo()
            {
                PromoType = "manual",
                Id = "manual-003",
                Icon = "/content/searchpromo/bulb.png",
                Url = "https://www.hlidacstatu.cz/kindex",
                Title = "Klíčová rizika",
                More = MoreTextDefault,
                Description = "Index klíčových rizik 1300 největší úřadů a firem, <b>za období 2017 - 2023</b>",
                Fulltext = "",
                Priority = 20,
            });
            await SaveAsync(new SearchPromo()
            {
                PromoType = "manual",
                Id = "manual-004",
                Icon = KIndexData.KIndexLabelIconUrl(KIndexData.KIndexLabelValues.D),
                Url = "https://www.hlidacstatu.cz/sponzori",
                Title = "Sponzoři politiků",
                More = MoreTextDefault,
                Description = "Kompletní seznam sponzorů politických stran od 2012, osoby i firmy",
                Fulltext = "",
                Priority = 20,
            });


            // Platy uredniku
            _logger.Information($"SearchPromoRepo.FillDbAsync PlatyUredniku loading all orgs");
            var PUOrgs = (await PuRepo.ExportAllAsync(null, PuRepo.DefaultYear)).Where(t => t.Tags?.Count > 0)
                .ToArray();
            int count = 0;
            _logger.Information($"SearchPromoRepo.FillDbAsync PlatyUredniku saving searchpromo");
            await Devmasters.Batch.Manager.DoActionForAllAsync(PUOrgs,
                async org =>
                {
                    var sp = new SearchPromo();

                    sp.PromoType = org.GetType().Name;
                    sp.Id = org.GetType().Name + "-" + org.Id;
                    sp.Priority = 100;
                    sp.Ico = org.Ico;
                    sp.Icon = "/content/searchpromo/platy-uredniku.png";
                    sp.Url = org.GetUrl(false);
                    sp.Title = "Platy úředníků";
                    sp.More = MoreTextDefault;
                    sp.Description = "<b>" + org.Nazev + "</b><br />"
                                     + (org.Platy.AktualniRok().Count > 0
                                         ? $"Platy od {HlidacStatu.Util.RenderData.NicePrice(org.Platy.AktualniRok().Min(m => m.HrubyMesicniPlatVcetneOdmen))} do {HlidacStatu.Util.RenderData.NicePrice(org.Platy.AktualniRok().Max(m => m.HrubyMesicniPlatVcetneOdmen))}"
                                         : "Zatím platy neposkytly")
                        ;
                    sp.Fulltext = org.Nazev
                                  + " " + "plat úředník manažer management odměna"
                                  + " " + string.Join(" ", org.Tags?.Select(m => m.Tag) ?? Array.Empty<string>());
                    if (Util.DataValidators.CheckCZICO(sp.Ico))
                    {
                        var zkratky =
                            DirectDB.Instance.GetList<string>(
                                $"select text from AutocompleteSynonyms where query like 'ico:{sp.Ico}%'");
                        sp.Fulltext = (string.Join(" ", zkratky) + " " + sp.Fulltext).Trim();
                    }

                    count++;
                    await SaveAsync(sp);
                    return new Devmasters.Batch.ActionOutputData();
                }
                , outputWriter ?? Devmasters.Batch.Manager.DefaultOutputWriter,
                progressWriter ?? Devmasters.Batch.Manager.DefaultProgressWriter, true,
                prefix: "FillDbAsync platy uredniku ", monitor: new MonitoredTaskRepo.ForBatch());


            _logger.Information($"SearchPromoRepo.FillDbAsync PlatyUredniku saved {count} records");

            //=========================================================================
            // K-Index
            //=========================================================================

            _logger.Information($"SearchPromoRepo.FillDbAsync KIndex loading all orgs");
            IEnumerable<SubjectWithKIndex> KIndxOrgs = await
                (await HlidacStatu.Repositories.Analysis.KorupcniRiziko.Statistics.GetStatisticsAsync(
                    (await KIndexRepo.GetAvailableCalculationYearsAsync()).Max()))
                .SubjektOrderedListKIndexCompanyAscAsync();
            count = 0;
            _logger.Information($"SearchPromoRepo.FillDbAsync KIndex saving searchpromo");

            await Devmasters.Batch.Manager.DoActionForAllAsync(KIndxOrgs,
                async rec =>
                {
                    KIndexData kidx = await KIndex.GetCachedAsync(HlidacStatu.Util.ParseTools.NormalizeIco(rec.Ico));
                    var infof =
                        (await kidx.InfoFactsAsync((await KIndexRepo.GetAvailableCalculationYearsAsync()).Max()))
                        .RenderFacts(2, true, false, lineFormat: "{0}", html: false);
                    var sp = new SearchPromo();

                    sp.PromoType = "KIndex";
                    sp.Id = sp.PromoType + "-" + rec.Ico;
                    sp.Ico = rec.Ico;
                    sp.Priority = 100;

                    var lbl = KIndexData.CalculateLabel(rec.KIndex);

                    sp.Icon = KIndexData.KIndexLabelIconUrl(lbl);
                    sp.Url = $"/kindex/detail/{rec.Ico}";
                    sp.Title = "Klíčová rizika";
                    sp.More = "Více <i class=\"fa-solid fa-hand-point-right\"></i>";
                    sp.Description = "<b>" + rec.Jmeno + "</b><br />"
                                     + infof.ShortenMe(70);
                    sp.Fulltext = rec.Jmeno
                                  + " " + "kindex korupce 2020 2021 2022 2023 2024";
                    if (Util.DataValidators.CheckCZICO(sp.Ico))
                    {
                        var zkratky =
                            DirectDB.Instance.GetList<string>(
                                $"select text from AutocompleteSynonyms where query like 'ico:{sp.Ico}%'");
                        sp.Fulltext = (string.Join(" ", zkratky) + " " + sp.Fulltext).Trim();
                    }

                    count++;
                    await SaveAsync(sp);
                    return new Devmasters.Batch.ActionOutputData();
                }
                , outputWriter ?? Devmasters.Batch.Manager.DefaultOutputWriter,
                progressWriter ?? Devmasters.Batch.Manager.DefaultProgressWriter, true,
                prefix: "FillDbAsync KIndex ", monitor: new MonitoredTaskRepo.ForBatch());


            //=========================================================================
            // CEOS
            //=========================================================================
        }


        public static async Task SaveAsync(SearchPromo sp)
        {
            var dbSP = HlidacStatu.Connectors.Manager.GetESClient_SearchPromo();

            var res = await dbSP.IndexAsync<SearchPromo>(sp, m => m.Id(sp.Id));
            if (!res.IsValid)
            {
                throw new ApplicationException(res.ServerError?.ToString());
            }
        }

        static List<SearchPromo> manualItems = null;

        public static async Task<Search.GeneralResult<SearchPromo>> SearchPromoForHledejAsync(string query, int page,
            int size)
        {
            if (manualItems == null)
            {
                manualItems = (await SimpleSearchAsync("promoType:manual", 1, 100))?.Result.ToList();
            }

            var result = await SimpleSearchAsync(query, page, size);
            if (result.Total < size)
            {
                foreach (var item in manualItems.ShuffleMe().Take(4))
                {
                    result.AppendResult(item);
                }
            }

            return result;
        }

        public static async Task<Search.GeneralResult<SearchPromo>> SimpleSearchAsync(string query, int page, int size)
        {
            List<SearchPromo> found = new List<SearchPromo>();

            string modifQ = SimpleQueryCreator
                .GetSimpleQuery(query, Irules).FullQuery();

            string[] specifiedIcosInQuery =
                RegexUtil.GetRegexGroupValues(modifQ, @"(ico\w{0,11}\: \s? (?<ic>\d{3,8}))", "ic");

            page = page - 1;
            if (page < 0)
                page = 0;

            if (page * size >= HlidacStatu.Connectors.Manager.MaxResultWindow) //elastic limit
            {
                page = 0;
                size = 0; //return nothing
            }


            var qc = GetSimpleQuery(query);


            ISearchResponse<SearchPromo> res = null;
            try
            {
                var client = HlidacStatu.Connectors.Manager.GetESClient_SearchPromo();
                res = await client.SearchAsync<SearchPromo>(s => s
                    .Size(size)
                    .From(page * size)
                    .Query(q => qc)
                );

                if (res.IsValid)
                {
                    foreach (var i in res.Hits)
                    {
                        found.Add(i.Source);
                    }

                    return new Search.GeneralResult<SearchPromo>(query, res.Total, found, size, true)
                        { Page = page };
                }
                else
                {
                    HlidacStatu.Connectors.Manager.LogQueryError<SearchPromo>(res, query);
                    return new Search.GeneralResult<SearchPromo>(query, 0, found, size, false) { Page = page };
                }
            }
            catch (Exception e)
            {
                if (res != null && res.ServerError != null)
                    HlidacStatu.Connectors.Manager.LogQueryError<SearchPromo>(res, query);
                else
                    _logger.Error(e, "");
                throw;
            }
        }

        public static QueryContainer GetSimpleQuery(string query)
        {
            var qc = SimpleQueryCreator.GetSimpleQuery<SearchPromo>(query,
                Irules); //, new string[] {"fulltext","description","title","ico" });
            return qc;
        }
    }
}