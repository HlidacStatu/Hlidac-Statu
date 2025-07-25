﻿using HlidacStatu.Repositories.Searching;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HlidacStatu.Connectors;

namespace HlidacStatu.Datasets
{
    public partial class Search
    {
        public class DatasetSumGeneralResult : HlidacStatu.Searching.Search.GeneralResult<string>
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

        public class DatasetMultiResult : HlidacStatu.Searching.Search.ISearchResult
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

            public Searching.RouteValues ToRouteValues(int page)
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

            public static async Task<DatasetMultiResult> GeneralSearchAsync(string query, IEnumerable<DataSet> datasets = null, int page = 1, int pageSize = 20, string sort = null, bool withHighlighting = false)
            {
                DatasetMultiResult res = new DatasetMultiResult() { Query = query, DataSource = "DatasetMultiResult.GeneralSearch" };

                if (string.IsNullOrEmpty(query))
                    return res;

                if (!(await Repositories.Searching.Tools.ValidateQueryAsync(query)))
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
                await Parallel.ForEachAsync(datasets, po,
                    async (ds, _) =>
                    {
                        try
                        {
                            var rds = await ds.SearchDataAsync(query, page, pageSize, sort, withHighlighting: withHighlighting);
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
                            //_logger.Warning("DatasetMultiResult GeneralSearch for query" + query, e);
                        }

                    });
                sw.Stop();
                res.ElapsedTime = sw.Elapsed;
                return res;
            }


        }
    }
}
