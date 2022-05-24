using HlidacStatu.Entities;
using HlidacStatu.Entities.Insolvence;
using HlidacStatu.Repositories.ES;
using HlidacStatu.Repositories.Searching;
using HlidacStatu.Repositories.Searching.Rules;

using Nest;

using System;
using System.Threading.Tasks;

namespace HlidacStatu.Repositories
{
    public static partial class InsolvenceRepo
    {
        public static class Searching
        {
            static string[] queryShorcuts = new string[]
            {
                "ico:",
                "icodluznik:",
                "icoveritel:",
                "icospravce:",
                "jmeno:",
                "jmenodluznik:",
                "jmenoveritel:",
                "jmenospravce:",
                "spisovaznacka:",
                "zmeneno:",
                "zahajeno:",
                "stav:",
                "text:",
                "typdokumentu:", "dokumenttyp:",
                "osobaid:", "holding:",
                "osobaiddluznik:", "holdingdluznik:",
                "osobaidveritel:", "holdingveritel:",
                "osobaidspravce:", "holdingspravce:",
                "id:"
            };

            static string[] queryOperators = new string[] { "AND", "OR" };

            public static QueryContainer GetSimpleQuery(string query)
            {
                return GetSimpleQuery(new InsolvenceSearchResult() { Q = query, Page = 1 });
            }

            public static QueryContainer GetSimpleQuery(InsolvenceSearchResult searchdata)
            {
                var query = searchdata.Q;

                IRule[] irules = new IRule[]
                {
                    new OsobaId("osobaid:", "ico:"),
                    new OsobaId("osobaiddluznik:", "icodluznik:"),
                    new OsobaId("osobaidveritel:", "icoveritel:"),
                    new OsobaId("osobaidspravce:", "icospravce:"),

                    new Holding("holding:", "ico:"),
                    new Holding("holdindluznik:", "icoplatce:"),
                    new Holding("holdingveritel:", "icoveritel:"),
                    new Holding("holdingspravce:", "icospravce:"),

                    new TransformPrefixWithValue("ico:",
                        "(dluznici.iCO:${q} OR veritele.iCO:${q} OR spravci.iCO:${q}) ", null),
                    new TransformPrefixWithValue("jmeno:",
                        "(dluznici.plneJmeno:${q} OR veritele.plneJmeno:${q} OR spravci.plneJmeno:${q})", null),

                    new TransformPrefix("icodluznik:", "dluznici.iCO:", null),
                    new TransformPrefix("icoveritel:", "veritele.iCO:", null),
                    new TransformPrefix("icospravce:", "spravci.iCO:", null),
                    new TransformPrefix("jmenodluznik:", "dluznici.plneJmeno:", null),
                    new TransformPrefix("jmenoveritel:", "veritele.plneJmeno:", null),
                    new TransformPrefix("jmenospravce:", "spravci.plneJmeno:", null),
                    new TransformPrefix("spisovaznacka:", "spisovaZnacka:", null),
                    new TransformPrefix("id:", "spisovaZnacka:", null),

                    new TransformPrefix("zmeneno:", "posledniZmena:", "[<>]?[{\\[]+"),
                    new TransformPrefixWithValue("zmeneno:", "posledniZmena:[${q} TO ${q}||+1d}", "\\d+"),
                    new TransformPrefix("zahajeno:", "datumZalozeni:", "[<>]?[{\\[]+"),
                    new TransformPrefixWithValue("zahajeno:", "datumZalozeni:[${q} TO ${q}||+1d}", "\\d+"),

                    new TransformPrefix("stav:", "stav:", null),
                    new TransformPrefix("text:", "dokumenty.plainText:", null),
                    new TransformPrefix("texttypdokumentu:", "dokumenty.popis:", null),
                    new TransformPrefix("typdokumentu:", "dokumenty.typUdalosti:", null),
                    new TransformPrefix("oddil:", "dokumenty.oddil:", null),
                };


                string modifiedQ = query; // Search.Tools.FixInvalidQuery(query, queryShorcuts, queryOperators) ?? "";
                //check invalid query ( tag: missing value)

                if (searchdata.LimitedView)
                    modifiedQ = Query.ModifyQueryAND(modifiedQ, "onRadar:true");

                //var qc = Lib.Search.Tools.GetSimpleQuery<Rizeni>(modifiedQ, rules); ;
                var qc = SimpleQueryCreator.GetSimpleQuery<Rizeni>(modifiedQ, irules);

                return qc;
            }


            public static Task<InsolvenceSearchResult> SimpleSearchAsync(string query, int page, int pagesize, int order,
                bool withHighlighting = false,
                bool limitedView = true,
                AggregationContainerDescriptor<Rizeni> anyAggregation = null, bool exactNumOfResults = false) 
                => SimpleSearchAsync(new InsolvenceSearchResult()
                {
                    Q = query,
                    Page = page,
                    PageSize = pagesize,
                    LimitedView = limitedView,
                    Order = order.ToString(),
                    ExactNumOfResults = exactNumOfResults
                }, withHighlighting, anyAggregation);

            public static async Task<InsolvenceSearchResult> SimpleSearchAsync(InsolvenceSearchResult search,
                bool withHighlighting = false,
                AggregationContainerDescriptor<Rizeni> anyAggregation = null, bool exactNumOfResults = false)
            {
                var client = await Manager.GetESClient_InsolvenceAsync();
                var page = search.Page - 1 < 0 ? 0 : search.Page - 1;

                var sw = new Devmasters.DT.StopWatchEx();
                sw.Start();
                search.OrigQuery = search.Q;
                search.Q = Tools.FixInvalidQuery(search.Q ?? "", queryShorcuts, queryOperators);
                var sq = GetSimpleQuery(search);

                ISearchResponse<Rizeni> res = null;
                try
                {
                    res = await client
                        .SearchAsync<Rizeni>(s => s
                            .Size(search.PageSize)
                            .ExpandWildcards(Elasticsearch.Net.ExpandWildcards.All)
                            .From(page * search.PageSize)
                            .Source(sr => sr.Excludes(r => r.Fields("dokumenty.plainText")))
                            .Query(q => sq)
                            //.Sort(ss => new SortDescriptor<Rizeni>().Field(m => m.Field(f => f.PosledniZmena).Descending()))
                            .Sort(ss => GetSort(search.Order))
                            .Highlight(h => Tools.GetHighlight<Rizeni>(withHighlighting))
                            .Aggregations(aggr => anyAggregation)
                            .TrackTotalHits(search.ExactNumOfResults || page * search.PageSize == 0
                                ? true
                                : (bool?)null)
                        );
                    if (withHighlighting && res.Shards != null &&
                        res.Shards.Failed > 0) //if some error, do it again without highlighting
                    {
                        res = await client
                            .SearchAsync<Rizeni>(s => s
                                .Size(search.PageSize)
                                .ExpandWildcards(Elasticsearch.Net.ExpandWildcards.All)
                                .From(page * search.PageSize)
                                .Source(sr => sr.Excludes(r => r.Fields("dokumenty.plainText")))
                                .Query(q => sq)
                                //.Sort(ss => new SortDescriptor<Rizeni>().Field(m => m.Field(f => f.PosledniZmena).Descending()))
                                .Sort(ss => GetSort(search.Order))
                                .Highlight(h => Tools.GetHighlight<Rizeni>(false))
                                .Aggregations(aggr => anyAggregation)
                                .TrackTotalHits(search.ExactNumOfResults || page * search.PageSize == 0
                                    ? true
                                    : (bool?)null)
                            );
                    }
                }
                catch (Exception e)
                {
                    AuditRepo.Add(Audit.Operations.Search, "", "", "Insolvence", "error", search.Q, null);
                    if (res != null && res.ServerError != null)
                    {
                        Manager.LogQueryError<Rizeni>(res, "Exception, Orig query:"
                                                           + search.OrigQuery + "   query:"
                                                           + search.Q
                                                           + "\n\n res:" + search.ElasticResults.ToString()
                            , ex: e);
                    }
                    else
                    {
                        Util.Consts.Logger.Error("", e);
                    }

                    throw;
                }

                sw.Stop();
                AuditRepo.Add(Audit.Operations.Search, "", "", "Insolvence", res.IsValid ? "valid" : "invalid",
                    search.Q, null);

                if (res.IsValid == false)
                {
                    Manager.LogQueryError<Rizeni>(res, "Exception, Orig query:"
                                                       + search.OrigQuery + "   query:"
                                                       + search.Q
                                                       + "\n\n res:" + search.ElasticResults?.ToString()
                    );
                }

                search.Total = res?.Total ?? 0;
                search.IsValid = res?.IsValid ?? false;
                search.ElasticResults = res;
                search.ElapsedTime = sw.Elapsed;
                return search;
            }

            public static SortDescriptor<Rizeni> GetSort(string sorder)
            {
                InsolvenceSearchResult.InsolvenceOrderResult order = InsolvenceSearchResult.InsolvenceOrderResult
                    .Relevance;
                Enum.TryParse<InsolvenceSearchResult.InsolvenceOrderResult>(sorder, out order);
                return GetSort(order);
            }

            public static SortDescriptor<Rizeni> GetSort(InsolvenceSearchResult.InsolvenceOrderResult order)
            {
                SortDescriptor<Rizeni> s = new SortDescriptor<Rizeni>().Field(f => f.Field("_score").Descending());
                switch (order)
                {
                    case InsolvenceSearchResult.InsolvenceOrderResult.DateAddedDesc:
                        s = new SortDescriptor<Rizeni>().Field(m => m.Field(f => f.DatumZalozeni).Descending());
                        break;
                    case InsolvenceSearchResult.InsolvenceOrderResult.DateAddedAsc:
                        s = new SortDescriptor<Rizeni>().Field(m => m.Field(f => f.DatumZalozeni).Ascending());
                        break;
                    case InsolvenceSearchResult.InsolvenceOrderResult.LatestUpdateDesc:
                        s = new SortDescriptor<Rizeni>().Field(m => m.Field(f => f.PosledniZmena).Descending());
                        break;
                    case InsolvenceSearchResult.InsolvenceOrderResult.LatestUpdateAsc:
                        s = new SortDescriptor<Rizeni>().Field(m => m.Field(f => f.PosledniZmena).Ascending());
                        break;
                    case InsolvenceSearchResult.InsolvenceOrderResult.FastestForScroll:
                        s = new SortDescriptor<Rizeni>().Field(f => f.Field("_doc"));
                        break;
                    case InsolvenceSearchResult.InsolvenceOrderResult.Relevance:
                    default:
                        break;
                }

                return s;
            }
        }
    }
}