using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace HlidacStatu.CachingClients.PostgreSql
{
    public sealed class PostgreSqlFacory
    {
        public static IDistributedCache Create(PostgreSqlCacheOptions options, ILoggerFactory? loggerFactory)
        {
            IOptions<PostgreSqlCacheOptions> opt = Options.Create(options);
            
            ILogger<DatabaseOperations>? dbopsLogger = loggerFactory?.CreateLogger<DatabaseOperations>();
            ILogger<DatabaseExpiredItemsRemoverLoop>? dbremoverLoopLogger = loggerFactory?.CreateLogger<DatabaseExpiredItemsRemoverLoop>();
            
            DatabaseOperations dbops = new DatabaseOperations(opt, dbopsLogger);
            DatabaseExpiredItemsRemoverLoop dbloop = new DatabaseExpiredItemsRemoverLoop(opt, dbops, dbremoverLoopLogger);
            
            return new PostgreSqlCache(opt, dbops, dbloop);
        }
    }
}
