using HlidacStatu.Repositories.Searching;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HlidacStatu.Datasets
{
    public partial class Search
    {

        public class DatasetMultiQueryMultiResult : Repositories.Searching.Search.ISearchResult
        {
            public System.TimeSpan ElapsedTime { get; set; }
            public long Total { get { return Results.Sum(m => m.Total); } }
            public bool IsValid { get { return Results.All(m => m.IsValid || Exceptions.Any()); } }
            public bool HasResult { get { return IsValid && Total > 0; } }
            public string DataSource { get; set; }
            public int PageSize { get; set; }
            public int Page { get; set; }
            public string Order { get; set; }

            public string Query { get; set; }

            public virtual int MaxResultWindow() { return Repositories.Searching.Tools.MaxResultWindow; }

            public virtual RouteValues ToRouteValues(int page)
            {
                return new()
                {
                    Q = Query,
                    Page = page,
                };
            }


            public System.Collections.Concurrent.ConcurrentBag<DataSearchResult> Results { get; set; }
                = new();

            public System.Collections.Concurrent.ConcurrentBag<System.Exception> Exceptions { get; set; }
                = new();


            public System.AggregateException GetExceptions()
            {
                if (Exceptions.Count == 0)
                    return null;

                System.AggregateException agg = new System.AggregateException(
                    "Aggregated exceptions from DatasetMultiResult",
                    Exceptions
                    );
                return agg;
            }

            public static async Task<DatasetMultiQueryMultiResult> GeneralSearchAsync(Dictionary<DataSet, string> datasetsWithQuery, int page, int pageSize)
            {
                DatasetMultiQueryMultiResult res = new DatasetMultiQueryMultiResult()
                {
                    DataSource = "DatasetMultiQueryMultiResult.GeneralSearch"
                };

                if (datasetsWithQuery == null || datasetsWithQuery.Count == 0)
                    return res;

                if (!Repositories.Searching.Tools.ValidateQueryAsync(datasetsWithQuery.First().Value))
                {
                    res.Exceptions.Add(new System.Exception($"Invalid Query: {datasetsWithQuery.First().Value}"));
                    return res;
                }

                ParallelOptions po = new ParallelOptions();
                po.MaxDegreeOfParallelism = System.Diagnostics.Debugger.IsAttached ? 1 : po.MaxDegreeOfParallelism;

                await Parallel.ForEachAsync(datasetsWithQuery, po,
                    async (ds, _ ) =>
                    {
                        try
                        {
                            DataSearchResult rds = await ds.Key.SearchDataAsync(ds.Value, page, pageSize);

                            if (rds.IsValid)
                                res.Results.Add(rds);
                        }
                        catch (DataSetException e)
                        {
                            res.Exceptions.Add(e);
                        }
                        catch (System.Exception e)
                        {
                            res.Exceptions.Add(e);
                            //HlidacStatu.Util.Consts.Logger.Warning("DatasetMultiResult GeneralSearch for query" + query, e);
                        }

                    });

                return res;
            }

        }
    }
}
