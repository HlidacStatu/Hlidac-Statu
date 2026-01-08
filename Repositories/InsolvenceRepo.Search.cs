using HlidacStatu.Connectors;
using HlidacStatu.Entities;
using HlidacStatu.Entities.Insolvence;
using HlidacStatu.Repositories.Searching;
using HlidacStatu.Searching;
using Nest;
using System;
using System.Threading.Tasks;
using Serilog;

namespace HlidacStatu.Repositories
{
    public static partial class InsolvenceRepo
    {
        private static readonly ILogger _logger = Log.ForContext(typeof(InsolvenceRepo));

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

            public static Task<QueryContainer> GetSimpleQueryAsync(string query)
            {
                return GetSimpleQueryAsync(new InsolvenceSearchResult() { Q = query, Page = 1 });
            }

            public static async Task<QueryContainer> GetSimpleQueryAsync(InsolvenceSearchResult searchdata)
            {
                var query = searchdata.Q;

                IRule[] irules = new IRule[]
                {
                    new OsobaId(HlidacStatu.Repositories.OsobaVazbyRepo.Icos_s_VazbouNaOsobuAsync, "osobaid:", "ico:"),
                    new OsobaId(HlidacStatu.Repositories.OsobaVazbyRepo.Icos_s_VazbouNaOsobuAsync, "osobaiddluznik:",
                        "icodluznik:"),
                    new OsobaId(HlidacStatu.Repositories.OsobaVazbyRepo.Icos_s_VazbouNaOsobuAsync, "osobaidveritel:",
                        "icoveritel:"),
                    new OsobaId(HlidacStatu.Repositories.OsobaVazbyRepo.Icos_s_VazbouNaOsobuAsync, "osobaidspravce:",
                        "icospravce:"),

                    new Holding(HlidacStatu.Repositories.FirmaVazbyRepo.IcosInHolding, "holding:", "ico:"),
                    new Holding(HlidacStatu.Repositories.FirmaVazbyRepo.IcosInHolding, "holdindluznik:", "icoplatce:"),
                    new Holding(HlidacStatu.Repositories.FirmaVazbyRepo.IcosInHolding, "holdingveritel:",
                        "icoveritel:"),
                    new Holding(HlidacStatu.Repositories.FirmaVazbyRepo.IcosInHolding, "holdingspravce:",
                        "icospravce:"),

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
                    new TransformPrefix("text:", "plainText:", null),
                    new TransformPrefix("texttypdokumentu:", "popis:", null),
                    new TransformPrefix("typdokumentu:", "typUdalosti:", null),
                    new TransformPrefix("oddil:", "oddil:", null),
                };


                string modifiedQ = query; // Search.Tools.FixInvalidQuery(query, queryShorcuts, queryOperators) ?? "";
                //check invalid query ( tag: missing value)

                if (searchdata.LimitedView)
                    modifiedQ = Query.ModifyQueryAND(modifiedQ, "onRadar:true", "NOT(odstraneny:true)");
                else if (modifiedQ.Contains("odstraneny:") == false)
                {
                    modifiedQ = Query.ModifyQueryAND(modifiedQ, "NOT(odstraneny:true)");
                }

                //var qc = Lib.Search.Tools.GetSimpleQuery<Rizeni>(modifiedQ, rules); ;
                var qc = await SimpleQueryCreator.GetSimpleQueryAsync<Rizeni>(modifiedQ, irules);

                return qc;
            }


            public static Task<QueryContainer> GetSimpleFulltextQueryAsync(string query)
            {
                return GetSimpleQueryAsync(new InsolvenceSearchResult() { Q = query, Page = 1 });
            }

            public static async Task<QueryContainer> GetSimpleFulltextQueryAsync(
                InsolvenceFulltextSearchResult searchdata)
            {
                var query = searchdata.Q;

                IRule[] irules = new IRule[]
                {
                    new OsobaId(HlidacStatu.Repositories.OsobaVazbyRepo.Icos_s_VazbouNaOsobuAsync, "osobaid:", "ico:"),
                    new OsobaId(HlidacStatu.Repositories.OsobaVazbyRepo.Icos_s_VazbouNaOsobuAsync, "osobaiddluznik:",
                        "icodluznik:"),
                    new OsobaId(HlidacStatu.Repositories.OsobaVazbyRepo.Icos_s_VazbouNaOsobuAsync, "osobaidveritel:",
                        "icoveritel:"),
                    new OsobaId(HlidacStatu.Repositories.OsobaVazbyRepo.Icos_s_VazbouNaOsobuAsync, "osobaidspravce:",
                        "icospravce:"),

                    new Holding(HlidacStatu.Repositories.FirmaVazbyRepo.IcosInHolding, "holding:", "ico:"),
                    new Holding(HlidacStatu.Repositories.FirmaVazbyRepo.IcosInHolding, "holdindluznik:", "icoplatce:"),
                    new Holding(HlidacStatu.Repositories.FirmaVazbyRepo.IcosInHolding, "holdingveritel:",
                        "icoveritel:"),
                    new Holding(HlidacStatu.Repositories.FirmaVazbyRepo.IcosInHolding, "holdingspravce:",
                        "icospravce:"),

                    new TransformPrefixWithValue("ico:",
                        "(rizeni.dluznici.iCO:${q} OR rizeni.veritele.iCO:${q} OR rizeni.spravci.iCO:${q}) ", null),
                    new TransformPrefixWithValue("jmeno:",
                        "(rizeni.dluznici.plneJmeno:${q} OR rizeni.veritele.plneJmeno:${q} OR rizeni.spravci.plneJmeno:${q})",
                        null),

                    new TransformPrefix("icodluznik:", "rizeni.dluznici.iCO:", null),
                    new TransformPrefix("icoveritel:", "rizeni.veritele.iCO:", null),
                    new TransformPrefix("icospravce:", "rizeni.spravci.iCO:", null),
                    new TransformPrefix("jmenodluznik:", "rizeni.dluznici.plneJmeno:", null),
                    new TransformPrefix("jmenoveritel:", "rizeni.veritele.plneJmeno:", null),
                    new TransformPrefix("jmenospravce:", "rizeni.spravci.plneJmeno:", null),
                    new TransformPrefix("spisovaznacka:", "rizeni.spisovaZnacka:", null),
                    new TransformPrefix("id:", "rizeni.spisovaZnacka:", null),

                    new TransformPrefix("zmeneno:", "rizeni.posledniZmena:", "[<>]?[{\\[]+"),
                    new TransformPrefixWithValue("zmeneno:", "rizeni.posledniZmena:[${q} TO ${q}||+1d}", "\\d+"),
                    new TransformPrefix("zahajeno:", "rizeni.datumZalozeni:", "[<>]?[{\\[]+"),
                    new TransformPrefixWithValue("zahajeno:", "rizeni.datumZalozeni:[${q} TO ${q}||+1d}", "\\d+"),

                    new TransformPrefix("stav:", "rizeni.stav:", null),
                    new TransformPrefix("text:", "plainText:", null),
                    new TransformPrefix("texttypdokumentu:", "popis:", null),
                    new TransformPrefix("typdokumentu:", "typUdalosti:", null),
                    new TransformPrefix("oddil:", "oddil:", null),
                };


                string modifiedQ = query; // Search.Tools.FixInvalidQuery(query, queryShorcuts, queryOperators) ?? "";
                //check invalid query ( tag: missing value)

                if (searchdata.LimitedView)
                    modifiedQ = Query.ModifyQueryAND(modifiedQ, "rizeni.onRadar:true", "NOT(rizeni.odstraneny:true)");

                //var qc = Lib.Search.Tools.GetSimpleQuery<Rizeni>(modifiedQ, rules); ;
                var qc = await SimpleQueryCreator.GetSimpleQueryAsync<SearchableDocument>(modifiedQ, irules);

                return qc;
            }

            public static Task<InsolvenceSearchResult> SimpleSearchAsync(string query, int page, int pagesize,
                int order,
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
                var client = Manager.GetESClient_Insolvence();
                var page = search.Page - 1 < 0 ? 0 : search.Page - 1;

                var sw = new Devmasters.DT.StopWatchEx();
                sw.Start();

                //check odstraneny

                search.OrigQuery = search.Q;
                search.Q = HlidacStatu.Searching.Tools.FixInvalidQuery(search.Q ?? "", queryShorcuts, queryOperators);
                var sq = await GetSimpleQueryAsync(search);

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
                            .Sort(ss => InsolvenceSearchResult.GetSort(search.Order))
                            .Highlight(h => Repositories.Searching.Tools.GetHighlight<Rizeni>(withHighlighting))
                            .Aggregations(aggr => anyAggregation)
                            .TrackTotalHits(search.ExactNumOfResults || page * search.PageSize == 0
                                ? true
                                : (bool?)null)
                        );
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
                        _logger.Error(e, "");
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


            public static Task<InsolvenceFulltextSearchResult> SimpleFulltextSearchAsync(string query,
                int page, int pagesize, int order,
                bool withHighlighting = false,
                bool limitedView = true,
                AggregationContainerDescriptor<Rizeni> anyAggregation = null, bool exactNumOfResults = false)
                => SimpleFulltextSearchAsync(new InsolvenceFulltextSearchResult()
                {
                    Q = query,
                    Page = page,
                    PageSize = pagesize,
                    LimitedView = limitedView,
                    Order = order.ToString(),
                    ExactNumOfResults = exactNumOfResults
                }, withHighlighting, anyAggregation);

            public static async Task<InsolvenceFulltextSearchResult> SimpleFulltextSearchAsync(
                InsolvenceFulltextSearchResult search,
                bool withHighlighting = false,
                AggregationContainerDescriptor<Rizeni> anyAggregation = null, bool exactNumOfResults = false)
            {
                var client = Manager.GetESClient_InsolvenceDocs();
                var page = search.Page - 1 < 0 ? 0 : search.Page - 1;

                var sw = new Devmasters.DT.StopWatchEx();
                sw.Start();
                search.OrigQuery = search.Q;
                search.Q = HlidacStatu.Searching.Tools.FixInvalidQuery(search.Q ?? "", queryShorcuts, queryOperators);
                var sq = await GetSimpleFulltextQueryAsync(search);

                ISearchResponse<SearchableDocument> res = null;
                try
                {
                    res = await client
                        .SearchAsync<SearchableDocument>(s => s
                            .Size(search.PageSize)
                            .From(page * search.PageSize)
                            .Source(sr => sr.Excludes(r => r.Fields("plainText")))
                            .Query(q => sq)
                            .Collapse(c => c
                                .Field(f => f.SpisovaZnacka)
                                .InnerHits(ih =>
                                    ih.Name("rec").Size(1).Source(ss => ss.Excludes(ex => ex.Field(f => f.PlainText))))
                            )
                            //.Sort(ss => new SortDescriptor<Rizeni>().Field(m => m.Field(f => f.PosledniZmena).Descending()))
                            .Sort(ss => InsolvenceFulltextSearchResult.GetSort(search.Order))
                            .Highlight(h => Repositories.Searching.Tools.GetHighlight<Rizeni>(withHighlighting))
                            .Aggregations(aggr => anyAggregation)
                            .TrackTotalHits(search.ExactNumOfResults || page * search.PageSize == 0
                                ? true
                                : (bool?)null)
                        );

                    if (withHighlighting && res.Shards != null &&
                        res.Shards.Failed > 0) //if some error, do it again without highlighting
                    {
                        res = await client
                            .SearchAsync<SearchableDocument>(s => s
                                .Size(search.PageSize)
                                .ExpandWildcards(Elasticsearch.Net.ExpandWildcards.All)
                                .From(page * search.PageSize)
                                .Source(false)
                                .Query(q => sq)
                                .Collapse(c => c
                                    .Field(f => f.SpisovaZnacka)
                                    .InnerHits(ih => ih
                                        .Name("rec")
                                        .Size(1)
                                        .Source(ss => ss.Excludes(ex => ex.Field(f => f.PlainText)))
                                    )
                                )
                                //.Sort(ss => new SortDescriptor<Rizeni>().Field(m => m.Field(f => f.PosledniZmena).Descending()))
                                .Sort(ss => InsolvenceFulltextSearchResult.GetSort(search.Order))
                                .Highlight(h => Repositories.Searching.Tools.GetHighlight<Rizeni>(false))
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
                        Manager.LogQueryError<SearchableDocument>(res, "Exception, Orig query:"
                                                                       + search.OrigQuery + "   query:"
                                                                       + search.Q
                                                                       + "\n\n res:" + search.ElasticResults.ToString()
                            , ex: e);
                    }
                    else
                    {
                        _logger.Error(e, "");
                    }

                    throw;
                }

                sw.Stop();
                AuditRepo.Add(Audit.Operations.Search, "", "", "Insolvence", res.IsValid ? "valid" : "invalid",
                    search.Q, null);

                if (res.IsValid == false)
                {
                    Manager.LogQueryError<SearchableDocument>(res, "Exception, Orig query:"
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
        }
    }
}