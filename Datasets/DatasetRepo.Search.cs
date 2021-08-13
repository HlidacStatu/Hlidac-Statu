using System;
using System.Collections.Generic;
using System.Linq;
using HlidacStatu.Entities;
using HlidacStatu.Repositories;
using HlidacStatu.Repositories.ES;
using HlidacStatu.Repositories.Searching;
using HlidacStatu.Repositories.Searching.Rules;
using Nest;

namespace HlidacStatu.Datasets
{
    public static partial class DatasetRepo
    {
        public static class Searching
        {
            public static string GetSpecificQueriesForDataset(DataSet ds, string mappingProperty, string query,
                bool addKeyword, string attrNameModif = "")
            {
                var props = ds.GetMappingList(mappingProperty, attrNameModif).ToArray();
                if (addKeyword)
                    for (int i = 0; i < props.Length; i++)
                    {
                        props[i] += ".keyword";
                    }

                var qSearch = SearchDataQuery(props, query);
                return qSearch;
            }

            public static Dictionary<DataSet, string> GetSpecificQueriesForDatasets(string mappingProperty,
                string query, bool addKeyword)
            {
                Dictionary<DataSet, string> queries = new Dictionary<DataSet, string>();
                foreach (var ds in DataSetDB.ProductionDataSets.Get())
                {
                    var qSearch = GetSpecificQueriesForDataset(ds, mappingProperty, query, addKeyword);
                    if (!string.IsNullOrEmpty(qSearch))
                    {
                        queries.Add(ds, qSearch);
                    }
                }

                return queries;
            }

            static string[] queryOperators = new string[]
            {
                "AND", "OR"
            };

            static string[] queryShorcuts = new string[]
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

            public static DataSearchResult SearchData(DataSet ds, string queryString, int page, int pageSize,
                string sort = null, bool excludeBigProperties = true, bool withHighlighting = false,
                bool exactNumOfResults = false)
            {
                Devmasters.DT.StopWatchEx sw = new Devmasters.DT.StopWatchEx();

                sw.Start();
                var query = Repositories.Searching.Tools.FixInvalidQuery(queryString, queryShorcuts, queryOperators);

                var res = _searchData(ds, query, page, pageSize, sort, excludeBigProperties, withHighlighting,
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

                    return new DataSearchResult()
                    {
                        ElapsedTime = sw.Elapsed,
                        Q = queryString,
                        IsValid = true,
                        Total = res.Total,
                        Result = res.Hits
                            .Select(m => Newtonsoft.Json.JsonConvert.SerializeObject(m.Source))
                            .Select(s =>
                                (dynamic) Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(s,
                                    expConverter)),

                        Page = page,
                        PageSize = pageSize,
                        DataSet = ds,
                        ElasticResultsRaw = res,
                    };
                }
                else
                    return new DataSearchResult()
                    {
                        ElapsedTime = sw.Elapsed,
                        Q = queryString,
                        IsValid = true,
                        Total = 0,
                        Result = new dynamic[] { },
                        Page = page,
                        PageSize = pageSize,
                        DataSet = ds,
                        ElasticResultsRaw = res,
                    };
            }

            public static DataSearchRawResult SearchDataRaw(DataSet ds, string queryString, int page, int pageSize,
                string sort = null, bool excludeBigProperties = true, bool withHighlighting = false,
                bool exactNumOfResults = false)
            {
                var query = Repositories.Searching.Tools.FixInvalidQuery(queryString, queryShorcuts, queryOperators);
                var res = _searchData(ds, query, page, pageSize, sort, excludeBigProperties, withHighlighting,
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
                    return new DataSearchRawResult()
                    {
                        Q = queryString,
                        IsValid = true,
                        Total = res.Total,
                        Result = res.Hits.Select(m =>
                            new Tuple<string, string>(m.Id, Newtonsoft.Json.JsonConvert.SerializeObject(m.Source))),
                        Page = page,
                        PageSize = pageSize,
                        DataSet = ds,
                        ElasticResultsRaw = res,
                        Order = sort ?? "0"
                    };
                else
                    return new DataSearchRawResult()
                    {
                        Q = queryString,
                        IsValid = true,
                        Total = 0,
                        Result = new List<Tuple<string, string>>(),
                        ElasticResultsRaw = res,
                        Page = page,
                        PageSize = pageSize,
                        DataSet = ds,
                        Order = sort ?? "0"
                    };
            }

            public static ISearchResponse<object> _searchData(DataSet ds, string queryString, int page, int pageSize,
                string sort = null,
                bool excludeBigProperties = true, bool withHighlighting = false, bool exactNumOfResults = false)
            {
                SortDescriptor<object> sortD = new SortDescriptor<object>();
                if (sort == "0")
                    sort = null;

                if (!string.IsNullOrEmpty(sort))
                {
                    var allProps = ds.GetMappingList();
                    var txtProps = ds.GetTextMappingList();

                    if (sort.EndsWith(DataSearchResult.OrderDesc) ||
                        sort.ToLower().EndsWith(DataSearchResult.OrderDescUrl))
                    {
                        sort = sort.Replace(DataSearchResult.OrderDesc, "").Replace(DataSearchResult.OrderDescUrl, "")
                            .Trim();
                        if (sort.EndsWith(".keyword", StringComparison.OrdinalIgnoreCase))
                            sort = sort.Replace(".keywork", "", StringComparison.OrdinalIgnoreCase).Trim();
                        if (allProps.Any(k=>string.Equals(k,sort, StringComparison.OrdinalIgnoreCase)))
                        {
                            if (txtProps.Any(k => string.Equals(k, sort, StringComparison.OrdinalIgnoreCase)))
                                sort = sort + ".keyword";
                            sortD = sortD.Field(sort, SortOrder.Descending);
                        }

                    }
                    else
                    {
                        sort = sort.Replace(DataSearchResult.OrderAsc, "").Replace(DataSearchResult.OrderAscUrl, "")
                            .Trim();
                        if (sort.EndsWith(".keyword", StringComparison.OrdinalIgnoreCase))
                            sort = sort.Replace(".keywork", "", StringComparison.OrdinalIgnoreCase).Trim();
                        if (allProps.Any(k => string.Equals(k, sort, StringComparison.OrdinalIgnoreCase)))
                        {
                            if (txtProps.Any(k => string.Equals(k, sort, StringComparison.OrdinalIgnoreCase)))
                                sort = sort + ".keyword";
                            sortD = sortD.Field(sort, SortOrder.Descending);
                        }
                        sortD = sortD.Field(sort, SortOrder.Ascending);
                    }

                }


                ElasticClient client = Manager.GetESClient(ds.DatasetId, idxType: Manager.IndexType.DataSource);

                QueryContainer qc = GetSimpleQuery(ds, queryString);


                page = page - 1;
                if (page < 0)
                    page = 0;
                if (page * pageSize > Repositories.Searching.Tools.MaxResultWindow)
                {
                    page = (Repositories.Searching.Tools.MaxResultWindow / pageSize) - 1;
                }

                //exclude big properties from result
                var maps = excludeBigProperties ? ds.GetMappingList("DocumentPlainText").ToArray() : new string[] { };


                var res = client
                    .Search<object>(s => s
                        .Size(pageSize)
                        .Source(ss => ss.Excludes(ex => ex.Fields(maps)))
                        .From(page * pageSize)
                        .Query(q => qc)
                        .Sort(ss => sortD)
                        .Highlight(h => Repositories.Searching.Tools.GetHighlight<Object>(withHighlighting))
                        .TrackTotalHits(exactNumOfResults || page * pageSize == 0 ? true : (bool?) null)
                    );

                //fix Highlighting for large texts
                if (withHighlighting
                    && res.Shards != null
                    && res.Shards.Failed > 0) //if some error, do it again without highlighting
                {
                    res = client
                        .Search<object>(s => s
                            .Size(pageSize)
                            .Source(ss => ss.Excludes(ex => ex.Fields(maps)))
                            .From(page * pageSize)
                            .Query(q => qc)
                            .Sort(ss => sortD)
                            .Highlight(h => Repositories.Searching.Tools.GetHighlight<Object>(false))
                            .TrackTotalHits(exactNumOfResults || page * pageSize == 0 ? true : (bool?) null)
                        );
                }

                AuditRepo.Add(Audit.Operations.Search, "", "", "Dataset." + ds.DatasetId,
                    res.IsValid ? "valid" : "invalid", queryString, null);

                return res;
            }


            static string simpleQueryOsobaPrefix = "___";

            //static RegexOptions regexQueryOption = RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline;
            public static QueryContainer GetSimpleQuery(DataSet ds, string query)
            {
                var idQuerypath = GetSpecificQueriesForDataset(ds, "id", "${q}", false);

                var icoQuerypath = GetSpecificQueriesForDataset(ds, "ICO", "${q}", false);
                //var osobaIdQuerypathToIco = GetSpecificQueriesForDataset(ds, "OsobaId", "${q}", true)
                //                + " OR ( " + simpleQueryOsobaPrefix + "osobaid" + simpleQueryOsobaPrefix + ":${v} )";

                var osobaIdQuerypath = GetSpecificQueriesForDataset(ds, "OsobaId", "${q}", true);

                List<IRule> irules = new List<IRule>
                {
                    new TransformPrefixWithValue("id:", idQuerypath, null),
                    new OsobaId("osobaid:", icoQuerypath, addLastCondition: osobaIdQuerypath),
                    new Holding(null, icoQuerypath),
                    //new TransformPrefix("osobaid","",null,false),

                    new TransformPrefixWithValue("ico:", icoQuerypath, null),
                };

                //datetime rules
                foreach (var m in ds.GetDatetimeMappingList())
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

                    string qpref = GetSpecificQueriesForDataset(ds, pref, "${q}", false);
                    string qpref_keyw = GetSpecificQueriesForDataset(ds, pref, "${q}", true);
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