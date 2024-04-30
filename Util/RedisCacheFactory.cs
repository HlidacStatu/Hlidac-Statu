using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;

namespace HlidacStatu.Util;

public static class RedisCacheFactory
{
 
    public static FusionCache CreateCache(string redisConnectionString, string cacheName)
    {
        // INSTANTIATE A REDIS DISTRIBUTED CACHE
        var redis = new RedisCache(new RedisCacheOptions() { Configuration = redisConnectionString });

        // INSTANTIATE THE FUSION CACHE SERIALIZER
        var seroptions = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true,
            IncludeFields = true
        };
        var serializer = new FusionCacheSystemTextJsonSerializer(seroptions);

        // INSTANTIATE FUSION CACHE
        var cache = new FusionCache(new FusionCacheOptions()
        {
            CacheName = cacheName,
            CacheKeyPrefix = cacheName,
            DefaultEntryOptions = new FusionCacheEntryOptions()
            {
                Duration = TimeSpan.FromMinutes(1), //základní doba trvanlivosti
                IsFailSafeEnabled = true,
                FailSafeThrottleDuration = TimeSpan.FromSeconds(30), //základní doba trvanlivosti se posune o minutu, pokud je db down
                FailSafeMaxDuration = TimeSpan.FromMinutes(3), //maximální doba trvanlivosti - po této době se starý záznam maže
                FactorySoftTimeout = TimeSpan.FromMilliseconds(150), //kolik času má factory na odpověď při expiraci základní doby trvanlivosti. Jinak se použije expirovaný záznam
                
                DistributedCacheDuration = TimeSpan.FromDays(7), //základní doba trvanlivosti v distributed cache
                DistributedCacheFailSafeMaxDuration = TimeSpan.FromDays(365), //maximální doba trvanlivosti v distributed cache
                AllowBackgroundDistributedCacheOperations = true,
                DistributedCacheSoftTimeout = TimeSpan.FromMilliseconds(300)
                
            }
        });
            
        // SETUP THE DISTRIBUTED 2ND LEVEL
        cache.SetupDistributedCache(redis, serializer);

        return cache;
    }
}