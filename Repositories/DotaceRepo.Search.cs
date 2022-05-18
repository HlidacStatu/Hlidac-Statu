using Devmasters;
using Devmasters.DT;
using HlidacStatu.Entities;
using HlidacStatu.Entities.Dotace;
using HlidacStatu.Repositories.ES;
using HlidacStatu.Repositories.Searching;
using HlidacStatu.Repositories.Searching.Rules;
using Nest;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace HlidacStatu.Repositories
{
    public static partial class DotaceRepo
    {
        public static class Searching
        {
            public static readonly string[] QueryOperators = new string[] { "AND", "OR" };


            public static readonly IRule[] Irules = new IRule[]
            {
                new OsobaId("osobaid:", "ico:"),
                new Holding(null, "ico:"),
                //(prijemce.jmeno:${q} OR prijemce.obchodniJmeno:${q})
                new TransformPrefix("ico:", "prijemce.ico:", null),
                new TransformPrefixWithValue("jmeno:", "(prijemce.hlidacJmeno:${q} OR prijemce.obchodniJmeno:${q})",
                    null),
                new TransformPrefix("projekt:", "nazevProjektu:", null),
                new TransformPrefix("kod:", "kodProjektu:", null),
                new TransformPrefix("castka:", "dotaceCelkem:", null),
                new TransformPrefixWithValue("cena:", "dotaceCelkem:<=${q} ", "<=\\d"),
                new TransformPrefixWithValue("cena:", "dotaceCelkem:>=${q} ", ">=\\d"),
                new TransformPrefixWithValue("cena:", "dotaceCelkem:<${q} ", "<\\d"),
                new TransformPrefixWithValue("cena:", "dotaceCelkem:>${q} ", ">\\d"),
                new TransformPrefixWithValue("cena:", "dotaceCelkem:${q} ", null),
            };

            public static string[] QueryShorcuts()
            {
                return Irules.SelectMany(m => m.Prefixes).Distinct().ToArray();
            }

            public static QueryContainer GetSimpleQuery(string query)
            {
                return GetSimpleQuery(new DotaceSearchResult() { Q = query, Page = 1 });
            }

            public static QueryContainer GetSimpleQuery(DotaceSearchResult searchdata)
            {
                var query = searchdata.Q;

                var qc = SimpleQueryCreator.GetSimpleQuery<Dotace>(query, Irules);

                return qc;
            }

            public static Task<DotaceSearchResult> SimpleSearchAsync(string query, int page, int pagesize,
                DotaceSearchResult.DotaceOrderResult order,
                bool withHighlighting = false,
                AggregationContainerDescriptor<Dotace> anyAggregation = null, bool exactNumOfResults = false)
                => SimpleSearchAsync(query, page, pagesize, ((int)order).ToString(),
                    withHighlighting,
                    anyAggregation, exactNumOfResults);

            public static Task<DotaceSearchResult> SimpleSearchAsync(string query, int page, int pagesize, string order,
                bool withHighlighting = false,
                AggregationContainerDescriptor<Dotace> anyAggregation = null, bool exactNumOfResults = false)
                => SimpleSearchAsync(new DotaceSearchResult()
                {
                    Q = query,
                    Page = page,
                    PageSize = pagesize,
                    Order = TextUtil.NormalizeToNumbersOnly(order),
                    ExactNumOfResults = exactNumOfResults
                }, withHighlighting, anyAggregation);

            public static async Task<DotaceSearchResult> SimpleSearchAsync(DotaceSearchResult search,
                bool withHighlighting = false,
                AggregationContainerDescriptor<Dotace> anyAggregation = null)
            {
                var page = search.Page - 1 < 0 ? 0 : search.Page - 1;

                var sw = new StopWatchEx();
                sw.Start();
                search.OrigQuery = search.Q;
                search.Q = Tools.FixInvalidQuery(search.Q ?? "", QueryShorcuts(), QueryOperators);

                ISearchResponse<Dotace> res = null;
                try
                {
                    var client = await Manager.GetESClient_DotaceAsync();
                    res = await client.SearchAsync<Dotace>(s => s
                            .Size(search.PageSize)
                            .ExpandWildcards(Elasticsearch.Net.ExpandWildcards.All)
                            .From(page * search.PageSize)
                            .Query(q => GetSimpleQuery(search))
                            .Sort(ss => GetSort(search.Order))
                            .Highlight(h => Tools.GetHighlight<Dotace>(withHighlighting))
                            .Aggregations(aggr => anyAggregation)
                            .TrackTotalHits((search.ExactNumOfResults || page * search.PageSize == 0)
                                ? true
                                : (bool?)null)
                        );
                    if (res.IsValid && withHighlighting &&
                        res.Shards.Failed > 0) //if some error, do it again without highlighting
                    {
                        res = await client.SearchAsync<Dotace>(s => s
                                .Size(search.PageSize)
                                .ExpandWildcards(Elasticsearch.Net.ExpandWildcards.All)
                                .From(page * search.PageSize)
                                .Query(q => GetSimpleQuery(search))
                                .Sort(ss => GetSort(search.Order))
                                .Highlight(h => Tools.GetHighlight<Dotace>(false))
                                .Aggregations(aggr => anyAggregation)
                                .TrackTotalHits(search.ExactNumOfResults || page * search.PageSize == 0
                                    ? true
                                    : (bool?)null)
                            );
                    }
                }
                catch (Exception e)
                {
                    AuditRepo.Add(Audit.Operations.Search, "", "", "Dotace", "error", search.Q, null);
                    if (res != null && res.ServerError != null)
                    {
                        Manager.LogQueryError<Dotace>(res, "Exception, Orig query:"
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

                AuditRepo.Add(Audit.Operations.Search, "", "", "Dotace", res.IsValid ? "valid" : "invalid", search.Q,
                    null);

                if (res.IsValid == false)
                {
                    Manager.LogQueryError<Dotace>(res, "Exception, Orig query:"
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

            public static SortDescriptor<Dotace> GetSort(string sorder)
            {
                DotaceSearchResult.DotaceOrderResult order = DotaceSearchResult.DotaceOrderResult.Relevance;
                Enum.TryParse<DotaceSearchResult.DotaceOrderResult>(sorder, out order);
                return GetSort(order);
            }

            public static SortDescriptor<Dotace> GetSort(DotaceSearchResult.DotaceOrderResult order)
            {
                SortDescriptor<Dotace> s = new SortDescriptor<Dotace>().Field(f => f.Field("_score").Descending());
                switch (order)
                {
                    case DotaceSearchResult.DotaceOrderResult.DateAddedDesc:
                        s = new SortDescriptor<Dotace>().Field(m => m.Field(f => f.DatumPodpisu).Descending());
                        break;
                    case DotaceSearchResult.DotaceOrderResult.DateAddedAsc:
                        s = new SortDescriptor<Dotace>().Field(m => m.Field(f => f.DatumPodpisu).Ascending());
                        break;
                    case DotaceSearchResult.DotaceOrderResult.LatestUpdateDesc:
                        s = new SortDescriptor<Dotace>().Field(m => m.Field(f => f.DotaceCelkem).Descending());
                        break;
                    case DotaceSearchResult.DotaceOrderResult.LatestUpdateAsc:
                        s = new SortDescriptor<Dotace>().Field(m => m.Field(f => f.DotaceCelkem).Ascending());
                        break;
                    case DotaceSearchResult.DotaceOrderResult.FastestForScroll:
                        s = new SortDescriptor<Dotace>().Field(f => f.Field("_doc"));
                        break;
                    case DotaceSearchResult.DotaceOrderResult.ICODesc:
                        s = new SortDescriptor<Dotace>().Field(m => m.Field(f => f.Prijemce.Ico).Descending());
                        break;
                    case DotaceSearchResult.DotaceOrderResult.ICOAsc:
                        s = new SortDescriptor<Dotace>().Field(m => m.Field(f => f.Prijemce.Ico).Ascending());
                        break;
                    case DotaceSearchResult.DotaceOrderResult.Relevance:
                    default:
                        break;
                }

                return s;
            }
        }
    }
}