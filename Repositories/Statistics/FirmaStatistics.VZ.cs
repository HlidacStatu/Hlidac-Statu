using System.Threading.Tasks;
using HlidacStatu.Entities;
using HlidacStatu.Lib.Analytics;
using HlidacStatu.Repositories.Cache;

namespace HlidacStatu.Repositories.Statistics
{
    public static partial class FirmaStatistics
    {
        public static async Task<StatisticsSubjectPerYear<Firma.Statistics.VZ>> CachedHoldingStatisticsVZAsync(
            Firma firma,
            bool forceUpdateCache = false)
        {
            if (forceUpdateCache)
                await StatisticsCache.InvalidateHoldingVzStatisticsAsync(firma);

            return await StatisticsCache.GetHoldingVzStatisticsAsync(firma);
        }
        

        public static async Task<StatisticsSubjectPerYear<Firma.Statistics.VZ>> CachedStatisticsVZ(
            Firma firma,
            bool forceUpdateCache = false)
        {
            if (forceUpdateCache)
                await StatisticsCache.InvalidateFirmaVzStatisticsAsync(firma);

            return await StatisticsCache.GetFirmaVzStatisticsAsync(firma);
        }
    }
}