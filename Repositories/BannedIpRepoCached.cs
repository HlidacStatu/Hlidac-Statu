using Devmasters.Cache.LocalMemory;

using HlidacStatu.Entities;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HlidacStatu.Repositories
{
    public class BannedIpRepoCached
    {
        private const string CacheKey = "BannedIps_service";
        private static readonly AutoUpdatedLocalMemoryCache<List<BannedIp>> _cache;

        static BannedIpRepoCached()
        {
            _cache = new AutoUpdatedLocalMemoryCache<List<BannedIp>>(
                TimeSpan.FromSeconds(30),
                CacheKey,
                (_) => BannedIpRepo.GetBannedIps()
            );
        }

        public static List<BannedIp> GetBannedIps()
        {
            return _cache.Get();
        }

        public static async Task BanIpAsync(string ipAddress, DateTime expiration, int lastStatusCode, string pathList)
        {
            await BannedIpRepo.BanIpAsync(ipAddress, expiration, lastStatusCode, pathList);
            _cache.ForceRefreshCache();
        }

        public static async Task AllowIpAsync(string ipAddress)
        {
            await BannedIpRepo.AllowIpAsync(ipAddress);
            _cache.ForceRefreshCache();
        }
    }
}