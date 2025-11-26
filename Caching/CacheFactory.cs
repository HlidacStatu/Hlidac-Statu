using Enyim.Caching;
using Enyim.Caching.Memcached;
using HlidacStatu.CachingClients.PostgreSql;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;

namespace Hlidacstatu.Caching;


public static class CacheFactory
{
    /// <summary>
    /// SCÉNÁŘ 1: řešíme když data expirují rychle - statistiky webů se mění co 5 minut
    /// duration 5 minut
    /// failsafe 6 hodin
    /// nepotřeba distributed cache
    /// Je potřeba nastavit cacheName i prefixName
    /// </summary>
    private static FusionCacheEntryOptions L1DefaultEntryOptions => new FusionCacheEntryOptions
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
        SkipBackplaneNotifications = true,
        
        JitterMaxDuration = TimeSpan.FromMinutes(1)
    };


    /// <summary>
    /// SCÉNÁŘ 2: řešíme kdy se data mění pomaleji (hodiny)
    /// duration 10 minut
    /// failsafe 6 hodin
    /// distributed failsafe duration - v L2 max 1 den
    /// Je potřeba nastavit cacheName i prefixName
    /// </summary>
    private static FusionCacheEntryOptions DistributedCacheEntryOptions => new FusionCacheEntryOptions
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
        SkipBackplaneNotifications = true,
        JitterMaxDuration = TimeSpan.FromMinutes(2)
    };

    /// <summary>
    /// SCÉNÁŘ 3: NEMÁ EXPIRACI! řešíme, kdy jsou data přepočítaná ručně odjinud (nevíme interval)
    /// duration 10 minut
    /// failsafe 6 hodin
    /// nepotřeba distributed cache
    /// FACTORY metoda je přímo volání do předpočítaných dat, předpočítaná data se updatuje z DOWNLOADERU
    /// Je potřeba nastavit cacheName i prefixName
    /// </summary>
    private static FusionCacheEntryOptions L1FromPermanentStoreEntryOptions => new FusionCacheEntryOptions
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
        SkipBackplaneNotifications = true,
        JitterMaxDuration = TimeSpan.FromMinutes(1)
    };


    public enum CacheType
    {
        L1Default,
        L1FromPermanentStore,
        L2PostgreSql,
        L2Memcache,
    }

    private static IServiceProvider? _serviceProvider = null;

    public static IFusionCache CreateNew(CacheType cacheType, string cachePrefix)
    {
        //build service provider
        _serviceProvider ??= BuildServiceProvider();
        
        bool isDistributedCache = cacheType == CacheType.L2Memcache || cacheType == CacheType.L2PostgreSql;

        var logger = _serviceProvider?.GetService<Microsoft.Extensions.Logging.ILogger<FusionCache>?>();
        
        var cache = new FusionCache(new FusionCacheOptions()
        {
            CacheName = cachePrefix,
            CacheKeyPrefix = cachePrefix,
            DistributedCacheCircuitBreakerDuration = isDistributedCache? TimeSpan.FromSeconds(10) : TimeSpan.Zero,
            DefaultEntryOptions = cacheType switch
            {
                CacheType.L1Default => L1DefaultEntryOptions,
                CacheType.L1FromPermanentStore => L1FromPermanentStoreEntryOptions,
                CacheType.L2PostgreSql => DistributedCacheEntryOptions,
                CacheType.L2Memcache => DistributedCacheEntryOptions,
                _ => throw new ArgumentOutOfRangeException(nameof(cacheType), cacheType, null)
            }
        }, logger: logger);

        if (isDistributedCache)
        {
            IDistributedCache? distributedCache = ResolveL2CacheProvider(cacheType); 
            
            if(distributedCache != null)
                cache.SetupDistributedCache(distributedCache, new FusionCacheSystemTextJsonSerializer());
        }

        return cache;

    }

    private static IDistributedCache? ResolveL2CacheProvider(CacheType cacheType)
    {
        _serviceProvider ??= BuildServiceProvider();
        
        return cacheType switch
        {
            CacheType.L2PostgreSql => _serviceProvider.GetRequiredService<IEnumerable<IDistributedCache>>()
                .OfType<PostgreSqlCache>()
                .First(),
            CacheType.L2Memcache => _serviceProvider.GetRequiredService<IEnumerable<IDistributedCache>>()
                .OfType<MemcachedClient>()
                .First(),
            _ => null
            
        };
    }

    /// <summary>
    /// Tady builduju services pro přidání logování do Postgres, FusionCache i Enyim cache
    /// Taky se vytváří singletony pro L2 distributed cache.
    /// Memcached se musí přidat ještě solo jako IDistributed (pro fusioncache)
    /// </summary>
    /// <returns></returns>
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

        // Hack for memcache, otherwise it is not added as a IDistributed cache
        services.AddSingleton<IDistributedCache>(sp =>
        {
            var service = sp.GetRequiredService<IMemcachedClient>() as MemcachedClient;
            return (IDistributedCache)service;
        });
            
        
        
        return services.BuildServiceProvider();
    }
}