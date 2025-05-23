using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using HlidacStatu.Entities;
using Microsoft.EntityFrameworkCore;

namespace HlidacStatu.Repositories.Auditing;

public static class ChangeTracker
{
    public static List<AuditLog> CreateAuditLogs(DbContext dbContext, string userName)
    {
        var auditEntries = dbContext.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Select(entry =>
            {
                var pkName = entry.Metadata.FindPrimaryKey()?.Properties.FirstOrDefault()?.Name;
                var pkValue = pkName != null ? entry.Property(pkName).CurrentValue : null;
                
                var audit = new AuditLog
                {
                    EntityId = pkValue,
                    EntityName = entry.Entity.GetType().Name,
                    Action = entry.State,
                    Username = userName,
                    Timestamp = DateTime.UtcNow,
                    Changes = entry.Properties
                        .Where(p => entry.State == EntityState.Added || entry.State == EntityState.Deleted || p.IsModified)
                        .Select(p => new AuditLog.AuditChange() {
                            Property = p.Metadata.Name,
                            OldValue = entry.State == EntityState.Added ? null : p.OriginalValue,
                            NewValue = entry.State == EntityState.Deleted ? null : p.CurrentValue
                        })
                        .Where(ch => ch.IsProperChange)
                        .ToList()
                };
                return audit;
            }).ToList();

        return auditEntries;
    }
    
    
    public static async Task SaveAuditsLogAsync(List<AuditLog> audits)
    {
        if (audits == null || audits.Count == 0)
            return;
        
        foreach (var audit in audits.Where(a => a.EntityId == null && a.EntryReference != null))
        {
            var pkName = audit.EntryReference.Metadata.FindPrimaryKey()?.Properties.FirstOrDefault()?.Name;
            if (pkName != null)
            {
                audit.EntityId = audit.EntryReference.Property(pkName).CurrentValue;
            }

            audit.EntryReference = null; // clear to avoid serialization issues
        }

        await AuditLogRepo.BulkSaveAsync(audits);
    }
}