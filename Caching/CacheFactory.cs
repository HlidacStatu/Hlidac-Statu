using Enyim.Caching;
using Enyim.Caching.Memcached;
using HlidacStatu.Caching.PostgreSql;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
    /// <param name="cacheName">jméno cache</param>
    /// <param name="prefixName">Prefix pro keyname !důležitý</param>
    public static IFusionCache CreateDefaultL1OnlyCache(string cacheName, string prefixName,
        ILoggerFactory? loggerFactory) => new FusionCache(new FusionCacheOptions()
    {
        CacheName = cacheName,
        CacheKeyPrefix = prefixName,

        DefaultEntryOptions = new FusionCacheEntryOptions
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
        }
    }, logger: loggerFactory?.CreateLogger<FusionCache>());


    /// <summary>
    /// SCÉNÁŘ 2: řešíme kdy se data mění pomaleji (hodiny)
    /// duration 10 minut
    /// failsafe 6 hodin
    /// distributed failsafe duration - v L2 max 1 den
    /// Je potřeba nastavit cacheName i prefixName
    /// </summary>
    /// <param name="cacheName">jméno cache</param>
    /// <param name="prefixName">Prefix pro keyname !důležitý</param>
    public static IFusionCache CreateDefaultCacheWithL2Support(string cacheName, string prefixName,
        ILoggerFactory? loggerFactory) => new FusionCache(new FusionCacheOptions()
    {
        // DISTRIBUTED CACHE CIRCUIT-BREAKER
        DistributedCacheCircuitBreakerDuration = TimeSpan.FromSeconds(10),
        CacheName = cacheName,
        CacheKeyPrefix = prefixName,

        DefaultEntryOptions = new FusionCacheEntryOptions
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
        }
    }, logger: loggerFactory?.CreateLogger<FusionCache>());

    /// <summary>
    /// SCÉNÁŘ 3: NEMÁ EXPIRACI! řešíme, kdy jsou data přepočítaná ručně odjinud (nevíme interval)
    /// duration 10 minut
    /// failsafe 6 hodin
    /// nepotřeba distributed cache
    /// FACTORY metoda je přímo volání do předpočítaných dat, předpočítaná data se updatuje z DOWNLOADERU
    /// Je potřeba nastavit cacheName i prefixName
    /// </summary>
    /// <param name="cacheName">jméno cache</param>
    /// <param name="prefixName">Prefix pro keyname !důležitý</param>
    public static IFusionCache CreateDefaultL1OnlyForPermanentDataCache(string cacheName, string prefixName,
        ILoggerFactory? loggerFactory) => new FusionCache(new FusionCacheOptions()
    {
        CacheName = cacheName,
        CacheKeyPrefix = prefixName,

        DefaultEntryOptions = new FusionCacheEntryOptions
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
        }
    }, logger: loggerFactory?.CreateLogger<FusionCache>());


    public static IFusionCache WithPostgreSqlL2(this IFusionCache fusionCache)
    {
        var postgreSqlClient = GetPostgreSqlClient();
        if(postgreSqlClient == null)
            throw new InvalidOperationException("Couldnt instantiate a PostgresClient");
        
        return fusionCache.SetupDistributedCache(postgreSqlClient, new FusionCacheSystemTextJsonSerializer());
    }

    public static IFusionCache WithMemcacheL2(this IFusionCache fusionCache)
    {
        var memcachedClient = GetMemcachedClient();
        if(memcachedClient == null)
            throw new InvalidOperationException("Couldnt instantiate a MemcachedClient");

        return fusionCache.SetupDistributedCache(memcachedClient, new FusionCacheSystemTextJsonSerializer());
    }
    
    private static IDistributedCache? _memcachedClient;
    private static IDistributedCache? _postgreSqlClient;

    private static IDistributedCache? GetMemcachedClient()
    {
        if (_memcachedClient == null)
        {
            var nodeIPs = Devmasters.Config.GetWebConfigValue("HazelcastServers").Split(',',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            IServiceCollection services = new ServiceCollection();

            //use global static logger and send it further down 
            services.AddLogging(builder => builder.AddSerilog(logger: Log.Logger, dispose: false));

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
            });
            _memcachedClient = services.BuildServiceProvider().GetService<IMemcachedClient>() as IDistributedCache;
        }

        return _memcachedClient;
    }
    
    private static IDistributedCache? GetPostgreSqlClient()
    {
        if (_postgreSqlClient == null)
        {
            IServiceCollection services = new ServiceCollection();

            //use global static logger and send it further down 
            services.AddLogging(builder => builder.AddSerilog(logger: Log.Logger, dispose: false));

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
            _postgreSqlClient = services.BuildServiceProvider().GetService<IMemcachedClient>() as IDistributedCache;
        }

        return _postgreSqlClient;
    }
}