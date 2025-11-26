using HlidacStatu.Entities;
using HlidacStatu.Repositories;
using HlidacStatu.Repositories.Searching;
using HlidacStatu.Searching;

using Nest;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using HlidacStatu.Connectors;

namespace HlidacStatu.Datasets
{
    public static partial class DatasetRepo
    {
        public static class Searching
        {
            public static async Task<string> GetSpecificQueriesForDatasetAsync(DataSet ds, string mappingProperty, string query,
                bool addKeyword, string attrNameModif = "")
            {
                var props = (await ds.GetMappingListAsync(mappingProperty, attrNameModif)).ToArray();
                if (addKeyword)
                    for (int i = 0; i < props.Length; i++)
                    {
                        props[i] += ".keyword";
                    }

                var qSearch = SearchDataQuery(props, query);
                return qSearch;
            }

            public static async Task<Dictionary<DataSet, string>> GetSpecificQueriesForDatasetsAsync(string mappingProperty,
                string query, bool addKeyword)
            {
                Dictionary<DataSet, string> queries = new Dictionary<DataSet, string>();
                foreach (var ds in await DataSetCache.GetProductionDatasetsAsync())
                {
                    var qSearch = await GetSpecificQueriesForDatasetAsync(ds, mappingProperty, query, addKeyword);
                    if (!string.IsNullOrEmpty(qSearch))
                    {
                        queries.Add(ds, qSearch);
                    }
                }

                return queries;
            }

            public static string[] QueryOperators = new string[]
            {
                "AND", "OR"
            };

            public static string[] QueryShorcuts = new string[]
            {
                "ico:",
                "osobaid:",
                "holding:",
                "gps_lat:",
                "gps_lng:",
                "id:",
            };

            public static string SearchDataQuery(string[] properties, string value)
            {
                if (properties == null)
                    return string.Empty;
                if (properties.Count() == 0)
                    return string.Empty;
                //create query
                string q = properties
                    .Select(p => $"{p}:{value}")
                    .Aggregate((f, s) => f + " OR " + s);
                return $"( {q} )";
            }
            
            public static async Task<bool> CheckIfAnyRecordExistAsync(string query, IEnumerable<DataSet>? datasets = null)
            {
                
                [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_ContainedQuery")]
                static extern IQuery GetContainedQuery(QueryContainer c);
                
                static string UnpackQueryContainer(QueryContainer qc)
                {
                    try
                    {
                        var cq = GetContainedQuery(qc);
                        var queryStringQuery = (IQueryStringQuery)(QueryStringQueryDescriptor<object>)cq;
                        return queryStringQuery.Query;
                    }
                    catch (Exception e)
                    {
                        return qc.GetHashCode().ToString();
                    }
                }
                
                if (string.IsNullOrEmpty(query))
                    return false;

                datasets ??= await DataSetCache.GetProductionDatasetsAsync();

                var esClient = Manager.GetESClient();

                var esQuery = HlidacStatu.Searching.Tools.FixInvalidQuery(query, QueryShorcuts, QueryOperators);

                List<QueryContainer> queryContainers = new();
                foreach (var ds in datasets)
                {
                    QueryContainer qc = await GetSimpleQueryAsync(ds, esQuery);
                    queryContainers.Add(qc);
                }
                
                // get distinct query containers by its queries
                // also remove nonexisting icos 
                queryContainers = queryContainers
                    .Where(qc => !UnpackQueryContainer(qc).Contains("ico:noone", StringComparison.InvariantCultureIgnoreCase))
                    .DistinctBy(UnpackQueryContainer).ToList();

                string indexes = string.Join(",", datasets.Select(ds => $"{Manager.DataSourceIndexNamePrefix}{ds.DatasetId}"));
                
                var combinedQuery = new BoolQuery
                {
                    Should = queryContainers // Using Must, but you can use Should, MustNot, or Filter depending on your needs
                };
                
                var response = await esClient.SearchAsync<dynamic>(s => s
                        .Index(indexes) // Specify the indexes you want to search across
                        .Query(q => combinedQuery)
                        .Size(0) // We're only interested in the existence of documents, not the documents themselves
                );

                if (response.Total > 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
                

            }

            public static async Task<DataSearchResult> SearchDataAsync(DataSet ds, string queryString, int page, int pageSize,
                string sort = null, bool excludeBigProperties = true, bool withHighlighting = false,
                bool exactNumOfResults = false)
            {
                Devmasters.DT.StopWatchEx sw = new Devmasters.DT.StopWatchEx();

                sw.Start();
                var query = HlidacStatu.Searching.Tools.FixInvalidQuery(queryString, QueryShorcuts, QueryOperators);

                var res = await _searchDataAsync(ds, query, page, pageSize, sort, excludeBigProperties, withHighlighting,
                    exactNumOfResults);

                sw.Stop();
                if (!res.IsValid)
                {
                    throw DataSetException.GetExc(
                        ds.DatasetId,
                        ApiResponseStatus.InvalidSearchQuery.error.number,
                        ApiResponseStatus.InvalidSearchQuery.error.description,
                        queryString
                    );
                }

                if (res.Total > 0)
                {
                    var expConverter = new Newtonsoft.Json.Converters.ExpandoObjectConverter();

                    return new DataSearchResult(ds)
                    {
                        ElapsedTime = sw.Elapsed,
                        Q = queryString,
                        IsValid = true,
                        Total = res.Total,
                        Result = res.Hits
                            .Select(m => Newtonsoft.Json.JsonConvert.SerializeObject(m.Source))
                            .Select(s =>
                                (dynamic)Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(s,
                                    expConverter)),

                        Page = page,
                        PageSize = pageSize,                        
                        ElasticResultsRaw = res,
                    };
                }
                else
                    return new DataSearchResult(ds)
                    {
                        ElapsedTime = sw.Elapsed,
                        Q = queryString,
                        IsValid = true,
                        Total = 0,
                        Result = new dynamic[] { },
                        Page = page,
                        PageSize = pageSize,
                        ElasticResultsRaw = res,
                    };
            }

            public static async Task<DataSearchRawResult> SearchDataRawAsync(DataSet ds, string queryString, int page, int pageSize,
                string sort = null, bool excludeBigProperties = true, bool withHighlighting = false,
                bool exactNumOfResults = false)
            {
                var query = HlidacStatu.Searching.Tools.FixInvalidQuery(queryString, QueryShorcuts, QueryOperators);
                var res = await _searchDataAsync(ds, query, page, pageSize, sort, excludeBigProperties, withHighlighting,
                    exactNumOfResults);
                if (!res.IsValid)
                {
                    throw DataSetException.GetExc(ds.DatasetId,
                        ApiResponseStatus.InvalidSearchQuery.error.number,
                        ApiResponseStatus.InvalidSearchQuery.error.description,
                        queryString
                    );
                }

                if (res.Total > 0)
                    return new DataSearchRawResult(ds)
                    {
                        Q = queryString,
                        IsValid = true,
                        Total = res.Total,
                        Result = res.Hits.Select(m =>
                            new Tuple<string, string>(m.Id, Newtonsoft.Json.JsonConvert.SerializeObject(m.Source))),
                        Page = page,
                        PageSize = pageSize,
                        ElasticResultsRaw = res,
                        Order = sort ?? "0"
                    };
                else
                    return new DataSearchRawResult(ds)
                    {
                        Q = queryString,
                        IsValid = true,
                        Total = 0,
                        Result = new List<Tuple<string, string>>(),
                        ElasticResultsRaw = res,
                        Page = page,
                        PageSize = pageSize,
                        Order = sort ?? "0"
                    };
            }

            public static async Task<ISearchResponse<object>> _searchDataAsync(DataSet ds, string queryString, int page, int pageSize,
                string sort = null,
                bool excludeBigProperties = true, bool withHighlighting = false, bool exactNumOfResults = false)
            {
                SortDescriptor<object> sortD = new SortDescriptor<object>();
                if (sort == "0")
                    sort = null;

                if (!string.IsNullOrEmpty(sort))
                {
                    var allPropsTask = ds.GetMappingListAsync();
                    var txtPropsTask = ds.GetTextMappingListAsync();

                    var allProps = await allPropsTask;
                    var txtProps = await txtPropsTask;

                    if (sort.EndsWith(DataSearchResult.OrderDesc) ||
                        sort.ToLower().EndsWith(DataSearchResult.OrderDescUrl))
                    {
                        sort = sort.Replace(DataSearchResult.OrderDesc, "").Replace(DataSearchResult.OrderDescUrl, "")
                            .Trim();
                        if (sort.EndsWith(".keyword", StringComparison.OrdinalIgnoreCase))
                            sort = sort.Replace(".keyword", "", StringComparison.OrdinalIgnoreCase).Trim();
                        if (allProps.Any(k => string.Equals(k, sort, StringComparison.OrdinalIgnoreCase)))
                        {
                            var found = txtProps.FirstOrDefault(k => string.Equals(k, sort, StringComparison.OrdinalIgnoreCase));
                            if (found != null)
                                sort = found + ".keyword";
                            sortD = sortD.Field(sort, SortOrder.Descending);
                        }

                    }
                    else
                    {
                        sort = sort.Replace(DataSearchResult.OrderAsc, "").Replace(DataSearchResult.OrderAscUrl, "")
                            .Trim();
                        if (sort.EndsWith(".keyword", StringComparison.OrdinalIgnoreCase))
                            sort = sort.Replace(".keyword", "", StringComparison.OrdinalIgnoreCase).Trim();
                        if (allProps.Any(k => string.Equals(k, sort, StringComparison.OrdinalIgnoreCase)))
                        {
                            var found = txtProps.FirstOrDefault(k => string.Equals(k, sort, StringComparison.OrdinalIgnoreCase));
                            if (found != null)
                                sort = found + ".keyword";
                        }
                        sortD = sortD.Field(sort, SortOrder.Ascending);
                    }

                }

                ElasticClient client = Manager.GetESClient(ds.DatasetId, idxType: Manager.IndexType.DataSource);

                QueryContainer qc = await GetSimpleQueryAsync(ds, queryString);

                page = page - 1;
                if (page < 0)
                    page = 0;
                if (page * pageSize > Repositories.Searching.Tools.MaxResultWindow)
                {
                    page = (Repositories.Searching.Tools.MaxResultWindow / pageSize) - 1;
                }

                //exclude big properties from result
                var maps = excludeBigProperties ? (await ds.GetMappingListAsync("DocumentPlainText")).ToArray() : new string[] { };

                var res = await client
                    .SearchAsync<object>(s => s
                        .Size(pageSize)
                        .Source(ss => ss.Excludes(ex => ex.Fields(maps)))
                        .From(page * pageSize)
                        .Query(q => qc)
                        .Sort(ss => sortD)
                        .Highlight(h => Repositories.Searching.Tools.GetHighlight<Object>(withHighlighting))
                        .TrackTotalHits(exactNumOfResults || page * pageSize == 0 ? true : (bool?)null)
                    );

                //fix Highlighting for large texts
                if (withHighlighting
                    && res.Shards != null
                    && res.Shards.Failed > 0) //if some error, do it again without highlighting
                {
                    res = await client
                        .SearchAsync<object>(s => s
                            .Size(pageSize)
                            .Source(ss => ss.Excludes(ex => ex.Fields(maps)))
                            .From(page * pageSize)
                            .Query(q => qc)
                            .Sort(ss => sortD)
                            .Highlight(h => Repositories.Searching.Tools.GetHighlight<Object>(false))
                            .TrackTotalHits(exactNumOfResults || page * pageSize == 0 ? true : (bool?)null)
                        );
                }

                AuditRepo.Add(Audit.Operations.Search, "", "", "Dataset." + ds.DatasetId,
                    res.IsValid ? "valid" : "invalid", queryString, null);

                return res;
            }

            //static RegexOptions regexQueryOption = RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline;
            public static async Task<QueryContainer> GetSimpleQueryAsync(DataSet ds, string query)
            {
                var taskId = GetSpecificQueriesForDatasetAsync(ds, "id", "${q}", false);
                var taskIco = GetSpecificQueriesForDatasetAsync(ds, "ICO", "${q}", false);
                var taskOsoba = GetSpecificQueriesForDatasetAsync(ds, "OsobaId", "${q}", true);

                var idQuerypath = await taskId;
                var icoQuerypath = await taskIco;
                var osobaIdQuerypath = await taskOsoba;

                List<IRule> irules = new List<IRule>
                {
                    new TransformPrefixWithValue("id:", idQuerypath, null),
                    new OsobaId(HlidacStatu.Repositories.OsobaVazbyRepo.Icos_s_VazbouNaOsobu, "osobaid:", icoQuerypath, addLastCondition: osobaIdQuerypath),
                    new Holding(HlidacStatu.Repositories.FirmaVazbyRepo.IcosInHolding, null, icoQuerypath),

                    new TransformPrefixWithValue("ico:", icoQuerypath, null),
                };

                //datetime rules
                foreach (var m in await ds.GetDatetimeMappingListAsync())
                {
                    //irules.Add(new TransformPrefix(m + ":", m + ":", "[<>]?[{\\[]+"));
                    irules.Add(new TransformPrefixWithValue(m + ":",
                        m + ":[${q}T00:00:00+02:00 TO ${q}T00:00:00+02:00||+1d}", "^\\d{2,4}-\\d{2}-\\d{2}"));
                }


                // add searched prefixes
                string[] existingPrefixes = irules.SelectMany(m => m.Prefixes)
                    .Where(m => !string.IsNullOrEmpty(m))
                    .ToArray();

                var qp = SplittingQuery.SplitQuery(query);
                string[] foundPrefixes = qp.Parts.Select(m => m.Prefix)
                    .Where(m => !string.IsNullOrEmpty(m))
                    .Where(m => !existingPrefixes.Contains(m))
                    .ToArray();

                foreach (var fPref in foundPrefixes)
                {
                    var pref = fPref.Substring(0, fPref.Length - 1);

                    var t1 = GetSpecificQueriesForDatasetAsync(ds, pref, "${q}", false);
                    var t2 = GetSpecificQueriesForDatasetAsync(ds, pref, "${q}", true);
                    
                    string qpref = await t1;
                    string qpref_keyw = await t2;
                    string prefPath = "";
                    if (!string.IsNullOrWhiteSpace(qpref)
                        && !string.IsNullOrWhiteSpace(qpref_keyw)
                    )
                        prefPath = $"( {qpref} OR {qpref_keyw} )";
                    else if (!string.IsNullOrWhiteSpace(qpref)
                             && string.IsNullOrWhiteSpace(qpref_keyw))
                        prefPath = $" {qpref} ";
                    else if (!string.IsNullOrWhiteSpace(qpref_keyw)
                             && string.IsNullOrWhiteSpace(qpref))
                        prefPath = $" {qpref_keyw} ";

                    if (!string.IsNullOrWhiteSpace(prefPath))
                    {
                        //rules.Add(new Lib.Search.Rule(fPref, prefPath));
                        irules.Add(new TransformPrefixWithValue(fPref, prefPath, null));
                    }

                }

                var qc = SimpleQueryCreator.GetSimpleQuery<object>(query, irules.ToArray());

                return qc;
            }
        }
    }
}