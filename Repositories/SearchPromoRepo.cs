using Devmasters;
using Devmasters.Batch;
using HlidacStatu.Connectors;
using HlidacStatu.Entities;
using HlidacStatu.Repositories.Searching;
using HlidacStatu.Repositories.Searching.Rules;
using HlidacStatu.Util;
using Nest;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HlidacStatu.Repositories
{
    public class SearchPromoRepo
    {
        private static readonly ILogger _logger = Log.ForContext(typeof(SearchPromoRepo));

        public static IRule[] Irules = new IRule[] {
            new OsobaId("osobaid:","ico:" ),
            new Holding("holdingprijemce:","ico:" ),
            new Holding("holdingplatce:","ico:" ),
            new Holding("holdingdodavatel:","ico:" ),
            new Holding("holdingzadavatel:","ico:" ),
            new Holding(null,"ico:" ),
        };

        public static async Task FillDbAsync(bool fullRebuild = false, Action<string> outputWriter = null, Action<ActionProgressData> progressWriter = null)
        {
            //pravidla
            //titulek max 19 znaků
            //description tak do 70 znaků  


            // Platy
            var dbSP = await HlidacStatu.Connectors.Manager.GetESClient_SearchPromoAsync();
            var PUOrgs = (await PuRepo.ExportAllAsync(null, PuRepo.DefaultYear)).Where(t => t.Tags?.Count > 0).ToArray();
            using var db = new DbEntities();
            int count = 0;
            await Devmasters.Batch.Manager.DoActionForAllAsync(PUOrgs,
                async org =>
                {

                    var sp = new SearchPromo();

                    sp.PromoType = org.GetType().Name;
                    sp.Id = org.GetType().Name + "-" + org.Id;
                    sp.Ico = org.Ico;
                    sp.Icon = "/content/searchpromo/platy-uredniku.png";
                    sp.Url = org.GetUrl(false);
                    sp.Title = "Platy úředníků";
                    sp.More = "Více <i class=\"fa-solid fa-hand-point-right\"></i>";
                    sp.Description = "<b>" + org.Nazev + "</b><br />" 
                        + (org.Platy.AktualniRok().Count > 0 ? 
                                $"Platy od {HlidacStatu.Util.RenderData.NicePrice(org.Platy.AktualniRok().Min(m => m.HrubyMesicniPlatVcetneOdmen))} do {HlidacStatu.Util.RenderData.NicePrice(org.Platy.AktualniRok().Max(m => m.HrubyMesicniPlatVcetneOdmen))}" 
                                : "Odmítli poskytnout platy")
                                ;
                    sp.Fulltext = org.Nazev
                            + " " + "plat úředník manažer management odměna"
                            + " " + string.Join(" ", org.Tags?.Select(m => m.Tag) ?? Array.Empty<string>());
                    if (Util.DataValidators.CheckCZICO(sp.Ico))
                    {
                        var zkratky = DirectDB.GetList<string>($"select text from AutocompleteSynonyms where query like 'ico:{sp.Ico}%'");
                        sp.Fulltext = (string.Join(" ", zkratky) + " " + sp.Fulltext).Trim();
                    }
                    count++;
                    await SaveAsync(sp);
                    return new Devmasters.Batch.ActionOutputData();
                }
                , outputWriter ?? Devmasters.Batch.Manager.DefaultOutputWriter, progressWriter ?? Devmasters.Batch.Manager.DefaultProgressWriter, true,
                prefix: "FillDbAsync platy uredniku ", monitor: new MonitoredTaskRepo.ForBatch());

            Console.WriteLine($"Platy uredniku:{count}");
        }


        public static async Task SaveAsync(SearchPromo sp)
        {
            var dbSP = await HlidacStatu.Connectors.Manager.GetESClient_SearchPromoAsync();

            var res = await dbSP.IndexAsync<SearchPromo>(sp, m => m.Id(sp.Id));
            if (!res.IsValid)
            {
                throw new ApplicationException(res.ServerError?.ToString());
            }

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
                var client = await HlidacStatu.Connectors.Manager.GetESClient_SearchPromoAsync();
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