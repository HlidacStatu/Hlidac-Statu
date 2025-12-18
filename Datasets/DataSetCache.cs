using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HlidacStatu.Caching;
using ZiggyCreatures.Caching.Fusion;

namespace HlidacStatu.Datasets;

public class DataSetCache
{
    private static readonly object _memoryCachelock = new object();
    private static IFusionCache _memoryCache;
    internal static IFusionCache MemoryCache
    {
        get
        {
            if (_memoryCache == null)
            {
                lock (_memoryCachelock)
                {
                    _memoryCache ??= HlidacStatu.Caching.CacheFactory.CreateNew(
                        CacheFactory.CacheType.L1Default,
                        nameof(DataSetDB));
                }
            }

            return _memoryCache;
        }
    }
    
    public static ValueTask<DataSet[]> GetAllDatasetsAsync() => MemoryCache.GetOrSetAsync($"_AllDataSets", async _ =>
        {
            var ds = await HlidacStatu.Datasets.DataSet.CreateDataSetInstanceAsync(DataSetDB.DataSourcesDbName, false);
            var searchData = await ds.SearchDataRawAsync("*", 1, 500);
            var datasets = searchData
                .Result
                .Select(s => DataSet.GetCachedDataset(s.Item1))
                .Where(d => d != null)
                .ToArray();

            return datasets;
        }
    );
    
    public static ValueTask<DataSet[]> GetProductionDatasetsAsync() => MemoryCache.GetOrSetAsync($"_ProductionDataSets", async _ =>
        {
            var results = new List<DataSet>();
            var datasets = (await GetAllDatasetsAsync())
                .Where(d => d != null);

            foreach (var dataset in datasets)
            {
                var registration = await dataset.RegistrationAsync();
                if(registration.betaversion == false && registration.hidden == false)
                    results.Add(dataset);
            }
            
            return results.ToArray();
        }
    );
}