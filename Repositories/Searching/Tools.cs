using Devmasters;
using Devmasters.Batch;
using HlidacStatu.Entities;
using Nest;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Manager = HlidacStatu.Connectors.Manager;

namespace HlidacStatu.Repositories.Searching
{
    public static class Tools
    {
        private static readonly ILogger _logger = Log.ForContext(typeof(Tools));

        public const int MaxResultWindow = 10000;



        public static HighlightDescriptor<T> GetHighlight<T>(bool enable)
            where T : class
        {
            HighlightDescriptor<T> hh = new HighlightDescriptor<T>();
            if (enable)
                hh = hh.Order(HighlighterOrder.Score)
                    .PreTags("<highl>")
                    .PostTags("</highl>")
                    .Fields(ff => ff
                        .Field("*")
                        .RequireFieldMatch(false)
                        .Type(HighlighterType.Unified)
                        .FragmentSize(100)
                        .NumberOfFragments(3)
                        .Fragmenter(HighlighterFragmenter.Span)
                        .BoundaryScanner(BoundaryScanner.Sentence)
                        .BoundaryScannerLocale("cs_CZ")
                    );
            return hh;
        }

        public static async Task<bool> ValidateQueryAsync<T>(ElasticClient client, QueryContainer qc)
            where T : class
        {
            return (await ValidateSpecificQueryRawAsync<T>(client, qc))?.Valid ?? false;
        }

        public static async Task<bool> ValidateQueryAsync(string query)
        {
            return (await ValidateQueryRawAsync(query))?.Valid ?? false;
        }

        public static async Task<ValidateQueryResponse> ValidateQueryRawAsync(string query)
        {
            return await ValidateSpecificQueryRawAsync<Smlouva>(await Manager.GetESClientAsync(),
                SmlouvaRepo.Searching.GetSimpleQuery(query));
        }


        public static async Task<ValidateQueryResponse> ValidateSpecificQueryRawAsync<T>(ElasticClient client, QueryContainer qc)
            where T : class
        {
            var res = await client.Indices
                .ValidateQueryAsync<T>(v => v
                    .Query(q => qc)
                );

            return res;
        }


        static string ScrollLifeTime = "2m";

        public static async Task DoActionForAllAsync<T>(
            Func<IHit<T>, object, ActionOutputData> action,
            object actionParameters,
            Action<string> logOutputFunc,
            Action<ActionProgressData> progressOutputFunc,
            bool parallel,
            int blockSize = 500, int? maxDegreeOfParallelism = null,
            bool IdOnly = false,
            ElasticClient elasticClient = null,
            string query = null,
            Indices indexes = null, string prefix = null, IMonitor monitor = null
        )
            where T : class
        {
            prefix = prefix ?? HlidacStatu.Util.StackReport.GetCallingMethod(false, skipFrames: 1);

            var client = elasticClient ?? await Manager.GetESClientAsync();

            Func<int, int, Task<ISearchResponse<T>>> searchFunc = null;

            var qs = new QueryContainerDescriptor<T>().MatchAll();
            if (!string.IsNullOrEmpty(query))
            {
                qs = new QueryContainerDescriptor<T>()
                    .QueryString(qq => qq
                        .Query(query)
                        .DefaultOperator(Operator.And)
                    );
            }

            if (IdOnly)
                searchFunc = (size, page) => client.SearchAsync<T>(a => a
                    .Index(indexes ?? client.ConnectionSettings.DefaultIndex)
                    .Source(false)
                    //.Fields(f => f.Field("Id"))
                    .Size(size)
                    .From(page * size)
                    .Query(q => qs)
                    .Scroll(ScrollLifeTime)
                );
            else
                searchFunc = (size, page) => client.SearchAsync<T>(a => a
                        .Index(indexes ?? client.ConnectionSettings.DefaultIndex)
                        .Size(size)
                        .From(page * size)
                        .Query(q => qs)
                        .Scroll(ScrollLifeTime)
                    );

            await DoActionForQueryAsync<T>(client,
                searchFunc,
                action, actionParameters,
                logOutputFunc,
                progressOutputFunc,
                parallel,
                blockSize, maxDegreeOfParallelism, prefix, monitor
            );
        }

        public static async Task DoActionForQueryAsync<T>(ElasticClient client,
            Func<int, int, Task<ISearchResponse<T>>> searchFunc,
            Func<IHit<T>, object, ActionOutputData> action, object actionParameters,
            Action<string> logOutputFunc,
            Action<ActionProgressData> progressOutputFunc,
            bool parallel,
            int blockSize = 500, int? maxDegreeOfParallelism = null, string prefix = null,
            IMonitor monitor = null
        )
            where T : class
        {
            prefix = prefix ?? HlidacStatu.Util.StackReport.GetCallingMethod(false, skipFrames: 1);

            DateTime started = DateTime.Now;
            long total = 0;
            int currIteration = 0;

            int processedCount = 0;
            ISearchResponse<T> result = default(ISearchResponse<T>);
            //var total = NoveInzeraty.SmlouvaRepo.Search.NumberOfDocs(null, null);
            string scrollId = null;

            //create scroll search context
            bool firstResult = true;

            if (maxDegreeOfParallelism <= 1)
                parallel = false;
            try
            {
                result = await searchFunc(blockSize, currIteration);
                if (result.IsValid == false)
                    Manager.LogQueryError<T>(result);
            }
            catch (Exception)
            {
                await Task.Delay(10000);
                try
                {
                    result = await searchFunc(blockSize, currIteration);
                }
                catch (Exception)
                {
                    await Task.Delay(20000);
                    try
                    {
                        result = await searchFunc(blockSize, currIteration);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "Cannot read data from Elastic, skipping iteration" + currIteration);
                        return;
                    }
                }
            }

            scrollId = result.ScrollId;

            do
            {
                if (firstResult)
                {
                    firstResult = false;
                    if (monitor != null)
                        monitor.Start();
                }
                else
                {
                    result = await client.ScrollAsync<T>(ScrollLifeTime, scrollId);
                    scrollId = result.ScrollId;
                }

                currIteration++;

                if (result.Hits.Count() == 0)
                    break;
                total = result.Total;


                bool canceled = false;

                if (parallel)
                {
                    CancellationTokenSource cts = new CancellationTokenSource();
                    try
                    {
                        ParallelOptions pOptions = new ParallelOptions();
                        if (maxDegreeOfParallelism.HasValue)
                            pOptions.MaxDegreeOfParallelism = maxDegreeOfParallelism.Value;
                        pOptions.CancellationToken = cts.Token;
                        _ = Parallel.ForEach(result.Hits, (hit) =>
                        {
                            if (action != null)
                            {
                                ActionOutputData cancel = null;
                                try
                                {
                                    cancel = action(hit, actionParameters);
                                    Interlocked.Increment(ref processedCount);


                                    if (logOutputFunc != null && !string.IsNullOrEmpty(cancel.Log))
                                        logOutputFunc(cancel.Log);

                                    if (cancel.CancelRunning)
                                        cts.Cancel();
                                }
                                catch (Exception e)
                                {
                                    _logger.Error(e, "DoActionForAll action error");
                                    cts.Cancel();
                                }
                                finally
                                {
                                    if (monitor != null)
                                        monitor.SetProgress((decimal)ActionProgressData.SimplePercentDone(total, processedCount));

                                }
                            }

                            if (progressOutputFunc != null)
                            {
                                ActionProgressData apd = new ActionProgressData(total, processedCount, started, prefix);
                                progressOutputFunc(apd);
                            }
                        });
                    }
                    catch (OperationCanceledException)
                    {
                        //Catestrophic Failure
                        canceled = true;
                    }
                }
                else
                    foreach (var hit in result.Hits)
                    {
                        if (action != null)
                        {
                            ActionOutputData cancel = action(hit, actionParameters);
                            try
                            {

                                Interlocked.Increment(ref processedCount);
                                if (logOutputFunc != null && !string.IsNullOrEmpty(cancel.Log))
                                    logOutputFunc(cancel.Log);

                                if (cancel.CancelRunning)
                                {
                                    canceled = true;
                                    break;
                                }
                            }
                            catch (Exception e)
                            {
                                _logger.Error(e, "DoActionForQueryAsync action error");
                                if (canceled)
                                    break;

                            }
                            finally
                            {
                                if (monitor != null)
                                    monitor.SetProgress((decimal)ActionProgressData.SimplePercentDone(total, processedCount));

                                if (progressOutputFunc != null)
                                {
                                    ActionProgressData apd = new ActionProgressData(total, processedCount, started, prefix);
                                    progressOutputFunc(apd);
                                }

                            }
                        }

                    }


                if (canceled)
                    break;
            } while (result.Hits.Count() > 0);
            if (monitor != null)
                monitor.Finish(true, null);

            await client.ClearScrollAsync(c => c.ScrollId(scrollId));

            if (logOutputFunc != null)
                logOutputFunc("Done");
        }

        public class DataResultset<T>
            where T : class
        {
            public IEnumerable<T> Result { get; set; }
            public bool ErrorOccurred { get; set; } = false;
            public Exception Exception { get; set; } = null;
        }

        public static async Task<DataResultset<T>> GetAllRecordsAsync<T>(ElasticClient sourceESClient, int maxDegreeOfParallelism,
            string query = null, int batchSize = 10,
            Action<string> logOutputFunc = null, Action<Devmasters.Batch.ActionProgressData> progressOutputFunc = null,
            IMonitor monitor = null)

            where T : class
        {
            if (maxDegreeOfParallelism < 2)
                throw new ArgumentOutOfRangeException("maxDegreeOfParallelism", "maxDegreeOfParallelism cannot be smaller than 2");

            var qs = new QueryContainerDescriptor<T>().MatchAll();
            if (!string.IsNullOrEmpty(query))
            {
                query =  HlidacStatu.Searching.Tools.FixInvalidQuery(query, Repositories.SmlouvaRepo.Searching.Irules,
                    HlidacStatu.Searching.Tools.DefaultQueryOperators);

                qs = Repositories.SmlouvaRepo.Searching.GetSimpleQuery(query);
            }

            long total = 0;
            var resCount = await sourceESClient.SearchAsync<object>(a => a
                .Source(false)
                .Size(0)
                .Query(q => qs)
                .TrackTotalHits(true)
                );
            if (resCount.IsValid)
                total = resCount.Total;
            else
                total = 0;


            var scrollAllObservable = sourceESClient.ScrollAll<T>("4m", maxDegreeOfParallelism, sc => sc
                .MaxDegreeOfParallelism(maxDegreeOfParallelism)
                .Search(s => s
                    .Size(batchSize)
                    .Query(q => qs)
                )
            );

            var waitHandle = new ManualResetEvent(false);

            var allRecs = new ConcurrentBag<T>();
            var res = new DataResultset<T>();
            System.Collections.Concurrent.ConcurrentBag<Exception> exceptions = new();

            DateTime started = DateTime.Now;

            if (monitor != null)
                monitor.Start();

            var scrollAllObserver = new ScrollAllObserver<T>(
                onNext: response =>
                {
                    if (!response.SearchResponse.IsValid)
                    {
                        _logger.Warning("Invalid response {response}", response.SearchResponse.DebugInformation);
                    }

                    foreach (var h in response.SearchResponse.Hits)
                    {
                        allRecs.Add(h.Source);
                    }
                    if (progressOutputFunc != null)
                    {
                        progressOutputFunc(new ActionProgressData(total, allRecs.Count, started));
                    }
                    if (monitor != null)
                        monitor.SetProgress((decimal)ActionProgressData.SimplePercentDone(total, allRecs.Count));
                },
                onError: e =>
                {
                    _logger.Error(e, "Scroll error occured cl:{client} q:{query}", sourceESClient.ConnectionSettings.DefaultIndex, query);
                    res.ErrorOccurred = true;
                    res.Exception = e;
                    exceptions.Add(e);
                    if (logOutputFunc != null)
                    {
                        logOutputFunc(e.ToString());
                    }
                    _ = waitHandle.Set();
                },
                onCompleted: () => waitHandle.Set()
            );

            var subscriber = scrollAllObservable.Subscribe(scrollAllObserver);
            _ = waitHandle.WaitOne();
            subscriber.Dispose();

            if (monitor != null)
                monitor.Finish(exceptions.ToArray());

            res.Result = allRecs;

            return res;
        }


        public static async Task<DataResultset<string>> GetAllIdsAsync(this ElasticClient sourceESClient, int maxDegreeOfParallelism,
            string query = null, int batchSize = 100,
            Action<string> logOutputFunc = null, Action<Devmasters.Batch.ActionProgressData> progressOutputFunc = null,
            IMonitor monitor = null)
        {
            if (maxDegreeOfParallelism < 2)
                throw new ArgumentOutOfRangeException("maxDegreeOfParallelism", "maxDegreeOfParallelism cannot be smaller than 2");

            long total = 0;
            var qs = new QueryContainerDescriptor<object>().MatchAll();
            if (!string.IsNullOrEmpty(query))
            {
                query = HlidacStatu.Searching.Tools.FixInvalidQuery(query, Repositories.SmlouvaRepo.Searching.Irules, 
                    HlidacStatu.Searching.Tools.DefaultQueryOperators);

                qs = Repositories.SmlouvaRepo.Searching.GetSimpleQuery(query);
            }
            var resCount = await sourceESClient.SearchAsync<object>(a => a
                .Source(false)
                .Size(0)
                .Query(q => qs)
                .TrackTotalHits(true)
                );
            if (resCount.IsValid)
                total = resCount.Total;
            else
                total = 0;

            var scrollAllObservable = sourceESClient.ScrollAll<object>("4m", maxDegreeOfParallelism, sc => sc
                .MaxDegreeOfParallelism(maxDegreeOfParallelism)
                .Search(s => s
                    .Size(batchSize)
                    .Source(false)
                    .Query(q => qs)
                )
            );

            var waitHandle = new ManualResetEvent(false);
            DateTime started = DateTime.Now;
            var allIds = new ConcurrentBag<string>();
            var res = new DataResultset<string>();
            System.Collections.Concurrent.ConcurrentBag<Exception> exceptions = new();

            if (monitor != null)
                monitor.Start();

            var scrollAllObserver = new ScrollAllObserver<object>(
                onNext: response =>
                {
                    if (!response.SearchResponse.IsValid)
                    {
                        _logger.Warning("Invalid response {response}", response.SearchResponse.DebugInformation);
                    }

                    var ids = response.SearchResponse.Hits.Select(h => h.Id);
                    foreach (var id in ids)
                    {
                        allIds.Add(id);
                    }
                    if (progressOutputFunc != null)
                    {
                        progressOutputFunc(new ActionProgressData(total, allIds.Count, started));
                    }
                    if (monitor != null)
                        monitor.SetProgress((decimal)ActionProgressData.SimplePercentDone(total, allIds.Count));

                },
                onError: e =>
                {
                    _logger.Error(e, "Scroll error occured cl:{client} q:{query}", sourceESClient.ConnectionSettings.DefaultIndex, query);
                    res.ErrorOccurred = true;
                    res.Exception = e;
                    if (logOutputFunc != null)
                    {
                        logOutputFunc(e.ToString());
                    }
                    exceptions.Add(e);

                    _ = waitHandle.Set();
                },
                onCompleted: () =>
                {

                    waitHandle.Set();
                }
            );

            var subscriber = scrollAllObservable.SubscribeSafe(scrollAllObserver);
            _ = waitHandle.WaitOne();
            subscriber.Dispose();
            if (monitor != null)
                monitor.Finish(exceptions.ToArray());

            res.Result = allIds;

            return res;
        }

        public static List<string> SimpleGetAllIds(this ElasticClient sourceESClient, int maxDegreeOfParallelism,
                string simplequery, int batchSize = 100)
        {
            var qs = Repositories.SmlouvaRepo.Searching.GetSimpleQuery(simplequery);
            return SimpleGetAllIds(sourceESClient, maxDegreeOfParallelism, qs, batchSize);
        }
        public static List<string> SimpleGetAllIds(this ElasticClient sourceESClient, int maxDegreeOfParallelism,
                QueryContainer query, int batchSize = 100)
        {
            if (maxDegreeOfParallelism < 2)
                throw new ArgumentOutOfRangeException(nameof(maxDegreeOfParallelism), "maxDegreeOfParallelism cannot be smaller than 2");

            var scrollAllObservable = sourceESClient.ScrollAll<object>("4m", maxDegreeOfParallelism, sc => sc
                .MaxDegreeOfParallelism(maxDegreeOfParallelism)
                .Search(s => s
                    .Size(batchSize)
                    .Source(false)
                    .Query(q => query)
                )
            );

            var waitHandle = new ManualResetEvent(false);
            var allIds = new ConcurrentBag<string>();
            var scrollAllObserver = new ScrollAllObserver<object>(
                onNext: response =>
                {
                    if (!response.SearchResponse.IsValid)
                    {
                        _logger.Warning("Invalid response {response}", response.SearchResponse.DebugInformation);
                    }

                    var ids = response.SearchResponse.Hits.Select(h => h.Id);
                    foreach (var id in ids)
                    {
                        allIds.Add(id);
                    }
                },
                onError: e =>
                {
                    _logger.Error(e, "Scroll error occured cl:{client} q:{query}",
                        sourceESClient.ConnectionSettings.DefaultIndex, query);
                    _ = waitHandle.Set();
                },
                onCompleted: () =>
                {

                    waitHandle.Set();
                }
            );

            var subscriber = scrollAllObservable.SubscribeSafe(scrollAllObserver);
            _ = waitHandle.WaitOne();
            subscriber.Dispose();

            return allIds.ToList();
        }


        public static QueryContainer GetRawQuery(string jsonQuery)
        {
            QueryContainer qc = null;
            if (string.IsNullOrEmpty(jsonQuery))
                qc = new QueryContainerDescriptor<Smlouva>().MatchAll();
            else
            {
                qc = new QueryContainerDescriptor<Smlouva>().Raw(jsonQuery);
            }

            return qc;
        }


        public static string ToElasticDate(DateTime? date, string defaultValue = "")
        {
            if (date.HasValue == false)
                return defaultValue;

            switch (date.Value.Kind)
            {
                case DateTimeKind.Unspecified:
                case DateTimeKind.Local:
                    return date.Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                case DateTimeKind.Utc:
                    return date.Value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                default:
                    return date.Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            }
        }

        public static string ToElasticDateInterval(DateTime? fromDate, DateTime? toDate)
        {
            return $"[{ToElasticDate(fromDate, "*")} TO {ToElasticDate(toDate, "*")} }}";
        }
    }
}