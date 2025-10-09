using Devmasters;
using Devmasters.Crypto;
using Devmasters.DT;
using Devmasters.Enums;

using HlidacStatu.Entities;
using HlidacStatu.Repositories.Searching;


using Nest;

using Newtonsoft.Json;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HlidacStatu.Connectors;
using Serilog;
using System.ComponentModel;

namespace HlidacStatu.Repositories
{
    public static partial class SmlouvaRepo
    {
        private static readonly ILogger _logger = Log.ForContext(typeof(SmlouvaRepo));
        public static class Searching
        {
            public const int DefaultPageSize = 40;


            [ShowNiceDisplayName()]
            [Sortable(SortableAttribute.SortAlgorithm.BySortValue)]
            public enum OrderResult
            {
                [SortValue(0)]
                [NiceDisplayName("podle relevance")]
                [Description("By relevance")]
                Relevance = 0,

                [SortValue(5)]
                [NiceDisplayName("nově zveřejněné první")]
                [Description("Newly published first")]
                DateAddedDesc = 1,

                [NiceDisplayName("nově zveřejněné poslední")]
                [SortValue(6)]
                [Description("Newly published last")]
                DateAddedAsc = 2,

                [SortValue(1)]
                [NiceDisplayName("nejlevnější první")]
                [Description("Cheapest first")]
                PriceAsc = 3,

                [SortValue(2)]
                [NiceDisplayName("nejdražší první")]
                [Description("Most expensive first")]
                PriceDesc = 4,

                [SortValue(7)]
                [NiceDisplayName("nově uzavřené první")]
                [Description("Newly closed first")]
                DateSignedDesc = 5,

                [NiceDisplayName("nově uzavřené poslední")]
                [SortValue(8)]
                [Description("Newly closed last")]
                DateSignedAsc = 6,

                [NiceDisplayName("nejvíce chybové první")]
                [SortValue(10)]
                [Description("Most errors first")]
                ConfidenceDesc = 7,

                [NiceDisplayName("podle odběratele")]
                [SortValue(98)]
                [Description("By customer")]
                CustomerAsc = 8,

                [NiceDisplayName("podle dodavatele")]
                [SortValue(99)]
                [Description("By contractor")]
                ContractorAsc = 9,

                [Disabled]
                [Description("Classification relevance")]
                ClassificationRelevance = 665,

                [Disabled]
                [Description("Fastest for scroll")]
                FastestForScroll = 666,

                [Disabled]
                [Description("Last update")]
                LastUpdate = 667,
            }

            public static HlidacStatu.Searching.IRule[] Irules = new HlidacStatu.Searching.IRule[] {
               new HlidacStatu.Searching.OsobaId(HlidacStatu.Repositories.OsobaVazbyRepo.Icos_s_VazbouNaOsobu, "osobaid:","ico:" ),
               new HlidacStatu.Searching.Holding(HlidacStatu.Repositories.FirmaVazbyRepo.IcosInHolding, "holdingprijemce:","icoprijemce:" ),
               new HlidacStatu.Searching.Holding(HlidacStatu.Repositories.FirmaVazbyRepo.IcosInHolding, "holdingplatce:","icoplatce:" ),
               new HlidacStatu.Searching.Holding(HlidacStatu.Repositories.FirmaVazbyRepo.IcosInHolding, "holdingdodavatel:","icoprijemce:" ),
               new HlidacStatu.Searching.Holding(HlidacStatu.Repositories.FirmaVazbyRepo.IcosInHolding, "holdingzadavatel:","icoplatce:" ),
               new HlidacStatu.Searching.Holding(HlidacStatu.Repositories.FirmaVazbyRepo.IcosInHolding, null,"ico:" ),

               new HlidacStatu.Searching.TransformPrefixWithValue("ds:","(prijemce.datovaSchranka:${q} OR platce.datovaSchranka:${q}) ",null ),
               new HlidacStatu.Searching.TransformPrefix("dsprijemce:","prijemce.datovaSchranka:",null  ),
               new HlidacStatu.Searching.TransformPrefix("dsplatce:","platce.datovaSchranka:",null  ),
               new HlidacStatu.Searching.TransformPrefixWithValue("ico:","(prijemce.ico:${q} OR platce.ico:${q}) ",null ),
               new HlidacStatu.Searching.TransformPrefix("icoprijemce:","prijemce.ico:",null ),
               new HlidacStatu.Searching.TransformPrefix("icoplatce:","platce.ico:",null ),
               new HlidacStatu.Searching.TransformPrefix("jmenoprijemce:","prijemce.nazev:",null ),
               new HlidacStatu.Searching.TransformPrefix("jmenoplatce:","platce.nazev:",null ),
               new HlidacStatu.Searching.TransformPrefix("id:","id:",null ),
               new HlidacStatu.Searching.TransformPrefix("idverze:","id:",null ),
               new HlidacStatu.Searching.TransformPrefix("idsmlouvy:","identifikator.idSmlouvy:",null ),
               new HlidacStatu.Searching.TransformPrefix("predmet:","predmet:",null ),
               new HlidacStatu.Searching.TransformPrefix("cislosmlouvy:","cisloSmlouvy:",null ),
               new HlidacStatu.Searching.TransformPrefix("mena:","ciziMena.mena:",null ),
               new HlidacStatu.Searching.TransformPrefix("cenasdph:","hodnotaVcetneDph:",null ),
               new HlidacStatu.Searching.TransformPrefix("cenabezdph:","hodnotaBezDph:",null ),
               new HlidacStatu.Searching.TransformPrefix("cena:","calculatedPriceWithVATinCZK:",null ),
               new HlidacStatu.Searching.TransformPrefix("zverejneno:","casZverejneni:", "[<>]?[{\\[]+" ),
               new HlidacStatu.Searching.TransformPrefixWithValue("zverejneno:","casZverejneni:[${q} TO ${q}||+1d}", "\\d+" ),
               new HlidacStatu.Searching.TransformPrefix("podepsano:","datumUzavreni:", "[<>]?[{\\[]+" ),
               new HlidacStatu.Searching.TransformPrefixWithValue("podepsano:","datumUzavreni:[${q} TO ${q}||+1d}", "\\d+"  ),
               new HlidacStatu.Searching.TransformPrefix("schvalil:","schvalil:",null ),
               new HlidacStatu.Searching.TransformPrefix("textsmlouvy:","prilohy.plainTextContent:",null ),
               new HlidacStatu.Searching.Smlouva_Chyby(),
               new HlidacStatu.Searching.Smlouva_Oblast(1),
               new HlidacStatu.Searching.Smlouva_Oblast(2),
               new HlidacStatu.Searching.Smlouva_Oblasti(),
               
               new HlidacStatu.Searching.TransformPrefix("ico_platce:", "platce.ico:",null),
               new HlidacStatu.Searching.TransformPrefix("ico_prijemce:", "prijemce.ico:",null),
               new HlidacStatu.Searching.TransformPrefixWithValue("datum_od:","(casZverejneni:>=${q} OR datumUzavreni:>=${q})", "\\d+" ),
               new HlidacStatu.Searching.TransformPrefixWithValue("datum_do:","(casZverejneni:>=${q} OR datumUzavreni:>=${q})", "\\d+" ),
               new HlidacStatu.Searching.TransformPrefixWithValue("castka_od:","calculatedPriceWithVATinCZK:>=${q}", "\\d+" ),
               new HlidacStatu.Searching.TransformPrefixWithValue("castka_do:","calculatedPriceWithVATinCZK:<=${q}", "\\d+" ),

            };


            public static QueryContainer GetSimpleQuery(string query)
            {
                QueryContainer qc = HlidacStatu.Searching.SimpleQueryCreator.GetSimpleQuery<Smlouva>(query, Irules);
                return qc;
            }

            public static async Task<SmlouvaSearchResult> SearchRawAsync(QueryContainer query, int page, int pageSize, OrderResult order,
                AggregationContainerDescriptor<Smlouva> anyAggregation = null,
                bool? platnyZaznam = null, bool includeNeplatne = false, bool logError = true, bool fixQuery = true,
                bool withHighlighting = false, CancellationToken cancellationToken = default)
            {

                var result = new SmlouvaSearchResult()
                {
                    Page = page,
                    PageSize = pageSize,
                    OrigQuery = "",
                    Q = "",
                    Order = ((int)order).ToString()
                };

                ISearchResponse<Smlouva> res = await _coreSearchAsync(query, page, pageSize, order, anyAggregation, platnyZaznam,
                    includeNeplatne, logError, withHighlighting, cancellationToken: cancellationToken);


                if (res.IsValid == false && logError)
                    Manager.LogQueryError<Smlouva>(res, query.ToString());


                result.Total = res?.Total ?? 0;
                result.IsValid = res?.IsValid ?? false;
                result.ElasticResults = res;
                return result;
            }



            public static Task<SmlouvaSearchResult> SimpleSearchAsync(string query, int page, int pageSize, string order,
AggregationContainerDescriptor<Smlouva> anyAggregation = null,
bool? platnyZaznam = null, bool includeNeplatne = false, bool logError = true, bool fixQuery = true,
bool withHighlighting = false, bool exactNumOfResults = false)
            {
                order = TextUtil.NormalizeToNumbersOnly(order);
                OrderResult eorder = OrderResult.Relevance;
                Enum.TryParse<OrderResult>(order, out eorder);

                return SimpleSearchAsync(query, page, pageSize, eorder, anyAggregation,
                    platnyZaznam, includeNeplatne, logError, fixQuery,
                    withHighlighting, exactNumOfResults
                    );

            }
            public static async Task<SmlouvaSearchResult> SimpleSearchAsync(string query, int page, int pageSize, OrderResult order,
        AggregationContainerDescriptor<Smlouva> anyAggregation = null,
        bool? platnyZaznam = null, bool includeNeplatne = false, bool logError = true, bool fixQuery = true,
        bool withHighlighting = false, bool exactNumOfResults = false, CancellationToken cancellationToken = default)
            {

                var result = new SmlouvaSearchResult()
                {
                    Page = page,
                    PageSize = pageSize,
                    OrigQuery = query,
                    Q = query,
                    Order = ((int)order).ToString()
                };

                if (string.IsNullOrEmpty(query))
                {
                    result.ElasticResults = null;
                    result.IsValid = false;
                    result.Total = 0;
                    return result;
                }

                StopWatchEx sw = new StopWatchEx();
                sw.Start();

                if (fixQuery)
                {
                    query = HlidacStatu.Searching.Tools.FixInvalidQuery(query, Irules, HlidacStatu.Searching.Tools.DefaultQueryOperators);
                    result.Q = query;
                }

                if (platnyZaznam.HasValue)
                    query = HlidacStatu.Searching.Query.ModifyQueryAND(query, "platnyZaznam:" + platnyZaznam.Value.ToString().ToLower());


                ISearchResponse<Smlouva> res =
                    await _coreSearchAsync(GetSimpleQuery(query), page, pageSize, order, anyAggregation, platnyZaznam,
                    includeNeplatne, logError, withHighlighting, exactNumOfResults, cancellationToken: cancellationToken);

                AuditRepo.Add(Audit.Operations.Search, "", "", "Smlouva", res.IsValid ? "valid" : "invalid", query, null);

                if (res.IsValid == false && logError)
                    Manager.LogQueryError<Smlouva>(res, query);

                sw.Stop();

                result.ElapsedTime = sw.Elapsed;
                try
                {
                    result.Total = res?.Total ?? 0;

                }
                catch (Exception)
                {
                    result.Total = 0;
                }
                result.IsValid = res?.IsValid ?? false;
                result.ElasticResults = res;
                return result;
            }


            private static async Task<ISearchResponse<Smlouva>> _coreSearchAsync(QueryContainer query, int page, int pageSize,
                OrderResult order,
                AggregationContainerDescriptor<Smlouva> anyAggregation = null,
                bool? platnyZaznam = null, bool includeNeplatne = false, bool logError = true,
                bool withHighlighting = false, bool exactNumOfResults = false, CancellationToken cancellationToken = default)
            {
                page = page - 1;
                if (page < 0)
                    page = 0;

                if (page * pageSize >= Repositories.Searching.Tools.MaxResultWindow) //elastic limit
                {
                    page = 0; pageSize = 0; //return nothing
                }

                AggregationContainerDescriptor<Smlouva> baseAggrDesc = null;
                baseAggrDesc = anyAggregation == null ?
                        null //new AggregationContainerDescriptor<Smlouva>().Sum("sumKc", m => m.Field(f => f.CalculatedPriceWithVATinCZK))
                        : anyAggregation;

                Func<AggregationContainerDescriptor<Smlouva>, AggregationContainerDescriptor<Smlouva>> aggrFunc
                    = (aggr) => { return baseAggrDesc; };

                ISearchResponse<Smlouva> res = null;
                try
                {
                    var client = Manager.GetESClient();
                    if (platnyZaznam.HasValue && platnyZaznam == false)
                        client = Manager.GetESClient_Sneplatne();
                    Indices indexes = client.ConnectionSettings.DefaultIndex;
                    if (includeNeplatne)
                    {
                        indexes = Manager.defaultIndexName_SAll;
                    }

                    res = await client
                        .SearchAsync<Smlouva>(s => s
                            .Index(indexes)
                            .Size(pageSize)
                            .From(page * pageSize)
                            .Query(q => query)
                            .Source(m => m.Excludes(e => e.Field(o => o.Prilohy)))
                            .Sort(ss => GetSort(order))
                            .Aggregations(aggrFunc)
                            .Highlight(h => Repositories.Searching.Tools.GetHighlight<Smlouva>(withHighlighting))
                            .TrackTotalHits(exactNumOfResults || page * pageSize == 0 ? true : (bool?)null),
                            cancellationToken
                    );
                    if (res != null && res.IsValid == false && res.ServerError?.Status == 429)
                    {
                        await Task.Delay(100);
                        res = await client
                            .SearchAsync<Smlouva>(s => s
                                .Index(indexes)
                                .Size(pageSize)
                                .From(page * pageSize)
                                .Query(q => query)
                                .Source(m => m.Excludes(e => e.Field(o => o.Prilohy)))
                                .Sort(ss => GetSort(order))
                                .Aggregations(aggrFunc)
                                .Highlight(h => Repositories.Searching.Tools.GetHighlight<Smlouva>(withHighlighting))
                                .TrackTotalHits(exactNumOfResults || page * pageSize == 0 ? true : (bool?)null),
                                cancellationToken
                        );
                        if (res.IsValid == false && res.ServerError?.Status == 429)
                        {
                            await Task.Delay(100);
                            res = await client
                                .SearchAsync<Smlouva>(s => s
                                    .Index(indexes)
                                    .Size(pageSize)
                                    .From(page * pageSize)
                                    .Query(q => query)
                                    .Source(m => m.Excludes(e => e.Field(o => o.Prilohy)))
                                    .Sort(ss => GetSort(order))
                                    .Aggregations(aggrFunc)
                                    .Highlight(h => Tools.GetHighlight<Smlouva>(withHighlighting))
                                    .TrackTotalHits(exactNumOfResults || page * pageSize == 0 ? true : (bool?)null),
                                    cancellationToken
                            );

                        }

                    }

                    if (withHighlighting && res.Shards != null && res.Shards.Failed > 0) //if some error, do it again without highlighting
                    {
                        res = await client
                            .SearchAsync<Smlouva>(s => s
                                .Index(indexes)
                                .Size(pageSize)
                                .From(page * pageSize)
                                .Query(q => query)
                                .Source(m => m.Excludes(e => e.Field(o => o.Prilohy)))
                                .Sort(ss => GetSort(order))
                                .Aggregations(aggrFunc)
                                .Highlight(h => Tools.GetHighlight<Smlouva>(false))
                                .TrackTotalHits(exactNumOfResults || page * pageSize == 0 ? true : (bool?)null),
                                cancellationToken
                        );
                    }
                }
                catch (Exception e)
                {
                    if (e.Message == "A task was canceled.")
                        throw;

                    if (res != null && res.ServerError != null)
                        Manager.LogQueryError<Smlouva>(res);
                    else
                        _logger.Error(e, "");
                    throw;
                }

                if (res.IsValid == false && logError)
                    Manager.LogQueryError<Smlouva>(res);

                return res;
            }


            public static Task<ISearchResponse<Smlouva>> RawSearchAsync(string jsonQuery, int page, int pageSize, OrderResult order = OrderResult.Relevance,
                AggregationContainerDescriptor<Smlouva> anyAggregation = null, bool? platnyZaznam = null,
                bool includeNeplatne = false, bool exactNumOfResults = false) 
                => RawSearchAsync(Tools.GetRawQuery(jsonQuery), page, pageSize, order, anyAggregation, platnyZaznam, includeNeplatne,
                    exactNumOfResults: exactNumOfResults);

            public static async Task<ISearchResponse<Smlouva>> RawSearchAsync(QueryContainer query, int page, int pageSize, OrderResult order = OrderResult.Relevance,
                AggregationContainerDescriptor<Smlouva> anyAggregation = null, bool? platnyZaznam = null,
                bool includeNeplatne = false,
                bool withHighlighting = false, bool exactNumOfResults = false
                )
            {
                var res = await _coreSearchAsync(query, page, pageSize, order, anyAggregation, platnyZaznam: platnyZaznam, includeNeplatne: includeNeplatne, logError: true, withHighlighting: withHighlighting, exactNumOfResults: exactNumOfResults);
                return res;

            }
            public static SortDescriptor<Smlouva> GetSort(string sorder)
            {
                OrderResult order = OrderResult.Relevance;
                Enum.TryParse<OrderResult>(sorder, out order);
                return GetSort(order);
            }

            public static SortDescriptor<Smlouva> GetSort(OrderResult order)
            {
                SortDescriptor<Smlouva> s = new SortDescriptor<Smlouva>().Field(f => f.Field("_score").Descending());
                switch (order)
                {
                    case OrderResult.DateAddedDesc:
                        s = new SortDescriptor<Smlouva>().Field(m => m.Field(f => f.casZverejneni).Descending());
                        break;
                    case OrderResult.DateAddedAsc:
                        s = new SortDescriptor<Smlouva>().Field(m => m.Field(f => f.casZverejneni).Ascending());
                        break;
                    case OrderResult.DateSignedDesc:
                        s = new SortDescriptor<Smlouva>().Field(m => m.Field(f => f.datumUzavreni).Descending());
                        break;
                    case OrderResult.DateSignedAsc:
                        s = new SortDescriptor<Smlouva>().Field(m => m.Field(f => f.datumUzavreni).Ascending());
                        break;
                    case OrderResult.PriceAsc:
                        s = new SortDescriptor<Smlouva>().Field(m => m.Field(f => f.CalculatedPriceWithVATinCZK).Ascending());
                        break;
                    case OrderResult.PriceDesc:
                        s = new SortDescriptor<Smlouva>().Field(m => m.Field(f => f.CalculatedPriceWithVATinCZK).Descending());
                        break;
                    case OrderResult.FastestForScroll:
                        s = new SortDescriptor<Smlouva>().Field(f => f.Field("_doc"));
                        break;
                    case OrderResult.ConfidenceDesc:
                        s = new SortDescriptor<Smlouva>().Field(f => f.Field(ff => ff.ConfidenceValue).Descending());
                        break;
                    case OrderResult.CustomerAsc:
                        s = new SortDescriptor<Smlouva>().Field(f => f.Field(ff => ff.Platce.ico).Ascending());
                        break;
                    case OrderResult.ContractorAsc:
                        s = new SortDescriptor<Smlouva>().Field(f => f.Field("prijemce.ico").Ascending());
                        break;
                    case OrderResult.LastUpdate:
                        s = new SortDescriptor<Smlouva>().Field(f => f.Field("lastUpdate").Descending());
                        break;
                    case OrderResult.ClassificationRelevance:
                        s = new SortDescriptor<Smlouva>().Field(f => f.Field("classification.types.classifProbability").Descending());
                        break;
                    case OrderResult.Relevance:
                    default:
                        break;
                }

                return s;

            }

            public static string QueryHash(string typ, string q)
            {
                if (string.IsNullOrEmpty(q))
                    q = "_empty_";
                return Hash.ComputeHashToHex(typ.ToLower() + "|" + q.ToLower() + "|" + q.Reverse());
            }

            public static bool IsQueryHashCorrect(string typ, string q, string h)
            {
                return h == QueryHash(typ, q);
            }

            public static Devmasters.Cache.LocalMemory.Manager<SmlouvaSearchResult, (string query, AggregationContainerDescriptor<Smlouva> aggr)> cachedSearches = 
                new Devmasters.Cache.LocalMemory.Manager<SmlouvaSearchResult, (string query, AggregationContainerDescriptor<Smlouva> aggr)>(
                    "SMLOUVYsearch", 
                    tuple => funcSimpleSearchAsync(tuple).ConfigureAwait(false).GetAwaiter().GetResult(), 
                    TimeSpan.FromHours(24));
            
            public static SmlouvaSearchResult CachedSimpleSearchWithStat(TimeSpan expiration,
               string query, int page, int pageSize, OrderResult order,
               bool? platnyZaznam = null, bool includeNeplatne = false,
               bool logError = true, bool fixQuery = true
               )
            { 
                return CachedSimpleSearch(expiration, query, page, pageSize, order, platnyZaznam, includeNeplatne, logError, fixQuery,
                    new AggregationContainerDescriptor<Smlouva>().Sum("sumKc", m => m.Field(f => f.CalculatedPriceWithVATinCZK))
                    );
            }
            public static SmlouvaSearchResult CachedSimpleSearch(TimeSpan expiration,
                string query, int page, int pageSize, OrderResult order,
                bool? platnyZaznam = null, bool includeNeplatne = false,
                bool logError = true, bool fixQuery = true,
                AggregationContainerDescriptor<Smlouva> aggregation = null
                )
            {
                FullSearchQuery q = new FullSearchQuery()
                {
                    query = query,
                    page = page,
                    pageSize = pageSize,
                    order = order,
                    platnyZaznam = platnyZaznam,
                    includeNeplatne = includeNeplatne,
                    logError = logError,
                    fixQuery = fixQuery,
                    exactNumOfResults = true,
                    anyAggregation = aggregation
                };
                return cachedSearches.Get((JsonConvert.SerializeObject(q),q.anyAggregation), expiration);
            }
            private static async Task<SmlouvaSearchResult> funcSimpleSearchAsync((string query, AggregationContainerDescriptor<Smlouva> aggr) data)
            {
                var q = JsonConvert.DeserializeObject<FullSearchQuery>(data.query);
                var ret = await SimpleSearchAsync(
                    q.query, q.page, q.pageSize, q.order, data.aggr, q.platnyZaznam, q.includeNeplatne, 
                    q.logError, q.fixQuery, exactNumOfResults: q.exactNumOfResults
                    );
                //remove debug & more
                return ret;
            }

            private class FullSearchQuery
            {
                public string query;
                public int page;
                public int pageSize;
                public OrderResult order;

                public AggregationContainerDescriptor<Smlouva> anyAggregation = null;
                public bool? platnyZaznam = null;
                public bool includeNeplatne = false;
                public bool logError = true;
                public bool fixQuery = true;
                public bool exactNumOfResults = false;
            }

        }

    }
}