using System;
using HlidacStatu.Entities;
using HlidacStatu.Entities.OsobyES;
using HlidacStatu.Repositories.ES;
using HlidacStatu.Repositories.Searching;
using HlidacStatu.Repositories.Searching.Rules;
using Nest;

namespace HlidacStatu.Repositories
{
    public static partial class OsobyEsRepo
    {
        public static class Searching
        {
            public static OsobaEsSearchResult FulltextSearch(string query, int page, int pageSize, int? status = null)
        {
            string modifQ = SimpleQueryCreator
                .GetSimpleQuery(query, new IRule[] { new RemoveAllOperators() })
                .FullQuery();

            if (string.IsNullOrWhiteSpace(modifQ))
            {
                return new OsobaEsSearchResult()
                {
                    OrigQuery = query,
                    Total = 0,
                    IsValid = true,
                    ElasticResults = null,
                    ElapsedTime = TimeSpan.Zero
                };

            }

            page = page - 1 < 0 ? 0 : page - 1;

            var sw = new Devmasters.DT.StopWatchEx();
            sw.Start();
            

            ISearchResponse<OsobaES> res = null;
            try
            {
                int fuzzyDistance = 1;
                if(status.HasValue)
                {
                    res = _esClient
                        .Search<OsobaES>(s => s
                            .Size(pageSize)
                            .From(page * pageSize)
                            .Query(_query => _query
                                .Bool(_bool => _bool
                                    .Must(_must => _must
                                        .Fuzzy(_fuzzy => _fuzzy
                                            .Field(_field => _field.FullName)
                                            .Value(modifQ)
                                            .Fuzziness(Fuzziness.EditDistance(fuzzyDistance))
                                        )
                                        && _must.Term(_field => _field.Status, status.Value)
                                    )
                                    .Should(
                                        _boostWomen => _boostWomen
                                        .Match(_match => _match
                                            .Field(_field => _field.FullName)
                                            .Query(modifQ)
                                            .Operator(Operator.And)
                                        ),
                                        _boostExact => _boostExact
                                        .Match(_match => _match
                                            .Field("fullName.lower")
                                            .Query(modifQ)
                                            .Operator(Operator.And)
                                        ),
                                        _boostAscii => _boostAscii
                                        .Match(_match => _match
                                            .Field("fullName.lowerascii")
                                            .Query(modifQ)
                                            .Operator(Operator.And)
                                        )
                                    )
                                )
                            )
                            .TrackTotalHits(true)
                        );
                }
                else
                {

                    res = _esClient //.MultiSearch<OsobaES>(s => s
                        .Search<OsobaES>(s => s
                        .Size(pageSize)
                        .From(page * pageSize)
                        .Query(_query => _query
                                .MultiMatch(c => c
                            .Fields(f => f
                                .Field(p => p.FullName)
                                .Field("fullName.lower",2)
                                .Field("fullName.lowerascii",1.5)
                                )
                            .Type(TextQueryType.MostFields)
                            .Fuzziness(Fuzziness.EditDistance(fuzzyDistance))
                            .Query(modifQ)
                            .Operator(Operator.And)
                            )
                        )
                        .TrackTotalHits(true)
                    );
                }

            }
            catch (Exception e)
            {
                AuditRepo.Add(Audit.Operations.Search, "", "", "OsobaES", "error", query, null);
                if (res != null && res.ServerError != null)
                {
                    Manager.LogQueryError<OsobaES>(res, "Exception, Orig query:"
                                                        + query + "   query:"
                                                        + modifQ
                        , ex: e);
                }
                else
                {
                    Util.Consts.Logger.Error("", e);
                }
                throw;
            }
            sw.Stop();

            AuditRepo.Add(Audit.Operations.Search, "", "", "OsobaES", res.IsValid ? "valid" : "invalid", query, null);

            if (res.IsValid == false)
            {
                Manager.LogQueryError<OsobaES>(res, "Exception, Orig query:"
                                                    + query + "   query:"
                                                    + query
                    );
            }

            var search = new OsobaEsSearchResult
            {
                OrigQuery = query,
                Total = res?.Total ?? 0,
                IsValid = res?.IsValid ?? false,
                ElasticResults = res,
                ElapsedTime = sw.Elapsed
            };
            return search;
        }
        }
    }
}