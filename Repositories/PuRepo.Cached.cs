using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HlidacStatu.Entities;
using HlidacStatu.Repositories.Cache;
using ZiggyCreatures.Caching.Fusion;

namespace HlidacStatu.Repositories;

public static partial class PuRepo
{
    public static class Cached
    {
        public static async Task<int> GetPlatyAsync(int year)
        {
            return await CacheService.Cache.GetOrSetAsync<int>(
                $"platyCount_{year}",
                async _ => (await PuRepo.GetPlatyAsync(PuRepo.DefaultYear)).Count
            );
        }

        public static async Task<List<PuPlat>> GetPoziceDlePlatuAsync(int min, int max, int year)
        {
            return await CacheService.Cache.GetOrSetAsync<List<PuPlat>>(
                $"{nameof(PuRepo.GetPoziceDlePlatuAsync)}_{min}_{max}_{year}",
                async _ => await PuRepo.GetPoziceDlePlatuAsync(min, max, year)
            );
        }
        
        public static async Task<List<PuPlat>> GetTop100PlatuAsync(int year, string tag = null)
        {
            return await CacheService.Cache.GetOrSetAsync<List<PuPlat>>(
                $"{nameof(PuRepo.GetTop100PlatuAsync)}_{year}_{tag}",
                async _ => await PuRepo.GetTop100PlatuAsync(year, tag)
            );
        }

        public static async Task<PuOrganizace> GetFullDetailUpToYearAsync(string datovaSchranka, int? maxYear)
        {
            return await CacheService.Cache.GetOrSetAsync<PuOrganizace>(
                $"{nameof(PuRepo.GetFullDetailUpToYearAsync)}_{datovaSchranka}_{maxYear}",
                async _ => await PuRepo.GetFullDetailUpToYearAsync(datovaSchranka, maxYear)
            );
        }

        public static async Task<List<PuOrganizace>> GetActiveOrganizaceForTagAsync(string tag)
        {
            return await CacheService.Cache.GetOrSetAsync<List<PuOrganizace>>(
                $"{nameof(PuRepo.GetActiveOrganizaceForTagAsync)}_{tag}-urednici",
                _ => PuRepo.GetActiveOrganizaceForTagAsync(tag)
            );
        }

        public static async Task<PuPlat> GetPlatAsync(int id)
        {
            return await CacheService.Cache.GetOrSetAsync<PuPlat>(
                $"{nameof(PuRepo.GetPlatAsync)}_{id}-urednici",
                _ => PuRepo.GetPlatAsync(id)
            );
        }
    }
}