using System.Threading.Tasks;
using HlidacStatu.Entities;
using HlidacStatu.Lib.Analytics;
using HlidacStatu.Repositories.Cache;

namespace HlidacStatu.Repositories.Statistics
{
    public static partial class FirmaStatistics
    {
        public static async Task<StatisticsSubjectPerYear<Smlouva.Statistics.Data>> CachedHoldingStatisticsSmlouvyAsync(
            Firma firma, int? obor = null,
            bool forceUpdateCache = false)
        {
            if (forceUpdateCache)
                await StatisticsCache.InvalidateHoldingSmlouvyStatisticsAsync(firma, obor);

            return await StatisticsCache.GetHoldingSmlouvyStatisticsAsync(firma, obor);
        }
        
        public static async Task RemoveStatisticsAsync(Firma firma, int? obor)
        {
            await StatisticsCache.InvalidateHoldingSmlouvyStatisticsAsync(firma, obor);
            await StatisticsCache.InvalidateFirmaSmlouvyStatisticsAsync(firma, obor);
        }

        public static async Task<StatisticsSubjectPerYear<Smlouva.Statistics.Data>> CachedFirmaStatisticsSmlouvyAsync(Firma firma, int? obor,
            bool forceUpdateCache = false)
        {
            StatisticsSubjectPerYear<Smlouva.Statistics.Data> ret =
                new StatisticsSubjectPerYear<Smlouva.Statistics.Data>();

            if (string.IsNullOrWhiteSpace(firma.ICO))
            {
                return ret;
            }

            if (forceUpdateCache)
                await StatisticsCache.InvalidateFirmaSmlouvyStatisticsAsync(firma, obor);
            
            return await StatisticsCache.GetFirmaSmlouvyStatisticsAsync(firma, obor);
        }
    }
}