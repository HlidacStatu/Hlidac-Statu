using Devmasters;
using Devmasters.Crypto;
using Devmasters.DT;
using Devmasters.Enums;

using HlidacStatu.Entities;
using HlidacStatu.Repositories.ES;
using HlidacStatu.Repositories.Searching;
using HlidacStatu.Repositories.Searching.Rules;
using HlidacStatu.Util.Cache;

using Nest;

using Newtonsoft.Json;

using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace HlidacStatu.Repositories
{
    public static partial class SmlouvaRepo
    {
        public static class Searching
        {
            public const int DefaultPageSize = 40;


            [ShowNiceDisplayName()]
            [Sortable(SortableAttribute.SortAlgorithm.BySortValue)]
            public enum OrderResult
            {
                [SortValue(0)]
                [NiceDisplayName("podle relevance")]
                Relevance = 0,

                [SortValue(5)]
                [NiceDisplayName("nově zveřejněné první")]
                DateAddedDesc = 1,

                [NiceDisplayName("nově zveřejněné poslední")]
                [SortValue(6)]
                DateAddedAsc = 2,

                [SortValue(1)]
                [NiceDisplayName("nejlevnější první")]
                PriceAsc = 3,

                [SortValue(2)]
                [NiceDisplayName("nejdražší první")]
                PriceDesc = 4,

                [SortValue(7)]
                [NiceDisplayName("nově uzavřené první")]
                DateSignedDesc = 5,

                [NiceDisplayName("nově uzavřené poslední")]
                [SortValue(8)]
                DateSignedAsc = 6,

                [NiceDisplayName("nejvíce chybové první")]
                [SortValue(10)]
                ConfidenceDesc = 7,

                [NiceDisplayName("podle odběratele")]
                [SortValue(98)]
                CustomerAsc = 8,

                [NiceDisplayName("podle dodavatele")]
                [SortValue(99)]
                ContractorAsc = 9,

                [Disabled]
                ClassificationRelevance = 665,

                [Disabled]
                FastestForScroll = 666,
                [Disabled]
                LastUpdate = 667,

            }

            static string[] queryShorcuts = new string[] {
                "ico:",
                "osobaid:",
                "ds:",
                "dsprijemce:",
                "dsplatce:",
                "icoprijemce:",
                "icoplatce:",
                "jmenoprijemce:",
                "jmenoplatce:",
                "id:",
                "idverze:",
                "idsmlouvy:",
                "predmet:",
                "cislosmlouvy:",
                "mena:",
                "cenasdph:",
                "cenabezdph:",
                "cena:",
                "zverejneno:",
                "podepsano:",
                "schvalil:",
                "textsmlouvy:",
                "holding:",
                "holdingprijemce:",
                "holdingplatce:",
                "holdingdodavatel:",
                "holdingzadavatel:",
            };


            public static IRule[] Irules = new IRule[] {
               new OsobaId("osobaid:","ico:" ),
               new Holding("holdingprijemce:","icoprijemce:" ),
               new Holding("holdingplatce:","icoplatce:" ),
               new Holding("holdingdodavatel:","icoprijemce:" ),
               new Holding("holdingzadavatel:","icoplatce:" ),
               new Holding(null,"ico:" ),

               new TransformPrefixWithValue("ds:","(prijemce.datovaSchranka:${q} OR platce.datovaSchranka:${q}) ",null ),
               new TransformPrefix("dsprijemce:","prijemce.datovaSchranka:",null  ),
               new TransformPrefix("dsplatce:","platce.datovaSchranka:",null  ),
               new TransformPrefixWithValue("ico:","(prijemce.ico:${q} OR platce.ico:${q}) ",null ),
               new TransformPrefix("icoprijemce:","prijemce.ico:",null ),
               new TransformPrefix("icoplatce:","platce.ico:",null ),
               new TransformPrefix("jmenoprijemce:","prijemce.nazev:",null ),
               new TransformPrefix("jmenoplatce:","platce.nazev:",null ),
               new TransformPrefix("id:","id:",null ),
               new TransformPrefix("idverze:","id:",null ),
               new TransformPrefix("idsmlouvy:","identifikator.idSmlouvy:",null ),
               new TransformPrefix("predmet:","predmet:",null ),
               new TransformPrefix("cislosmlouvy:","cisloSmlouvy:",null ),
               new TransformPrefix("mena:","ciziMena.mena:",null ),
               new TransformPrefix("cenasdph:","hodnotaVcetneDph:",null ),
               new TransformPrefix("cenabezdph:","hodnotaBezDph:",null ),
               new TransformPrefix("cena:","calculatedPriceWithVATinCZK:",null ),
               new TransformPrefix("zverejneno:","casZverejneni:", "[<>]?[{\\[]+" ),
               new TransformPrefixWithValue("zverejneno:","casZverejneni:[${q} TO ${q}||+1d}", "\\d+" ),
               new TransformPrefix("podepsano:","datumUzavreni:", "[<>]?[{\\[]+" ),
               new TransformPrefixWithValue("podepsano:","datumUzavreni:[${q} TO ${q}||+1d}", "\\d+"  ),
               new TransformPrefix("schvalil:","schvalil:",null ),
               new TransformPrefix("textsmlouvy:","prilohy.plainTextContent:",null ),
               new Smlouva_Chyby(),
               new Smlouva_Oblast(1),
               new Smlouva_Oblast(2),
               new Smlouva_Oblasti(),

            };


            public static QueryContainer GetSimpleQuery(string query)
            {
                var qc = SimpleQueryCreator.GetSimpleQuery<Smlouva>(query, Irules);
                return qc;
            }


            static string regex = "[^/]*\r\n/(?<regex>[^/]*)/\r\n[^/]*\r\n";
            static RegexOptions options = ((RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline)
                                           | RegexOptions.IgnoreCase);
            static Regex regFindRegex = new Regex(regex, options);


            public static SmlouvaSearchResult SearchRaw(QueryContainer query, int page, int pageSize, OrderResult order,
        AggregationContainerDescriptor<Smlouva> anyAggregation = null,
        bool? platnyZaznam = null, bool includeNeplatne = false, bool logError = true, bool fixQuery = true,
        bool withHighlighting = false)
            {

                var result = new SmlouvaSearchResult()
                {
                    Page = page,
                    PageSize = pageSize,
                    OrigQuery = "",
                    Q = "",
                    Order = ((int)order).ToString()
                };

                ISearchResponse<Smlouva> res = _coreSearch(query, page, pageSize, order, anyAggregation, platnyZaznam,
                    includeNeplatne, logError, withHighlighting);


                if (res.IsValid == false && logError)
                    Manager.LogQueryError<Smlouva>(res, query.ToString());


                result.Total = res?.Total ?? 0;
                result.IsValid = res?.IsValid ?? false;
                result.ElasticResults = res;
                return result;
            }



            public static SmlouvaSearchResult SimpleSearch(string query, int page, int pageSize, string order,
AggregationContainerDescriptor<Smlouva> anyAggregation = null,
bool? platnyZaznam = null, bool includeNeplatne = false, bool logError = true, bool fixQuery = true,
bool withHighlighting = false, bool exactNumOfResults = false)
            {
                order = TextUtil.NormalizeToNumbersOnly(order);
                OrderResult eorder = OrderResult.Relevance;
                Enum.TryParse<OrderResult>(order, out eorder);

                return SimpleSearch(query, page, pageSize, eorder, anyAggregation,
                    platnyZaznam, includeNeplatne, logError, fixQuery,
                    withHighlighting, exactNumOfResults
                    );

            }
            public static SmlouvaSearchResult SimpleSearch(string query, int page, int pageSize, OrderResult order,
        AggregationContainerDescriptor<Smlouva> anyAggregation = null,
        bool? platnyZaznam = null, bool includeNeplatne = false, bool logError = true, bool fixQuery = true,
        bool withHighlighting = false, bool exactNumOfResults = false)
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
                    query = Tools.FixInvalidQuery(query, Irules, Tools.DefaultQueryOperators);
                    result.Q = query;
                }

                if (platnyZaznam.HasValue)
                    query = Query.ModifyQueryAND(query, "platnyZaznam:" + platnyZaznam.Value.ToString().ToLower());


                ISearchResponse<Smlouva> res =
                    _coreSearch(GetSimpleQuery(query), page, pageSize, order, anyAggregation, platnyZaznam,
                    includeNeplatne, logError, withHighlighting, exactNumOfResults);

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


            private static ISearchResponse<Smlouva> _coreSearch(QueryContainer query, int page, int pageSize,
                OrderResult order,
                AggregationContainerDescriptor<Smlouva> anyAggregation = null,
                bool? platnyZaznam = null, bool includeNeplatne = false, bool logError = true,
                bool withHighlighting = false, bool exactNumOfResults = false)
            {
                page = page - 1;
                if (page < 0)
                    page = 0;

                if (page * pageSize >= Tools.MaxResultWindow) //elastic limit
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

                    res = client
                        .Search<Smlouva>(s => s
                            .Index(indexes)
                            .Size(pageSize)
                            .From(page * pageSize)
                            .Query(q => query)
                            .Source(m => m.Excludes(e => e.Field(o => o.Prilohy)))
                            .Sort(ss => GetSort(order))
                            .Aggregations(aggrFunc)
                            .Highlight(h => Tools.GetHighlight<Smlouva>(withHighlighting))
                            .TrackTotalHits(exactNumOfResults || page * pageSize == 0 ? true : (bool?)null)
                    );
                    if (res != null && res.IsValid == false && res.ServerError?.Status == 429)
                    {
                        Thread.Sleep(100);
                        res = client
                            .Search<Smlouva>(s => s
                                .Index(indexes)
                                .Size(pageSize)
                                .From(page * pageSize)
                                .Query(q => query)
                                .Source(m => m.Excludes(e => e.Field(o => o.Prilohy)))
                                .Sort(ss => GetSort(order))
                                .Aggregations(aggrFunc)
                                .Highlight(h => Tools.GetHighlight<Smlouva>(withHighlighting))
                                .TrackTotalHits(exactNumOfResults || page * pageSize == 0 ? true : (bool?)null)
                        );
                        if (res.IsValid == false && res.ServerError?.Status == 429)
                        {
                            Thread.Sleep(200);
                            res = client
                                .Search<Smlouva>(s => s
                                    .Index(indexes)
                                    .Size(pageSize)
                                    .From(page * pageSize)
                                    .Query(q => query)
                                    .Source(m => m.Excludes(e => e.Field(o => o.Prilohy)))
                                    .Sort(ss => GetSort(order))
                                    .Aggregations(aggrFunc)
                                    .Highlight(h => Tools.GetHighlight<Smlouva>(withHighlighting))
                                    .TrackTotalHits(exactNumOfResults || page * pageSize == 0 ? true : (bool?)null)
                            );

                        }

                    }

                    if (withHighlighting && res.Shards != null && res.Shards.Failed > 0) //if some error, do it again without highlighting
                    {
                        res = client
                            .Search<Smlouva>(s => s
                                .Index(indexes)
                                .Size(pageSize)
                                .From(page * pageSize)
                                .Query(q => query)
                                .Source(m => m.Excludes(e => e.Field(o => o.Prilohy)))
                                .Sort(ss => GetSort(order))
                                .Aggregations(aggrFunc)
                                .Highlight(h => Tools.GetHighlight<Smlouva>(false))
                                .TrackTotalHits(exactNumOfResults || page * pageSize == 0 ? true : (bool?)null)
                        );
                    }
                }
                catch (Exception e)
                {

                    if (res != null && res.ServerError != null)
                        Manager.LogQueryError<Smlouva>(res);
                    else
                        Util.Consts.Logger.Error("", e);
                    throw;
                }

                if (res.IsValid == false && logError)
                    Manager.LogQueryError<Smlouva>(res);

                return res;
            }


            public static ISearchResponse<Smlouva> RawSearch(string jsonQuery, int page, int pageSize, OrderResult order = OrderResult.Relevance,
                AggregationContainerDescriptor<Smlouva> anyAggregation = null, bool? platnyZaznam = null,
                bool includeNeplatne = false, bool exactNumOfResults = false
                )
            {
                return RawSearch(Tools.GetRawQuery(jsonQuery), page, pageSize, order, anyAggregation, platnyZaznam, includeNeplatne,
                    exactNumOfResults: exactNumOfResults);
            }
            public static ISearchResponse<Smlouva> RawSearch(QueryContainer query, int page, int pageSize, OrderResult order = OrderResult.Relevance,
                AggregationContainerDescriptor<Smlouva> anyAggregation = null, bool? platnyZaznam = null,
                bool includeNeplatne = false,
                bool withHighlighting = false, bool exactNumOfResults = false
                )
            {
                var res = _coreSearch(query, page, pageSize, order, anyAggregation, platnyZaznam: platnyZaznam, includeNeplatne: includeNeplatne, logError: true, withHighlighting: withHighlighting, exactNumOfResults: exactNumOfResults);
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

            public static MemoryCacheManager<SmlouvaSearchResult, (string query, AggregationContainerDescriptor<Smlouva> aggr)> cachedSearches = 
                new MemoryCacheManager<SmlouvaSearchResult, (string query, AggregationContainerDescriptor<Smlouva> aggr)>(
                    "SMLOUVYsearch", funcSimpleSearch, TimeSpan.FromHours(24));

            //
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
            private static SmlouvaSearchResult funcSimpleSearch((string query, AggregationContainerDescriptor<Smlouva> aggr) data)
            {
                var q = JsonConvert.DeserializeObject<FullSearchQuery>(data.query);
                var ret = SimpleSearch(
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