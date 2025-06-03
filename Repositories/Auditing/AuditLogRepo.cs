using System.Collections.Generic;
using System.Threading.Tasks;
using HlidacStatu.Connectors;
using HlidacStatu.Entities;
using Nest;
using Serilog;

namespace HlidacStatu.Repositories.Auditing
{
    public static class AuditLogRepo
    {

        private static readonly ILogger _logger = Log.ForContext(typeof(AuditLogRepo));

        public static readonly ElasticClient AuditLogClient = Manager.GetESClient_AuditLog();
        
        public static async Task<bool> BulkSaveAsync(List<AuditLog> auditLogs)
        {
            var result = await AuditLogClient.IndexManyAsync(auditLogs);

            if (!result.IsValid || result.Errors)
            {
                var a = result.DebugInformation;
                _logger.Error($"Error when bulkSaving auditlogs to ES: {a}");
            }

            return result.Errors;
        }
        
        // public static async Task<bool> SaveAsync(AuditLog auditLog)
        // {
        //     var result = await AuditLogClient.IndexAsync<AuditLog>(auditLog);
        //
        //     if (!result.IsValid)
        //     {
        //         var a = result.DebugInformation;
        //         _logger.Error($"Error when saving auditlog to ES: {a}");
        //     }
        //
        //     return result.IsValid;
        // }
        
    }
}