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

namespace HlidacStatu.Repositories
{
    public static partial class DotaceRepo
    {
        public static class Searching
        {
            public static readonly string[] QueryOperators = new string[] { "AND", "OR" };

            public static readonly IRule[] Irules = new IRule[]
            {
                new OsobaId(HlidacStatu.Repositories.OsobaVazbyRepo.Icos_s_VazbouNaOsobu, "osobaid:", "ico:"),
                new Holding(HlidacStatu.Repositories.FirmaVazbyRepo.IcosInHolding, null, "ico:"),
                //(prijemce.jmeno:${q} OR prijemce.obchodniJmeno:${q})
                new TransformPrefixWithValue("ico:", "(recipient.ico:${q} OR subsidyProviderIco:${q})",null),
                new TransformPrefixWithValue("jmeno:", "(recipient.name:${q} OR recipient.hlidacName:${q})",null),
                new TransformPrefix("rok:", "approvedYear:", null),
                new TransformPrefix("projekt:", "projectName:", null),
                new TransformPrefix("program:", "programName.keyword:", null),
                new TransformPrefix("kodProgramu:", "programCode:", null),
                new TransformPrefix("kodProjektu:", "projectCode:", null),
                new TransformPrefix("castka:", "assumedAmount:", null),
                new TransformPrefixWithValue("cena:", "assumedAmount:<=${q} ", "<=\\d"),
                new TransformPrefixWithValue("cena:", "assumedAmount:>=${q} ", ">=\\d"),
                new TransformPrefixWithValue("cena:", "assumedAmount:<${q} ", "<\\d"),
                new TransformPrefixWithValue("cena:", "assumedAmount:>${q} ", ">\\d"),
                new TransformPrefixWithValue("cena:", "assumedAmount:${q} ", null),
                new HlidacStatu.Searching.Dotace_Oblast(),
                new HlidacStatu.Searching.Dotace_Typ(),
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
                AggregationContainerDescriptor<Dotace> anyAggregation = null, bool exactNumOfResults = false, CancellationToken cancellationToken = default)
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
                AggregationContainerDescriptor<Dotace> anyAggregation = null, CancellationToken cancellationToken = default)
            {
                var page = search.Page - 1 < 0 ? 0 : search.Page - 1;

                var sw = new StopWatchEx();
                sw.Start();
                search.OrigQuery = search.Q;
                search.Q = HlidacStatu.Searching.Tools.FixInvalidQuery(search.Q ?? "", QueryShorcuts(), QueryOperators);

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
                            .Highlight(h => Repositories.Searching.Tools.GetHighlight<Dotace>(withHighlighting))
                            .Aggregations(aggr => anyAggregation)
                            .TrackTotalHits((search.ExactNumOfResults || page * search.PageSize == 0)
                                ? true
                                : (bool?)null)
                            , cancellationToken
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
                                .Highlight(h => Repositories.Searching.Tools.GetHighlight<Dotace>(false))
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
                        Logger.Error(e, "");
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
                        s = new SortDescriptor<Dotace>().Field(m => m.Field(f => f.ApprovedYear).Descending());
                        break;
                    case DotaceSearchResult.DotaceOrderResult.DateAddedAsc:
                        s = new SortDescriptor<Dotace>().Field(m => m.Field(f => f.ApprovedYear).Ascending());
                        break;
                    case DotaceSearchResult.DotaceOrderResult.AmountDesc:
                        s = new SortDescriptor<Dotace>().Field(m => m.Field(f => f.AssumedAmount).Descending());
                        break;
                    case DotaceSearchResult.DotaceOrderResult.AmountAsc:
                        s = new SortDescriptor<Dotace>().Field(m => m.Field(f => f.AssumedAmount).Ascending());
                        break;
                    case DotaceSearchResult.DotaceOrderResult.FastestForScroll:
                        s = new SortDescriptor<Dotace>().Field(f => f.Field("_doc"));
                        break;
                    case DotaceSearchResult.DotaceOrderResult.ICODesc:
                        s = new SortDescriptor<Dotace>().Field(m => m.Field(f => f.Recipient.Ico).Descending());
                        break;
                    case DotaceSearchResult.DotaceOrderResult.ICOAsc:
                        s = new SortDescriptor<Dotace>().Field(m => m.Field(f => f.Recipient.Ico).Ascending());
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