using HlidacStatu.Entities;
using HlidacStatu.Entities.OsobyES;
using HlidacStatu.Repositories.Searching;
using HlidacStatu.Searching;

using Nest;

using System;
using System.Threading.Tasks;
using HlidacStatu.Connectors;

namespace HlidacStatu.Repositories
{
    public static partial class OsobyEsRepo
    {
        public static class Searching
        {
            public static async Task<OsobaEsSearchResult> FulltextSearchAsync(string query, int page, int pageSize, int? status = null)
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
                    if (status.HasValue)
                    {
                        QueryContainer qc = null;
                        
                        if (status.Value < 0) //vsechny > 0
                            qc = new QueryContainerDescriptor<OsobaES>()
                                .Range(r=>r.Field(f=>f.Status).GreaterThanOrEquals(1));
                        else
                            qc = new QueryContainerDescriptor<OsobaES>()
                                .Term(r => r.Status, status.Value);

                        res = await _esClient
                            .SearchAsync<OsobaES>(s => s
                                .Size(pageSize)
                                .From(page * pageSize)
                                .Query(_query => _query
                                    .Bool(_bool => _bool
                                        .Must(_must => _must
                                            .MultiMatch(c => c
                                                .Fields(f => f
                                                    .Field(p => p.FullName)
                                                    .Field("fullName.lower", 2)
                                                    .Field("fullName.lowerascii", 1.5)
                                                    )
                                            .Type(TextQueryType.MostFields)
                                            .Fuzziness(Fuzziness.EditDistance(fuzzyDistance))
                                            .Query(modifQ)
                                            .Operator(Operator.And)
                                            ),q=> qc
                                        )
                                    )
                                )
                                .TrackTotalHits(true)
                            );
                    }
                    else
                    {

                        res = await _esClient //.MultiSearch<OsobaES>(s => s
                            .SearchAsync<OsobaES>(s => s
                            .Size(pageSize)
                            .From(page * pageSize)
                            .Query(_query => _query
                                .MultiMatch(c => c
                                    .Fields(f => f
                                        .Field(p => p.FullName)
                                        .Field("fullName.lower", 2)
                                        .Field("fullName.lowerascii", 1.5)
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
                        _logger.Error(e, "");
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