using System.Linq;
using System.Threading.Tasks;
using HlidacStatu.Caching;
using ZiggyCreatures.Caching.Fusion;

namespace HlidacStatu.Datasets;

public class DataSetCache
{
    internal static readonly IFusionCache Cache =
        HlidacStatu.Caching.CacheFactory.CreateNew(CacheFactory.CacheType.L1Default, nameof(DataSetDB));
    
    public static ValueTask<DataSet[]> GetAllDatasetsAsync() => Cache.GetOrSetAsync($"_AllDataSets", async _ =>
        {
            var ds = new HlidacStatu.Datasets.DataSet(DataSetDB.DataSourcesDbName, false);
            var searchData = await ds.SearchDataRawAsync("*", 1, 500);
            var datasets = searchData
                .Result
                .Select(s => DataSet.GetCachedDataset(s.Item1))
                .Where(d => d != null)
                .ToArray();

            return datasets;
        }
    );
    
    public static ValueTask<DataSet[]> GetProductionDatasetsAsync() => Cache.GetOrSetAsync($"_ProductionDataSets", async _ =>
        {
            var datasets = (await GetAllDatasetsAsync())
                .Where(d => d != null)
                .Where(d => d.Registration().betaversion == false
                            && d.Registration().hidden == false)
                .ToArray();

            return datasets;
        }
    );
}