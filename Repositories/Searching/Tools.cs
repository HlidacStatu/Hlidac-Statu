using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Devmasters;
using Devmasters.Batch;

using HlidacStatu.Entities;
using HlidacStatu.Util;

using Nest;

using Manager = HlidacStatu.Repositories.ES.Manager;

namespace HlidacStatu.Repositories.Searching
{
    public static class Tools
    {
        public const int MaxResultWindow = 10000;

        static string regexInvalidQueryTemplate = @"(^|\s|[(])(?<q>$operator$\s{1} (?<v>(\w{1,})) )($|\s|[)])";

        static RegexOptions regexQueryOption =
            RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline;

        public static readonly string[] DefaultQueryOperators = new string[] { "AND", "OR" };


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

        public static string FixInvalidQuery(string query, Rules.IRule[] rules, string[] operators = null)
        {
            return FixInvalidQuery(query, rules.SelectMany(m => m.Prefixes).ToArray(), operators);
        }

        public static string FixInvalidQuery(string query, string[] shortcuts, string[] operators = null)
        {
            if (operators == null)
                operators = DefaultQueryOperators;

            if (string.IsNullOrEmpty(query))
                return query;

            string newquery = query;

            //fix query ala (issues.issueTypeId:18+OR+issues.issueTypeId:12)+ico:00283924
            if (!string.IsNullOrEmpty(query))
            {
                query = query.Trim();
                if (query.Contains("+") && !query.Contains(" "))
                    newquery = System.Net.WebUtility.UrlDecode(query);
            }

            MatchEvaluator evalDelimiterMatch = (m) =>
            {
                var s = m.Value;
                if (string.IsNullOrEmpty(s))
                    return string.Empty;
                if (m.Groups["v"].Success)
                {
                    var v = m.Groups["v"]?.Value?.ToUpper()?.Trim() ?? "";
                    if (operators.Contains(v))
                        return s;
                }

                var newVal = s.Replace(": ", ":");
                return newVal;
            };

            foreach (var qo in shortcuts)
            {
                string lookFor = regexInvalidQueryTemplate.Replace("$operator$", qo);
                //if (modifiedQ.ToLower().Contains(lookFor.ToLower()))
                if (Regex.IsMatch(newquery, lookFor, regexQueryOption))
                {
                    newquery = Regex.Replace(newquery, lookFor, evalDelimiterMatch, regexQueryOption);
                }
            }

            // [\p{L}\p{M}\d_] \p{L} -znaky všech abeced \p{M} -kombinované znaky
            string invalidFormatRegex = @"(AND \s+)? ([^""]\w +\.?\w +) :($|[^\""=><[\p{L}\p{M}\d_{)]) (AND )?";
            Match mIsInvalid = Regex.Match(query, invalidFormatRegex,
                (RegexOptions.IgnorePatternWhitespace | RegexOptions.IgnoreCase));
            if (mIsInvalid.Success)
            {
                newquery = Regex.Replace(newquery, invalidFormatRegex, " ",
                    RegexOptions.IgnorePatternWhitespace | RegexOptions.IgnoreCase).Trim();
            }

            var textParts = TextUtil.SplitStringToPartsWithQuotes(newquery, '\"');
            //make operator UpperCase and space around '(' and ')'
            if (textParts.Count > 0)
            {
                string fixedOperator = "";
                foreach (var tp in textParts)
                {
                    if (tp.Item2 == true)
                        fixedOperator = fixedOperator + tp.Item1;
                    else
                    {
                        var fixPart = tp.Item1;
                        fixPart = System.Net.WebUtility.UrlDecode(fixPart);
                        fixPart = System.Net.WebUtility.HtmlDecode(fixPart);
                        Regex opReg =
                            new Regex(string.Format(@"(^|\s)({0})(\s|$)", operators.Aggregate((f, s) => f + "|" + s)),
                                regexQueryOption);

                        //UPPER Operator
                        fixPart = opReg.Replace(fixPart, (me) => { return me.Value.ToUpper(); });

                        //Space around '(' and ')'
                        fixPart = fixPart.Replace("(", "( ").Replace(")", " )");
                        fixedOperator = fixedOperator + fixPart;
                    }
                }

                newquery = fixedOperator;
            }

            //fix DÚK/Sou/059/2009  -> DÚK\\/Sou\\/059\\/2009
            //
            // but support regex name:/joh?n(ath[oa]n)/
            //
            //if (newquery.Contains("/")) //&& regFindRegex.Match(query).Success == false)
            //{
            //    newquery = newquery.Replace("/", "\\/");
            //}

            //fix with small "to" in zverejneno:[2018-12-13 to *]
            var dateintervalRegex =
                @"(podepsano|zverejneno):({|\[)\d{4}\-\d{2}\-\d{2}(?<to> \s*to\s*)(\d{4}-\d{2}-\d{2}|\*)(}|\])";
            if (Regex.IsMatch(newquery, dateintervalRegex, regexQueryOption))
            {
                newquery = newquery.ReplaceGroupMatchNameWithRegex(dateintervalRegex, "to", " TO ");
            }


            if (newquery != query)
                return newquery;
            else
                return query;
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
            return await ValidateSpecificQueryRawAsync<Smlouva>(await Repositories.ES.Manager.GetESClientAsync(),
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

            var client = elasticClient ?? await Repositories.ES.Manager.GetESClientAsync();

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
                        Consts.Logger.Error("Cannot read data from Elastic, skipping iteration" + currIteration, ex);
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
                                    Consts.Logger.Error("DoActionForAll action error", e);
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
                                Devmasters.Log.Logger.Root.Error("DoActionForQueryAsync action error", e);
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
                monitor.Finish(true,null);

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
                qs = new QueryContainerDescriptor<T>()
                    .QueryString(qq => qq
                        .Query(query)
                        .DefaultOperator(Operator.And)
                    );
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
                        HlidacStatu.Util.Consts.Logger.Warning("Invalid response {response}", response.SearchResponse.DebugInformation);
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
                    HlidacStatu.Util.Consts.Logger.Error("Scroll error occured cl:{client} q:{query}", e, sourceESClient.ConnectionSettings.DefaultIndex, query);
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
                qs = new QueryContainerDescriptor<object>()
                    .QueryString(qq => qq
                        .Query(query)
                        .DefaultOperator(Operator.And)
                    );
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
            System.Collections.Concurrent.ConcurrentBag<Exception> exceptions = new ();

            if (monitor != null)
                monitor.Start();

            var scrollAllObserver = new ScrollAllObserver<object>(
                onNext: response =>
                {
                    if (!response.SearchResponse.IsValid)
                    {
                        HlidacStatu.Util.Consts.Logger.Warning("Invalid response {response}", response.SearchResponse.DebugInformation);
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
                    HlidacStatu.Util.Consts.Logger.Error("Scroll error occured cl:{client} q:{query}", e, sourceESClient.ConnectionSettings.DefaultIndex, query);
                    res.ErrorOccurred = true;
                    res.Exception = e;
                    if (logOutputFunc != null)
                    {
                        logOutputFunc(e.ToString());
                    }
                    exceptions.Add(e);

                    _ = waitHandle.Set();
                },
                onCompleted: () => {

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