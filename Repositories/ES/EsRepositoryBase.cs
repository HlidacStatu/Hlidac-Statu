namespace HlidacStatu.Repositories.ES
{
    // public abstract class EsRepositoryBase<T> where T : class
    // {
    //     private ElasticClient _client;
    //
    //     /// <summary>
    //     /// If there is something to do before indexing, override this method
    //     /// </summary>
    //     protected abstract T BeforeSave(T item);
    //
    //     public T Get(string id)
    //     {
    //         if (id == null) throw new ArgumentNullException(nameof(id));
    //
    //         var response = _client.Get<T>(id);
    //
    //         return response.IsValid ? response.Source : null;
    //     }
    //
    //     public bool Exists(string id)
    //     {
    //         if (id == null) throw new ArgumentNullException(nameof(id));
    //
    //         var response = _client.DocumentExists<T>(id);
    //
    //         if (response.IsValid)
    //             return response.Exists;
    //
    //         throw response.OriginalException;
    //     }
    //
    //     public void Save(T data)
    //     {
    //         var fixedData = BeforeSave(data);
    //
    //         _client.IndexDocument<T>(fixedData);
    //     }
    //
    //     public  IEnumerable<T> GetAll(QueryContainer query,
    //         string scrollTimeout = "4m",
    //         int scrollSize = 300)
    //     {
    //         ISearchResponse<T> initialResponse = null;
    //         if (query is null)
    //         {
    //             initialResponse = _client.Search<T>(scr => scr
    //                 .From(0)
    //                 .Take(scrollSize)
    //                 .MatchAll()
    //                 .Scroll(scrollTimeout));
    //         }
    //         else
    //         {
    //             initialResponse = _client.Search<T>(scr => scr
    //                 .From(0)
    //                 .Take(scrollSize)
    //                 .Query(q => query)
    //                 .Scroll(scrollTimeout));
    //         }
    //
    //         if (!initialResponse.IsValid || string.IsNullOrEmpty(initialResponse.ScrollId))
    //         {
    //             var reason = initialResponse.ServerError.Error.Reason;
    //             _logger.Error("GetAll: initialResponse is not valid. Reason: {reason}", reason);
    //             throw new Exception(reason);
    //         }
    //
    //         if (!initialResponse.Documents.Any())
    //         {
    //             _logger.Debug("Initial response has no documents.");
    //         }
    //
    //         foreach (var documents in initialResponse.Documents)
    //         {
    //             yield return documents;
    //         }
    //
    //         string scrollid = initialResponse.ScrollId;
    //         bool lastResponseHadData = true;
    //         while (lastResponseHadData)
    //         {
    //             ISearchResponse<T> loopingResponse =
    //                 _client.Scroll<T>(scrollTimeout, scrollid);
    //
    //             if (!loopingResponse.IsValid)
    //             {
    //                 _logger.Error("GetAll: loopingResponse is not valid. Reason: {reason}"
    //                     , loopingResponse.ServerError.Error.Reason);
    //                 break;
    //             }
    //
    //             foreach (var documents in loopingResponse.Documents)
    //             {
    //                 yield return documents;
    //             }
    //
    //             scrollid = loopingResponse.ScrollId;
    //
    //             lastResponseHadData = loopingResponse.Documents.Any();
    //         }
    //
    //         _client.ClearScroll(new ClearScrollRequest(scrollid));
    //     }
    //
    //     // multithread, not async
    //     public IEnumerable<string> GetAllIds(int maxDegreeOfParallelism = 5, QueryContainer query = null)
    //     {
    //         IObservable<ScrollAllResponse<T>> scrollAllObservable;
    //         if (query is null)
    //         {
    //             scrollAllObservable = _client.ScrollAll<T>("4m", maxDegreeOfParallelism, sc => sc
    //                 .MaxDegreeOfParallelism(maxDegreeOfParallelism)
    //                 .Search(s => s
    //                     .Size(1000)
    //                     .Source(false)
    //                     .MatchAll()
    //                 )
    //             );
    //         }
    //         else
    //         {
    //             scrollAllObservable = _client.ScrollAll<T>("4m", maxDegreeOfParallelism, sc => sc
    //                 .MaxDegreeOfParallelism(maxDegreeOfParallelism)
    //                 .Search(s => s
    //                     .Size(1000)
    //                     .Source(false)
    //                     .Query(q => query)
    //                 )
    //             );
    //         }
    //
    //         var waitHandle = new ManualResetEvent(false);
    //         var allIds = new ConcurrentBag<string>();
    //         var scrollAllObserver = new ScrollAllObserver<T>(
    //             onNext: response =>
    //             {
    //                 if (!response.SearchResponse.IsValid)
    //                 {
    //                     _logger.Warning("Invalid response {response}", response.SearchResponse.DebugInformation);
    //                 }
    //
    //                 if (!response.SearchResponse.Hits.Any())
    //                 {
    //                     _logger.Warning("No results found in response {response}",
    //                         response.SearchResponse.DebugInformation);
    //                 }
    //
    //                 var ids = response.SearchResponse.Hits.Select(h => h.Id);
    //                 foreach (var id in ids)
    //                 {
    //                     allIds.Add(id);
    //                 }
    //             },
    //             onError: e => { _logger.Error("Scroll error occured", e); },
    //             onCompleted: () => waitHandle.Set()
    //         );
    //
    //         scrollAllObservable.Subscribe(scrollAllObserver);
    //
    //         waitHandle.WaitOne();
    //
    //         _logger.Info("Loaded {idCount} Ids in total.", allIds.Count);
    //
    //         return allIds;
    //     }
    //     
    //     public static DotaceSearchResult SimpleSearch(DotaceSearchResult search,
    //             bool withHighlighting = false,
    //             AggregationContainerDescriptor<Dotace> anyAggregation = null)
    //         {
    //             var page = search.Page - 1 < 0 ? 0 : search.Page - 1;
    //
    //             var sw = new StopWatchEx();
    //             sw.Start();
    //             search.OrigQuery = search.Q;
    //             search.Q = Tools.FixInvalidQuery(search.Q ?? "", QueryShorcuts(), QueryOperators);
    //
    //             ISearchResponse<T> res = null;
    //             try
    //             {
    //                 var simpleQuery = SimpleQueryCreator.GetSimpleQuery<T>(query, Irules);
    //                 
    //                 res = Manager.GetESClient_Dotace()
    //                     .Search<T>(s => s
    //                         .Size(search.PageSize)
    //                         .ExpandWildcards(Elasticsearch.Net.ExpandWildcards.All)
    //                         .From(page * search.PageSize)
    //                         .Query(q => GetSimpleQuery(search))
    //                         .Sort(ss => GetSort(search.Order))
    //                         .Highlight(h => Tools.GetHighlight<T>(withHighlighting))
    //                         .Aggregations(aggr => anyAggregation)
    //                         .TrackTotalHits((search.ExactNumOfResults || page * search.PageSize == 0)
    //                             ? true
    //                             : (bool?)null)
    //                     );
    //                 if (res.IsValid && withHighlighting &&
    //                     res.Shards.Failed > 0) //if some error, do it again without highlighting
    //                 {
    //                     res = Manager.GetESClient_Dotace()
    //                         .Search<T>(s => s
    //                             .Size(search.PageSize)
    //                             .ExpandWildcards(Elasticsearch.Net.ExpandWildcards.All)
    //                             .From(page * search.PageSize)
    //                             .Query(q => GetSimpleQuery(search))
    //                             .Sort(ss => GetSort(search.Order))
    //                             .Highlight(h => Tools.GetHighlight<T>(false))
    //                             .Aggregations(aggr => anyAggregation)
    //                             .TrackTotalHits(search.ExactNumOfResults || page * search.PageSize == 0
    //                                 ? true
    //                                 : (bool?)null)
    //                         );
    //                 }
    //             }
    //             catch (Exception e)
    //             {
    //                 AuditRepo.Add(Audit.Operations.Search, "", "", "Dotace", "error", search.Q, null);
    //                 if (res != null && res.ServerError != null)
    //                 {
    //                     Manager.LogQueryError<T>(res, "Exception, Orig query:"
    //                                                        + search.OrigQuery + "   query:"
    //                                                        + search.Q
    //                                                        + "\n\n res:" + search.ElasticResults.ToString()
    //                         , ex: e);
    //                 }
    //                 else
    //                 {
    //                     _logger.Error("", e);
    //                 }
    //
    //                 throw;
    //             }
    //
    //             sw.Stop();
    //
    //             AuditRepo.Add(Audit.Operations.Search, "", "", "Dotace", res.IsValid ? "valid" : "invalid", search.Q,
    //                 null);
    //
    //             if (res.IsValid == false)
    //             {
    //                 Manager.LogQueryError<T>(res, "Exception, Orig query:"
    //                                                    + search.OrigQuery + "   query:"
    //                                                    + search.Q
    //                                                    + "\n\n res:" + search.ElasticResults?.ToString()
    //                 );
    //             }
    //
    //             search.Total = res?.Total ?? 0;
    //             search.IsValid = res?.IsValid ?? false;
    //             search.ElasticResults = res;
    //             search.ElapsedTime = sw.Elapsed;
    //             return search;
    //         }
    // }
}