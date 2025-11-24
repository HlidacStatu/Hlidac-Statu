using Enyim.Caching;
using Enyim.Caching.Memcached;
using HlidacStatu.CachingClients.PostgreSql;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;

namespace Caching;

public static class CacheFactory
{
    /// <summary>
    /// SCÉNÁŘ 1: řešíme když data expirují rychle - statistiky webů se mění co 5 minut
    /// duration 5 minut
    /// failsafe 6 hodin
    /// nepotřeba distributed cache
    /// Je potřeba nastavit cacheName i prefixName
    /// </summary>
    public static FusionCacheEntryOptions L1DefaultEntryOptions => new FusionCacheEntryOptions
    {
        IsFailSafeEnabled = true,
        // o kolik se prodlouží validita cache, pokud selže factory
        // až do FailSafeMaxDuration/DistributedCacheFailSafeMaxDuration
        FailSafeThrottleDuration = TimeSpan.FromMinutes(3),

        // Délka životnosti
        //L1
        Duration = TimeSpan.FromMinutes(5),
        FailSafeMaxDuration = TimeSpan.FromHours(6),
        // po 80% uběhlého času z Duration se spustí factory, abychom měli čerstvá data
        EagerRefreshThreshold = 0.8f,

        // TIMEOUTS
        //jak dlouho se pokouší načítat z factory, pokud nenačte, použije failsafe
        FactorySoftTimeout = TimeSpan.FromMilliseconds(500),
        //Kolik je maximální čas pro načítání dat, než to hodí exception
        FactoryHardTimeout = TimeSpan.FromSeconds(20),
        //pokud factory trvá dlouho, nevypne se, ale zkouší to dokončit - umí běžet i delší dobu než je factory hard timeout
        AllowTimedOutFactoryBackgroundCompletion = true,

        // DISTRIBUTED CACHE Vypneme
        SkipDistributedCacheRead = true,
        SkipDistributedCacheWrite = true,
        // vypneme backplane, protože nebudeme řešit synchronizace pro více instancí aplikace (zatím)
        AllowBackgroundBackplaneOperations = false,
        SkipBackplaneNotifications = true
    };


    /// <summary>
    /// SCÉNÁŘ 2: řešíme kdy se data mění pomaleji (hodiny)
    /// duration 10 minut
    /// failsafe 6 hodin
    /// distributed failsafe duration - v L2 max 1 den
    /// Je potřeba nastavit cacheName i prefixName
    /// </summary>
    public static FusionCacheEntryOptions DistributedCacheEntryOptions => new FusionCacheEntryOptions
    {
        IsFailSafeEnabled = true,
        // o kolik se prodlouží validita cache, pokud selže factory
        // až do FailSafeMaxDuration/DistributedCacheFailSafeMaxDuration
        FailSafeThrottleDuration = TimeSpan.FromMinutes(3),

        // Délka životnosti
        //L1
        Duration = TimeSpan.FromMinutes(10),
        FailSafeMaxDuration = TimeSpan.FromHours(6),
        //L2
        DistributedCacheFailSafeMaxDuration = TimeSpan.FromDays(1),
        // po 80% uběhlého času z Duration se spustí factory, abychom měli čerstvá data
        EagerRefreshThreshold = 0.8f,

        //! distributed cache duration je zbytečnej a boří funkcionalitu - nepoužívat 

        // TIMEOUTS
        //jak dlouho se pokouší načítat z factory, pokud nenačte, použije failsafe
        FactorySoftTimeout = TimeSpan.FromMilliseconds(500),
        //Kolik je maximální čas pro načítání dat, než to hodí exception
        FactoryHardTimeout = TimeSpan.FromSeconds(20),
        //pokud factory trvá dlouho, nevypne se, ale zkouší to dokončit - umí běžet i delší dobu než je factory hard timeout
        AllowTimedOutFactoryBackgroundCompletion = true,

        //jak dlouho se pokouší naččíst z L2, pokud nenačte použije failsafe lokalní
        DistributedCacheSoftTimeout = TimeSpan.FromSeconds(1),
        //pokud není failsafe lokální zkusí načítat z cache do max času až xxx
        DistributedCacheHardTimeout = TimeSpan.FromSeconds(10),

        // DISTRIBUTED CACHE OPTIONS
        AllowBackgroundDistributedCacheOperations = true,
        // vypneme backplane, protože nebudeme řešit synchronizace pro více instancí aplikace (zatím)
        AllowBackgroundBackplaneOperations = false,
        SkipBackplaneNotifications = true
    };
    // DistributedCacheCircuitBreakerDuration = TimeSpan.FromSeconds(10),


    /// <summary>
    /// SCÉNÁŘ 3: NEMÁ EXPIRACI! řešíme, kdy jsou data přepočítaná ručně odjinud (nevíme interval)
    /// duration 10 minut
    /// failsafe 6 hodin
    /// nepotřeba distributed cache
    /// FACTORY metoda je přímo volání do předpočítaných dat, předpočítaná data se updatuje z DOWNLOADERU
    /// Je potřeba nastavit cacheName i prefixName
    /// </summary>
    public static FusionCacheEntryOptions L1FromPermanentStoreEntryOptions => new FusionCacheEntryOptions
    {
        IsFailSafeEnabled = true,
        // o kolik se prodlouží validita cache, pokud selže factory
        // až do FailSafeMaxDuration/DistributedCacheFailSafeMaxDuration
        FailSafeThrottleDuration = TimeSpan.FromMinutes(3),

        // Délka životnosti
        //L1
        Duration = TimeSpan.FromMinutes(5),
        FailSafeMaxDuration = TimeSpan.FromHours(6),
        // po 80% uběhlého času z Duration se spustí factory, abychom měli čerstvá data
        EagerRefreshThreshold = 0.8f,

        // TIMEOUTS
        //jak dlouho se pokouší načítat z factory, pokud nenačte, použije failsafe
        FactorySoftTimeout = TimeSpan.FromMilliseconds(500),
        //Kolik je maximální čas pro načítání dat, než to hodí exception
        FactoryHardTimeout = TimeSpan.FromSeconds(20),
        //pokud factory trvá dlouho, nevypne se, ale zkouší to dokončit - umí běžet i delší dobu než je factory hard timeout
        AllowTimedOutFactoryBackgroundCompletion = true,

        // DISTRIBUTED CACHE Vypneme
        SkipDistributedCacheRead = true,
        SkipDistributedCacheWrite = true,
        // vypneme backplane, protože nebudeme řešit synchronizace pro více instancí aplikace (zatím)
        AllowBackgroundBackplaneOperations = false,
        SkipBackplaneNotifications = true
    };


    public enum CacheType
    {
        L1Default,
        L1FromPermanentStore,
        L2PostgreSql,
        L2Memcache,
    }

    private static IServiceProvider? _serviceProvider = null;

    private static IFusionCache GetCache(CacheType cacheType)
    {
        _serviceProvider ??= BuildServiceProvider();

        var fusionCacheProvider = _serviceProvider.GetRequiredService<IFusionCacheProvider>();
        var cache = fusionCacheProvider.GetCache(cacheType.ToString("G"));
        
        if(cache == null)
            throw new MissingMemberException($"No cache found for cache type {cacheType}");

        return cache;

    }

    private static ServiceProvider BuildServiceProvider()
    {
        IServiceCollection services = new ServiceCollection();
        services.AddLogging(builder => builder.AddSerilog(logger: Log.Logger, dispose: false));

        //postgre L2
        services.AddDistributedPostgreSqlCache(options =>
        {
            options.ConnectionString = Devmasters.Config.GetWebConfigValue("PostgreSqlCacheConnectionString");
            options.SchemaName = "public";
            options.TableName = "fusion_cache";
            options.CreateInfrastructure = true;
            options.ExpiredItemsDeletionInterval = TimeSpan.FromMinutes(30);
            options.DisableRemoveExpired = false;
            options.UpdateOnGetCacheItem = false;
            options.ReadOnlyMode = false;
        });
        //memcached L2
        var nodeIPs = Devmasters.Config.GetWebConfigValue("HazelcastServers").Split(',',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        services.AddEnyimMemcached(options =>
        {
            foreach (string nodeIp in nodeIPs)
            {
                if (nodeIp.Contains(":"))
                {
                    string[] strArray = nodeIp.Split(':');
                    options.AddServer(strArray[0], Convert.ToInt32(strArray[1]));
                }
                else
                    options.AddServer(nodeIp, 5701);
            }

            options.Protocol = MemcachedProtocol.Text;
            options.Transcoder = "MessagePackTranscoder";
        }, asDistributedCache: true);


        services.AddFusionCache(CacheType.L1Default.ToString("G"))
            .AsKeyedServiceByCacheName()
            .WithDefaultEntryOptions(L1DefaultEntryOptions)
            .WithoutDistributedCache()
            .WithoutBackplane();

        services.AddFusionCache(CacheType.L1FromPermanentStore.ToString("G"))
            .AsKeyedServiceByCacheName()
            .WithDefaultEntryOptions(L1FromPermanentStoreEntryOptions)
            .WithoutDistributedCache()
            .WithoutBackplane();

        //how do i add .With ... Distributed... ?
        services.AddFusionCache(CacheType.L2Memcache.ToString("G"))
            .AsKeyedServiceByCacheName()
            .WithDefaultEntryOptions(DistributedCacheEntryOptions)
            .WithSerializer(new FusionCacheSystemTextJsonSerializer())
            .WithDistributedCache(sp => sp.GetRequiredService<IEnumerable<IDistributedCache>>()
                .OfType<MemcachedClient>()
                .First())
            .WithoutBackplane();

        //how do i add .With ... Distributed... ?
        services.AddFusionCache(CacheType.L2PostgreSql.ToString("G"))
            .AsKeyedServiceByCacheName()
            .WithDefaultEntryOptions(DistributedCacheEntryOptions)
            .WithSerializer(new FusionCacheSystemTextJsonSerializer())
            .WithDistributedCache(sp => sp.GetRequiredService<IEnumerable<IDistributedCache>>()
                .OfType<PostgreSqlCache>()
                .First())
            .WithoutBackplane();

        return services.BuildServiceProvider();
    }
}