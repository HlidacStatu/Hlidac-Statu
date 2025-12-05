using HlidacStatu.Entities;
using HlidacStatu.Lib.Analytics;
using System.Threading.Tasks;
using HlidacStatu.Repositories.Cache;

namespace HlidacStatu.Repositories.Statistics
{
    public static partial class FirmaStatistics
    {
        public static async Task<StatisticsSubjectPerYear<Firma.Statistics.Dotace>> CachedHoldingStatisticsDotace(
            Firma firma, bool forceUpdateCache = false, bool invalidateOnly = false)
        {
            if (forceUpdateCache || invalidateOnly)
                await StatisticsCache.InvalidateHoldingDotaceStatisticsAsync(firma);

            if (invalidateOnly)
                return new();
            else
                return await StatisticsCache.GetHoldingDotaceStatisticsAsync(firma);
        }

        public static async Task<StatisticsSubjectPerYear<Firma.Statistics.Dotace>> CachedStatisticsDotaceAsync(
            Firma firma,
            bool forceUpdateCache = false)
        {
            if (forceUpdateCache)
            {
                await StatisticsCache.InvalidateFirmaDotaceStatisticsAsync(firma);
            }
            
            return await StatisticsCache.GetFirmaDotaceStatisticsAsync(firma);
            
        }
    }
}