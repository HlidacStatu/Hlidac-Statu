using HlidacStatu.Entities;
using HlidacStatu.Repositories;
using HlidacStatu.Repositories.Searching;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HlidacStatu.Datasets;

namespace HlidacStatu.XLib
{
    public class Search
    {

        public class MultiResult
        {
            public System.TimeSpan TotalSearchTime { get; set; } = System.TimeSpan.Zero;
            public System.TimeSpan AddOsobyTime { get; set; } = System.TimeSpan.Zero;
            public string Query { get; set; }
            public SmlouvaSearchResult Smlouvy { get; set; } = null;
            public VerejnaZakazkaSearchData VZ { get; set; } = null;
            public OsobaSearchResult Osoby { get; set; } = null;
            public bool OsobaFtx = false;
            public Repositories.Searching.Search.GeneralResult<Firma> Firmy { get; set; } = null;
            public Datasets.Search.DatasetMultiResult Datasets { get; set; }
            public InsolvenceSearchResult Insolvence { get; set; } = null;
            public DotaceSearchResult Dotace { get; set; } = null;

            public List<Registration> DatasetRegistrations { get; set; } = new();

            public bool HasSmlouvy { get { return (Smlouvy != null && Smlouvy.HasResult); } }
            public bool HasVZ { get { return (VZ != null && VZ.HasResult); } }
            public bool HasOsoby { get { return (Osoby != null && Osoby.HasResult); } }
            public bool HasFirmy { get { return (Firmy != null && Firmy.HasResult); } }
            public bool HasDatasets { get { return (Datasets != null && Datasets.HasResult); } }
            public bool HasInsolvence { get { return Insolvence != null && Insolvence.HasResult; } }
            public bool HasDotace { get { return Dotace != null && Dotace.HasResult; } }

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
                if (Firmy != null)
                    times.Add("Dataset.Total", Datasets.ElapsedTime);
                if (Datasets != null)
                {
                    foreach (var ds in Datasets.Results)
                    {
                        times.Add("Dataset." + ds.DataSet.DatasetId, ds.ElapsedTime);
                    }
                }
                if (Insolvence != null)
                    times.Add("Insolvence", Insolvence.ElapsedTime);
                if (Dotace != null)
                    times.Add("Dotace", Dotace.ElapsedTime);
                if (AddOsobyTime.Ticks > 0)
                    times.Add("AddOsobyTime", AddOsobyTime);

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
                        && (Insolvence?.IsValid ?? false)
                        && (Dotace?.IsValid ?? false)
                        ;
                }
            }

            public bool HasResults
            {
                get
                {
                    return HasSmlouvy || HasVZ || HasOsoby || HasFirmy || HasDatasets || HasInsolvence || HasDotace;
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

                    return t;
                }
            }

        }

        public static async Task<MultiResult> GeneralSearchAsync(string query, int page = 1, int pageSize = 10, bool showBeta = false, string order = null)
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

            taskList.Add(
                Task.Run(async () =>
                {
                    try
                    {
                        Devmasters.DT.StopWatchEx sw = new Devmasters.DT.StopWatchEx();
                        sw.Start();
                        res.Smlouvy = await SmlouvaRepo.Searching.SimpleSearchAsync(query, 1, pageSize, order,
                            anyAggregation: new Nest.AggregationContainerDescriptor<Smlouva>().Sum("sumKc",
                                m => m.Field(f => f.CalculatedPriceWithVATinCZK))
                        );
                        sw.Stop();
                        res.Smlouvy.ElapsedTime = sw.Elapsed;
                    }
                    catch (System.Exception e)
                    {
                        Util.Consts.Logger.Error("MultiResult GeneralSearch for Smlouvy query" + query, e);
                    }
                }));
            
            taskList.Add(
                Task.Run(async () =>
                {
                    try
                    {
                        Devmasters.DT.StopWatchEx sw = new Devmasters.DT.StopWatchEx();
                        sw.Start();
                        res.Firmy = await FirmaRepo.Searching.SimpleSearchAsync(query, 0, 50);
                        sw.Stop();
                        res.Firmy.ElapsedTime = sw.Elapsed;
                    }
                    catch (System.Exception e)
                    {
                        Util.Consts.Logger.Error("MultiResult GeneralSearch for Firmy query" + query, e);
                    }
                }));
            
            taskList.Add(
                Task.Run(async () =>
                {
                    try
                    {
                        Devmasters.DT.StopWatchEx sw = new Devmasters.DT.StopWatchEx();
                        sw.Start();
                        res.VZ = await VerejnaZakazkaRepo.Searching.SimpleSearchAsync(query, null, 1, pageSize, order);
                        sw.Stop();
                        res.VZ.ElapsedTime = sw.Elapsed;
                    }
                    catch (System.Exception e)
                    {
                        Util.Consts.Logger.Error("MultiResult GeneralSearch for Verejne zakazky query" + query, e);
                    }
                }));
            
            taskList.Add(
                Task.Run(async () =>
                {
                    try
                    {
                        Devmasters.DT.StopWatchEx sw = new Devmasters.DT.StopWatchEx();
                        sw.Start();
                        res.Osoby = await OsobaRepo.Searching.SimpleSearchAsync(query, 1, 10, OsobaRepo.Searching.OrderResult.Relevance);
                        sw.Stop();
                        res.Osoby.ElapsedTime = sw.Elapsed;
                    }
                    catch (System.Exception e)
                    {
                        Util.Consts.Logger.Error("MultiResult GeneralSearch for Osoba query" + query, e);
                    }
                }));

            taskList.Add(
                Task.Run(async() =>
                {
                    try
                    {
                        Devmasters.DT.StopWatchEx sw = new Devmasters.DT.StopWatchEx();
                        sw.Start();
                        var iqu = new InsolvenceSearchResult { Q = query, PageSize = pageSize, Order = order };
                        res.Insolvence = iqu;
                        res.Insolvence = await InsolvenceRepo.Searching.SimpleSearchAsync(new InsolvenceSearchResult { Q = query, PageSize = pageSize, Order = order });
                        sw.Stop();
                        res.Insolvence.ElapsedTime = sw.Elapsed;
                    }
                    catch (System.Exception e)
                    {
                        Util.Consts.Logger.Error("MultiResult GeneralSearch for insolvence query" + query, e);
                    }
                }));
            
            taskList.Add(
                Task.Run(async () =>
                {
                    try
                    {
                        Devmasters.DT.StopWatchEx sw = new Devmasters.DT.StopWatchEx();
                        sw.Start();
                        res.Dotace = await DotaceRepo.Searching.SimpleSearchAsync(
                                new DotaceSearchResult { Q = query, PageSize = pageSize, Order = order },
                                anyAggregation: new Nest.AggregationContainerDescriptor<Entities.Dotace.Dotace>().Sum("souhrn", s => s.Field(f => f.DotaceCelkem))
                            );
                        sw.Stop();
                        res.Dotace.ElapsedTime = sw.Elapsed;

                    }
                    catch (System.Exception e)
                    {
                        Util.Consts.Logger.Error("MultiResult GeneralSearch for insolvence query" + query, e);
                    }
                }));

            taskList.Add(
                Task.Run(async () =>
                {
                    try
                    {
                        res.Datasets = await Datasets.Search.DatasetMultiResult.GeneralSearchAsync(query, null, 1, 5);
                        if (res.Datasets.Exceptions.Count > 0)
                        {
                            Util.Consts.Logger.Error("MultiResult GeneralSearch for DatasetMulti query " + query,
                                res.Datasets.GetExceptions());
                        }
                    }
                    catch (System.Exception e)
                    {
                        Util.Consts.Logger.Error("MultiResult GeneralSearch for DatasetMulti query " + query, e);
                    }
                }));
            
            //hledání v názvech datasetů
            taskList.Add(
                Task.Run(async () =>
                {
                    try
                    {
                        res.DatasetRegistrations = await DataSetDB.Instance.SearchInDatasetsAsync(query, 1, 500);
                    }
                    catch (System.Exception e)
                    {
                        Util.Consts.Logger.Error("MultiResult GeneralSearch for DatasetRegistrations query " + query, e);
                    }
                }));

            await Task.WhenAll(taskList);

            totalsw.Stop();
            res.TotalSearchTime = totalsw.Elapsed;

            return res;
        }


    }
}
