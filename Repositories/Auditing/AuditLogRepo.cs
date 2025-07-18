using System;
using System.Collections.Generic;
using System.Linq;
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
        
        public static async Task<List<AuditLog>> LoadUntouchedAuditLogsAsync(string username = null)
        {
            // Step 1: Get all usernames sorted alphabetically
            var userAgg = await AuditLogClient.SearchAsync<AuditLog>(s => s
                .Size(0)
                .Query(q => q
                    .Bool(b => b
                        .Should(
                            sh => sh.Term(t => t.Field(f => f.State).Value((int)AuditLog.AuditState.New)),
                            sh => sh.Bool(bb => bb.MustNot(mn => mn.Exists(e => e.Field(f => f.State))))
                        )
                        .MinimumShouldMatch(1)
                    )
                )
                .Aggregations(a => a
                    .Terms("usernames", t => t
                            .Field(f => f.Username.Suffix("keyword"))
                            .Order(o => o.Ascending("_key"))
                            .Size(10000)
                    )
                )
            );

            var buckets = userAgg.Aggregations.Terms("usernames")?.Buckets.ToList();
            if (buckets == null || buckets.Count == 0)
                return new List<AuditLog>();

            string resolvedUsername;

            if (string.IsNullOrEmpty(username))
            {
                resolvedUsername = buckets.First().Key;
            }
            else
            {
                var followingName = buckets
                    .FirstOrDefault(b => string.Compare(b.Key, username, StringComparison.Ordinal) > 0);

                resolvedUsername = followingName?.Key ?? buckets.First().Key;
            }

            // Step 2: Load all audit logs for the resolved username
            var result = await AuditLogClient.SearchAsync<AuditLog>(s => s
                .Size(10000)
                .Query(q => q
                    .Term(t => t.Field(f => f.Username.Suffix("keyword")).Value(resolvedUsername))
                )
            );

        
            if (!result.IsValid)
            {
                var a = result.DebugInformation;
                _logger.Error($"Error when Loading auditlog from ES: {a}");
            }
        
            return result.Documents.ToList();
        }
        
        public static async Task<List<AuditLog>> LoadAuditLogsAsync(string username, string entityName, string entityId)
        {
            var result = await AuditLogClient.SearchAsync<AuditLog>(s => s
                .Size(10000)
                .Query(q => q
                    .Bool(b => b
                        .Must(m => m.Term(t => t.Field(f => f.Username.Suffix("keyword")).Value(username)),
                            m => m.Term(t => t.Field(f => f.EntityName.Suffix("keyword")).Value(entityName)),
                            m => m.Term(t => t.Field(f => f.EntityId.Suffix("keyword")).Value(entityId))
                        )
                    )
                )
            );

        
            if (!result.IsValid)
            {
                var a = result.DebugInformation;
                _logger.Error($"Error when Loading auditlog from ES: {a}");
            }
        
            return result.Documents.ToList();
        }

        public static async Task RemoveAsync(AuditLog auditItem)
        {
            if (auditItem == null || string.IsNullOrEmpty(auditItem.Id))
                return;

            await AuditLogClient.DeleteAsync<AuditLog>(auditItem.Id);
        }

        public static async Task SaveAsync(AuditLog auditItem)
        {
            var result = await AuditLogClient.IndexAsync<AuditLog>(auditItem, o => o.Id(auditItem.Id));

            if (!result.IsValid)
            {
                var a = result.DebugInformation;
                _logger.Error($"Error when saving auditlog to ES: {a}");
            }
        }
    }
}