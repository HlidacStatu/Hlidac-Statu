using HlidacStatu.Repositories.Searching;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HlidacStatu.Datasets
{
    public partial class Search
    {
        public class DatasetSumGeneralResult : Repositories.Searching.Search.GeneralResult<string>
        {
            public DataSet Dataset { get; set; }
            public DatasetSumGeneralResult(string query, long total, IEnumerable<string> results, int pageSize, DataSet dataset, TimeSpan searchElapsedTime)
                : base(query, total, results, pageSize, true)
            {
                Dataset = dataset;
                DataSource = "Dataset." + Dataset.DatasetId;
                ElapsedTime = searchElapsedTime;
            }


        }

        public class DatasetMultiResult : Repositories.Searching.Search.ISearchResult
        {
            public string Query { get; set; }
            public long Total { get { return Results.Sum(m => m.Total); } }
            public bool IsValid { get { return Results.All(m => m.IsValid || Exceptions.Any()); } }
            public bool HasResult { get { return IsValid && Total > 0; } }
            public string DataSource { get; set; }
            public TimeSpan ElapsedTime { get; set; } = TimeSpan.Zero;
            public int PageSize { get; set; }
            public int Page { get; set; }
            public string Order { get; set; }

            public virtual int MaxResultWindow() { return Repositories.Searching.Tools.MaxResultWindow; }

            public RouteValues ToRouteValues(int page)
            {
                return new()
                {
                    Q = Query,
                    Page = page,
                };
            }


            public System.Collections.Concurrent.ConcurrentBag<DataSearchResult> Results { get; set; }
                = new System.Collections.Concurrent.ConcurrentBag<DataSearchResult>();

            public System.Collections.Concurrent.ConcurrentBag<Exception> Exceptions { get; set; }
                = new System.Collections.Concurrent.ConcurrentBag<Exception>();


            public AggregateException GetExceptions()
            {
                if (Exceptions.Count == 0)
                    return null;

                AggregateException agg = new AggregateException(
                    "Aggregated exceptions from DatasetMultiResult",
                    Exceptions
                    );
                return agg;
            }

            static object objGeneralSearchLock = new object();


            public static DatasetMultiResult GeneralSearch(string query, IEnumerable<DataSet> datasets = null, int page = 1, int pageSize = 20, string sort = null)
            {
                DatasetMultiResult res = new DatasetMultiResult() { Query = query, DataSource = "DatasetMultiResult.GeneralSearch" };

                if (string.IsNullOrEmpty(query))
                    return res;

                if (!Repositories.Searching.Tools.ValidateQuery(query))
                {
                    res.Exceptions.Add(new Exception($"Invalid Query: {query}"));
                    return res;
                }

                if (datasets == null)
                    datasets = DataSetDB.ProductionDataSets.Get();

                ParallelOptions po = new ParallelOptions();
                po.MaxDegreeOfParallelism = System.Diagnostics.Debugger.IsAttached ? 1 : po.MaxDegreeOfParallelism;

                Devmasters.DT.StopWatchEx sw = new Devmasters.DT.StopWatchEx();
                sw.Start();
                Parallel.ForEach(datasets, po,
                    ds =>
                    {
                        try
                        {
                            var rds = ds.SearchData(query, page, pageSize, sort);
                            if (rds.IsValid)
                            {

                                res.Results.Add(rds);
                            }
                        }
                        catch (DataSetException e)
                        {
                            res.Exceptions.Add(e);
                        }
                        catch (Exception e)
                        {
                            res.Exceptions.Add(e);
                            //HlidacStatu.Util.Consts.Logger.Warning("DatasetMultiResult GeneralSearch for query" + query, e);
                        }

                    });
                sw.Stop();
                res.ElapsedTime = sw.Elapsed;
                return res;
            }


        }
    }
}
