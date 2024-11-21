using Devmasters;
using Devmasters.DT;
using HlidacStatu.Entities;
using HlidacStatu.Repositories.Searching;
using HlidacStatu.Searching;
using Nest;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HlidacStatu.Connectors;
using HlidacStatu.Entities.Entities;

namespace HlidacStatu.Repositories
{
    public static partial class SubsidyRepo
    {
        public static class Searching
        {
            public static readonly string[] QueryOperators = new string[] { "AND", "OR" };
            
            public static QueryContainer AddIsNotHiddenRule(QueryContainer query)
            {
                // Create a query for `isHidden = false`
                var isHiddenQuery = new TermQuery
                {
                    Field = "isHidden",
                    Value = false
                };

                // Combine the original query with the `isHidden` rule
                return query == null
                    ? isHiddenQuery
                    : new BoolQuery
                    {
                        Must = new QueryContainer[] { query, isHiddenQuery }
                    };
            }


            public static readonly IRule[] Irules = new IRule[]
            {
                new OsobaId(HlidacStatu.Repositories.OsobaVazbyRepo.Icos_s_VazbouNaOsobu, "osobaid:", "ico:"),
                new Holding(HlidacStatu.Repositories.FirmaVazbyRepo.IcosInHolding, null, "ico:"),
                //(prijemce.jmeno:${q} OR prijemce.obchodniJmeno:${q})
                new TransformPrefix("ico:", "common.recipient.ico:", null),
                new TransformPrefixWithValue("jmeno:", "(common.recipient.name:${q} OR common.recipient.hlidacName:${q})",
                    null),
                new TransformPrefix("projekt:", "common.projectName:", null),
                new TransformPrefix("kodProjektu:", "common.projectCode:", null),
                new TransformPrefix("castka:", "assumedAmount:", null),
                new TransformPrefixWithValue("cena:", "assumedAmount:<=${q} ", "<=\\d"),
                new TransformPrefixWithValue("cena:", "assumedAmount:>=${q} ", ">=\\d"),
                new TransformPrefixWithValue("cena:", "assumedAmount:<${q} ", "<\\d"),
                new TransformPrefixWithValue("cena:", "assumedAmount:>${q} ", ">\\d"),
                new TransformPrefixWithValue("cena:", "assumedAmount:${q} ", null),
            };

            public static string[] QueryShorcuts()
            {
                return Irules.SelectMany(m => m.Prefixes).Distinct().ToArray();
            }

            public static QueryContainer GetSimpleQuery(string query)
            {
                return GetSimpleQuery(new SubsidySearchResult() { Q = query, Page = 1 });
            }

            public static QueryContainer GetSimpleQuery(SubsidySearchResult searchdata)
            {
                var query = searchdata.Q;

                var qc = SimpleQueryCreator.GetSimpleQuery<Subsidy>(query, Irules);
                qc = AddIsNotHiddenRule(qc);

                return qc;
            }

            public static Task<SubsidySearchResult> SimpleSearchAsync(string query, int page, int pagesize,
                SubsidySearchResult.DotaceOrderResult order,
                bool withHighlighting = false,
                AggregationContainerDescriptor<Subsidy> anyAggregation = null, bool exactNumOfResults = false)
                => SimpleSearchAsync(query, page, pagesize, ((int)order).ToString(),
                    withHighlighting,
                    anyAggregation, exactNumOfResults);

            public static Task<SubsidySearchResult> SimpleSearchAsync(string query, int page, int pagesize, string order,
                bool withHighlighting = false,
                AggregationContainerDescriptor<Subsidy> anyAggregation = null, bool exactNumOfResults = false, CancellationToken cancellationToken = default)
                => SimpleSearchAsync(new SubsidySearchResult()
                {
                    Q = query,
                    Page = page,
                    PageSize = pagesize,
                    Order = TextUtil.NormalizeToNumbersOnly(order),
                    ExactNumOfResults = exactNumOfResults
                }, withHighlighting, anyAggregation);

            public static async Task<SubsidySearchResult> SimpleSearchAsync(SubsidySearchResult search,
                bool withHighlighting = false,
                AggregationContainerDescriptor<Subsidy> anyAggregation = null, CancellationToken cancellationToken = default)
            {
                var page = search.Page - 1 < 0 ? 0 : search.Page - 1;

                var sw = new StopWatchEx();
                sw.Start();
                search.OrigQuery = search.Q;
                search.Q = HlidacStatu.Searching.Tools.FixInvalidQuery(search.Q ?? "", QueryShorcuts(), QueryOperators);

                ISearchResponse<Subsidy> res = null;
                try
                {
                    var client = await Manager.GetESClient_SubsidyAsync();
                    res = await client.SearchAsync<Subsidy>(s => s
                            .Size(search.PageSize)
                            .ExpandWildcards(Elasticsearch.Net.ExpandWildcards.All)
                            .From(page * search.PageSize)
                            .Query(q => GetSimpleQuery(search))
                            .Sort(ss => GetSort(search.Order))
                            .Highlight(h => Repositories.Searching.Tools.GetHighlight<Subsidy>(withHighlighting))
                            .Aggregations(aggr => anyAggregation)
                            .TrackTotalHits((search.ExactNumOfResults || page * search.PageSize == 0)
                                ? true
                                : (bool?)null)
                            ,cancellationToken
                        );
                    if (res.IsValid && withHighlighting &&
                        res.Shards.Failed > 0) //if some error, do it again without highlighting
                    {
                        res = await client.SearchAsync<Subsidy>(s => s
                                .Size(search.PageSize)
                                .ExpandWildcards(Elasticsearch.Net.ExpandWildcards.All)
                                .From(page * search.PageSize)
                                .Query(q => GetSimpleQuery(search))
                                .Sort(ss => GetSort(search.Order))
                                .Highlight(h => Repositories.Searching.Tools.GetHighlight<Subsidy>(false))
                                .Aggregations(aggr => anyAggregation)
                                .TrackTotalHits(search.ExactNumOfResults || page * search.PageSize == 0
                                    ? true
                                    : (bool?)null), cancellationToken
                            );
                    }
                }
                catch (Exception e)
                {
                    if (e.Message == "A task was canceled.")
                        throw;
                    
                    AuditRepo.Add(Audit.Operations.Search, "", "", "Subsidy", "error", search.Q, null);
                    if (res != null && res.ServerError != null)
                    {
                        Manager.LogQueryError<Subsidy>(res, "Exception, Orig query:"
                                                           + search.OrigQuery + "   query:"
                                                           + search.Q
                                                           + "\n\n res:" + search.ElasticResults.ToString()
                            , ex: e);
                    }
                    else
                    {
                        Logger.Error(e, "");
                    }

                    throw;
                }

                sw.Stop();

                AuditRepo.Add(Audit.Operations.Search, "", "", "Subsidy", res.IsValid ? "valid" : "invalid", search.Q,
                    null);

                if (res.IsValid == false)
                {
                    Manager.LogQueryError<Subsidy>(res, "Exception, Orig query:"
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

            public static SortDescriptor<Subsidy> GetSort(string sorder)
            {
                SubsidySearchResult.DotaceOrderResult order = SubsidySearchResult.DotaceOrderResult.Relevance;
                Enum.TryParse<SubsidySearchResult.DotaceOrderResult>(sorder, out order);
                return GetSort(order);
            }

            public static SortDescriptor<Subsidy> GetSort(SubsidySearchResult.DotaceOrderResult order)
            {
                SortDescriptor<Subsidy> s = new SortDescriptor<Subsidy>().Field(f => f.Field("_score").Descending());
                switch (order)
                {
                    case SubsidySearchResult.DotaceOrderResult.DateAddedDesc:
                        s = new SortDescriptor<Subsidy>().Field(m => m.Field(f => f.Common.ApprovedYear).Descending());
                        break;
                    case SubsidySearchResult.DotaceOrderResult.DateAddedAsc:
                        s = new SortDescriptor<Subsidy>().Field(m => m.Field(f => f.Common.ApprovedYear).Ascending());
                        break;
                    case SubsidySearchResult.DotaceOrderResult.LatestUpdateDesc:
                        s = new SortDescriptor<Subsidy>().Field(m => m.Field(f => f.AssumedAmount).Descending());
                        break;
                    case SubsidySearchResult.DotaceOrderResult.LatestUpdateAsc:
                        s = new SortDescriptor<Subsidy>().Field(m => m.Field(f => f.AssumedAmount).Ascending());
                        break;
                    case SubsidySearchResult.DotaceOrderResult.FastestForScroll:
                        s = new SortDescriptor<Subsidy>().Field(f => f.Field("_doc"));
                        break;
                    case SubsidySearchResult.DotaceOrderResult.ICODesc:
                        s = new SortDescriptor<Subsidy>().Field(m => m.Field(f => f.Common.Recipient.Ico).Descending());
                        break;
                    case SubsidySearchResult.DotaceOrderResult.ICOAsc:
                        s = new SortDescriptor<Subsidy>().Field(m => m.Field(f => f.Common.Recipient.Ico).Ascending());
                        break;
                    case SubsidySearchResult.DotaceOrderResult.Relevance:
                    default:
                        break;
                }

                return s;
            }
        }
    }
}