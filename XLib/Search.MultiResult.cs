using Amazon.Runtime.Internal.Transform;
using HlidacStatu.Datasets;
using HlidacStatu.Entities;
using HlidacStatu.Repositories;
using HlidacStatu.Repositories.Searching;
using RazorEngine.Compilation.ImpromptuInterface.Dynamic;
using Serilog;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HlidacStatu.XLib
{
    public partial class Search
    {

        public class MultiResult
        {
            public System.TimeSpan TotalSearchTime { get; set; } = System.TimeSpan.Zero;
            public string Query { get; set; }
            public HlidacStatu.Searching.Search.GeneralResult<SearchPromo> SearchPromos { get; set; } = null;
            public SmlouvaSearchResult Smlouvy { get; set; } = null;
            public VerejnaZakazkaSearchData VZ { get; set; } = null;
            public OsobaSearchResult Osoby { get; set; } = null;
            public bool OsobaFtx = false;
            public HlidacStatu.Searching.Search.GeneralResult<Firma> Firmy { get; set; } = null;
            public Datasets.Search.DatasetMultiResult Datasets { get; set; }
            public InsolvenceFulltextSearchResult Insolvence { get; set; } = new();
            public DotaceSearchResult Dotace { get; set; } = null;
            public Searching.Search.GeneralResult<Lib.Data.External.Wordpress.Searching.Result> Wordpress { get; set; } = null;

            public Searching.Search.GeneralResult<Registration> DatasetRegistrations { get; set; } = null;
            public Searching.Search.GeneralResult<FullAnalysis> QueryAnalysis { get; set; } = null;
            public bool HasSmlouvy { get { return (Smlouvy != null && Smlouvy.HasResult); } }
            public bool HasVZ { get { return (VZ != null && VZ.HasResult); } }
            public bool HasOsoby { get { return (Osoby != null && Osoby.HasResult); } }
            public bool HasFirmy { get { return (Firmy != null && Firmy.HasResult); } }
            public bool HasDatasets { get { return (Datasets != null && Datasets.HasResult); } }
            public bool HasDatasetRegistrations { get { return (DatasetRegistrations != null && DatasetRegistrations.HasResult); } }
            public bool HasInsolvence { get { return Insolvence != null && Insolvence.HasResult; } }
            public bool HasDotace { get { return Dotace != null && Dotace.HasResult; } }
            public bool HasWordpress { get { return Wordpress != null && Wordpress.HasResult; } }
            public bool HasQueryAnalysis { get { return QueryAnalysis != null && QueryAnalysis.HasResult; } }

            public Dictionary<string, System.TimeSpan> SearchTimes()
            {
                Dictionary<string, System.TimeSpan> times = new Dictionary<string, System.TimeSpan>();
                if (Smlouvy != null)
                    times.Add("Smlouvy", Smlouvy.ElapsedTime);
                if (VZ != null)
                    times.Add("VZ", VZ.ElapsedTime);
                if (Osoby != null)
                    times.Add("Osoby", Osoby.ElapsedTime);
                if (Firmy != null)
                    times.Add("Firmy", Firmy.ElapsedTime);
                if (Datasets != null)
                    times.Add("Dataset.Total", Datasets.ElapsedTime);
                if (Datasets != null)
                {
                    foreach (var ds in Datasets.Results)
                    {
                        times.Add("Dataset." + ds.DataSet.DatasetId, ds.ElapsedTime);
                    }
                }
                if (DatasetRegistrations != null)
                    times.Add("DatasetRegistrations", DatasetRegistrations.ElapsedTime);

                if (Insolvence != null)
                    times.Add("Insolvence", Insolvence.ElapsedTime);
                if (Dotace != null)
                    times.Add("Dotace", Dotace.ElapsedTime);
                if (Wordpress != null)
                    times.Add("Wordpress", Wordpress.ElapsedTime);

                if (TotalSearchTime.Ticks > 0)
                    times.Add("Total", TotalSearchTime);
                return times;
            }

            public string SearchTimesReport(string delimiter = "\n")
            {
                var times = SearchTimes();
                if (times == null || times.Count() == 0)
                    return string.Empty;

                return times
                    .Select(kv => $"{kv.Key}: {kv.Value.TotalMilliseconds.ToString()}")
                    .Aggregate((f, s) => f + delimiter + s);
            }

            public bool IsValid
            {
                get
                {
                    return (Smlouvy?.IsValid ?? false)
                        && (VZ?.IsValid ?? false)
                        && (Osoby?.IsValid ?? false)
                        && (Firmy?.IsValid ?? false)
                        && (Datasets?.IsValid ?? false)
                        && (DatasetRegistrations?.IsValid ?? false)
                        && (Insolvence?.IsValid ?? false)
                        && (Dotace?.IsValid ?? false)
                        && (Wordpress?.IsValid ?? false)
                        && (QueryAnalysis?.IsValid ?? false)
                        ;
                }
            }

            public bool HasResults
            {
                get
                {
                    return HasSmlouvy || HasVZ || HasOsoby || HasFirmy || HasDatasets
                        || HasInsolvence || HasDotace || HasWordpress || HasQueryAnalysis 
                        || HasDatasetRegistrations ;
                }
            }

            public long Total
            {
                get
                {
                    long t = 0;
                    if (HasSmlouvy)
                        t += Smlouvy.Total;
                    if (HasVZ)
                        t += VZ.Total;
                    if (HasOsoby)
                        t += Osoby.Total;
                    if (HasFirmy)
                        t += Firmy.Total;
                    if (HasDatasets)
                        t += Datasets.Total;
                    if (HasInsolvence)
                        t += Insolvence.Total;
                    if (HasDotace)
                        t += Dotace.Total;
                    if (HasWordpress)
                        t += Wordpress.Total;
                    if (HasDatasets)
                        t += Wordpress.Total;

                    return t;
                }
            }

        }

        private static readonly ILogger _logger = Log.ForContext<Search>();

        [System.Flags]
        public enum PartsToSearch
        {
            None = 0,
            Smlouvy = 1 << 0,
            VZ = 1 << 1,
            Osoby = 1 << 2,
            Firmy = 1 << 3,
            Datasets = 1 << 4,
            Insolvence = 1 << 5,
            Wordpress = 1 << 6,
            QueryAnalysis = 1 << 7,
            SearchPromos = 1 << 8,
            Dotace = 1 << 9,
            DatasetDb = 1 << 10,
            All = ~0
        }
        public static async Task<MultiResult> GeneralSearchAsync(
            string query, int page = 1,
            PartsToSearch partsToSearch = PartsToSearch.All,
            bool showBeta = false, string order = null,
            System.Security.Principal.IPrincipal user = null,
            int smlouvySize = 10, int vzSize = 0, int firmySize = 20, int osobySize = 10, int insolvenceSize = 0,
            int dotaceSize = 0, int datasetSize = 0
            )
        {
            MultiResult res = new MultiResult() { Query = query };

            if (string.IsNullOrEmpty(query))
                return res;

            if (!(await Tools.ValidateQueryAsync(query)))
            {
                res.Smlouvy = new SmlouvaSearchResult();
                res.Smlouvy.Q = query;
                res.Smlouvy.IsValid = false;

                return res;
            }

            var totalsw = new Devmasters.DT.StopWatchEx();
            totalsw.Start();

            var taskList = new List<Task>();

            if (partsToSearch.HasFlag(PartsToSearch.Smlouvy))
            {
                taskList.Add(
                    Task.Run(async () =>
                    {
                        try
                        {
                            Devmasters.DT.StopWatchEx sw = new Devmasters.DT.StopWatchEx();
                            sw.Start();
                            res.Smlouvy = await SmlouvaRepo.Searching.SimpleSearchAsync(query, 1, smlouvySize, order,
                                anyAggregation: new Nest.AggregationContainerDescriptor<Smlouva>().Sum("sumKc",
                                    m => m.Field(f => f.CalculatedPriceWithVATinCZK))
                            );
                            sw.Stop();
                            res.Smlouvy.ElapsedTime = sw.Elapsed;
                        }
                        catch (System.Exception e)
                        {
                            _logger.Error(e, "MultiResult GeneralSearch for Smlouvy query" + query);
                        }
                    }));
            }

            if (partsToSearch.HasFlag(PartsToSearch.SearchPromos))
            {
                taskList.Add(
                Task.Run(async () =>
                {
                    try
                    {
                        Devmasters.DT.StopWatchEx sw = new Devmasters.DT.StopWatchEx();
                        sw.Start();
                        res.SearchPromos = await SearchPromoRepo.SearchPromoForHledejAsync(query, 1, 8);
                        sw.Stop();
                        res.SearchPromos.ElapsedTime = sw.Elapsed;
                    }
                    catch (System.Exception e)
                    {
                        _logger.Error(e, "MultiResult GeneralSearch for SearchPromos query" + query);
                    }
                }));
            }

            if (partsToSearch.HasFlag(PartsToSearch.Firmy))
            {
                taskList.Add(
                Task.Run(async () =>
                {
                    try
                    {
                        Devmasters.DT.StopWatchEx sw = new Devmasters.DT.StopWatchEx();
                        sw.Start();
                        res.Firmy = await FirmaRepo.Searching.SimpleSearchAsync(query, 0, firmySize);
                        sw.Stop();
                        res.Firmy.ElapsedTime = sw.Elapsed;
                    }
                    catch (System.Exception e)
                    {
                        _logger.Error(e, "MultiResult GeneralSearch for Firmy query" + query);
                    }
                }));
            }

            if (partsToSearch.HasFlag(PartsToSearch.VZ))
            {

                taskList.Add(
                Task.Run(async () =>
                {
                    try
                    {
                        Devmasters.DT.StopWatchEx sw = new Devmasters.DT.StopWatchEx();
                        sw.Start();
                        res.VZ = await VerejnaZakazkaRepo.Searching.SimpleSearchAsync(query, null, 1, vzSize, order);
                        sw.Stop();
                        res.VZ.ElapsedTime = sw.Elapsed;
                    }
                    catch (System.Exception e)
                    {
                        _logger.Error(e, "MultiResult GeneralSearch for Verejne zakazky query" + query);
                    }
                }));
            }

            if (partsToSearch.HasFlag(PartsToSearch.Osoby))
            {

                taskList.Add(
                Task.Run(async () =>
                {
                    try
                    {
                        Devmasters.DT.StopWatchEx sw = new Devmasters.DT.StopWatchEx();
                        sw.Start();
                        res.Osoby = await OsobaRepo.Searching.SimpleSearchAsync(query, 1, osobySize, OsobaRepo.Searching.OrderResult.Relevance);
                        sw.Stop();
                        res.Osoby.ElapsedTime = sw.Elapsed;
                    }
                    catch (System.Exception e)
                    {
                        _logger.Error(e, "MultiResult GeneralSearch for Osoba query" + query);
                    }
                }));
            }

            if (partsToSearch.HasFlag(PartsToSearch.Insolvence))
            {

                if (InsolvenceRepo.IsLimitedAccess(user) == false)
                {
                    taskList.Add(
                        Task.Run(async () =>
                        {
                            try
                            {
                                Devmasters.DT.StopWatchEx sw = new Devmasters.DT.StopWatchEx();
                                sw.Start();
                                var iqu = new InsolvenceFulltextSearchResult { Q = query, PageSize = insolvenceSize, Order = order };
                                res.Insolvence = iqu;
                                res.Insolvence = await InsolvenceRepo.Searching.SimpleFulltextSearchAsync(new InsolvenceFulltextSearchResult
                                {
                                    Q = query,
                                    PageSize = insolvenceSize,
                                    Order = order
                                });
                                sw.Stop();
                                res.Insolvence.ElapsedTime = sw.Elapsed;
                            }
                            catch (System.Exception e)
                            {
                                _logger.Error(e, "MultiResult GeneralSearch for insolvence query" + query);
                            }
                        }));
                }
            }

            if (partsToSearch.HasFlag(PartsToSearch.Dotace))
            {
                taskList.Add(
                Task.Run(async () =>
                {
                    try
                    {
                        Devmasters.DT.StopWatchEx sw = new Devmasters.DT.StopWatchEx();
                        sw.Start();
                        res.Dotace = await DotaceRepo.Searching.SimpleSearchAsync(
                                new DotaceSearchResult { Q = query, Page = 1, PageSize = dotaceSize, Order = order },
                                anyAggregation: new Nest.AggregationContainerDescriptor<Entities.Dotace.Dotace>().Sum("souhrn", s => s.Field(f => f.DotaceCelkem))
                            );
                        sw.Stop();
                        res.Dotace.ElapsedTime = sw.Elapsed;

                    }
                    catch (System.Exception e)
                    {
                        _logger.Error(e, "MultiResult GeneralSearch for insolvence query" + query);
                    }
                }));
            }

            if (partsToSearch.HasFlag(PartsToSearch.Wordpress))
            {

                taskList.Add(
                Task.Run(async () =>
                {
                    try
                    {
                        Devmasters.DT.StopWatchEx sw = new Devmasters.DT.StopWatchEx();
                        sw.Start();

                        var wpSearch = new HlidacStatu.Lib.Data.External.Wordpress.Searching(new System.Uri("http://texty.hlidacstatu.cz"));
                        var parsedQ = Repositories.Searching.Tools.CreateAutocompleteItemsFromQuery(query);
                        var wpRes = await wpSearch.SearchAsync(parsedQ, 1, 5);
                        res.Wordpress = new HlidacStatu.Searching.Search.GeneralResult<Lib.Data.External.Wordpress.Searching.Result>
                        (query, wpRes, 5)
                        {
                            Page = 1
                        };

                        sw.Stop();
                        res.Wordpress.ElapsedTime = sw.Elapsed;

                    }
                    catch (System.Exception e)
                    {
                        _logger.Error(e, "MultiResult GeneralSearch for Wordpress query" + query);
                    }
                }));
            }

            if (partsToSearch.HasFlag(PartsToSearch.Datasets))
            {

                taskList.Add(
                Task.Run(async () =>
                {
                    try
                    {
                        res.Datasets = await Datasets.Search.DatasetMultiResult.GeneralSearchAsync(query, null, 1, datasetSize);
                        if (res.Datasets.Exceptions.Count > 0)
                        {
                            _logger.Error(res.Datasets.GetExceptions(), "MultiResult GeneralSearch for DatasetMulti query " + query);
                        }
                    }
                    catch (System.Exception e)
                    {
                        _logger.Error(e, "MultiResult GeneralSearch for DatasetMulti query " + query);
                    }
                }));
            }

            if (partsToSearch.HasFlag(PartsToSearch.DatasetDb))
            {

                //hledání v názvech datasetů
                taskList.Add(
                Task.Run(async () =>
                {
                    try
                    {
                        Devmasters.DT.StopWatchEx sw = new Devmasters.DT.StopWatchEx();
                        sw.Start();
                        var searchRes = await DataSetDB.Instance.SearchInDatasetsAsync(query, 1, 20);
                        res.DatasetRegistrations = new Searching.Search.GeneralResult<Registration>
                        (query, searchRes, 20)
                        {
                            Page = 1
                        };

                        sw.Stop();
                        res.DatasetRegistrations.ElapsedTime = sw.Elapsed;

                    }
                    catch (System.Exception e)
                    {
                        _logger.Error(e, "MultiResult GeneralSearch for DatasetRegistrations query " + query);
                    }
                }));
            }

            
            if (partsToSearch.HasFlag(PartsToSearch.QueryAnalysis))
            {

                //QueryAnalysis
                taskList.Add(
                Task.Run(async () =>
                {
                    try
                    {
                        Devmasters.DT.StopWatchEx sw = new Devmasters.DT.StopWatchEx();
                        sw.Start();
                        var searchRes = await DataSetDB.Instance.SearchInDatasetsAsync(query, 1, 20);
                        res.DatasetRegistrations = new Searching.Search.GeneralResult<Registration>
                        (query, searchRes, 20)
                        {
                            Page = 1
                        };

                        sw.Stop();
                        res.DatasetRegistrations.ElapsedTime = sw.Elapsed;

                    }
                    catch (System.Exception e)
                    {
                        _logger.Error(e, "MultiResult GeneralSearch for DatasetRegistrations query " + query);
                    }
                }));
            }

            await Task.WhenAll(taskList);

            totalsw.Stop();
            res.TotalSearchTime = totalsw.Elapsed;

            return res;
        }


    }
}
