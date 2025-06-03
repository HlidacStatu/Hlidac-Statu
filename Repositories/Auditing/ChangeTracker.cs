using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
                    EntityId = StringifyObjectValue(pkValue),
                    EntityName = entry.Entity.GetType().Name,
                    EntryReference = entry,
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
        
        foreach (var audit in audits)
        {
            if(audit.EntryReference is null)
                continue;
            
            //find and set new id (for new records)
            if (audit.Action == EntityState.Added)
            {
                //find pk name
                var pkName = audit.EntryReference.Metadata.FindPrimaryKey()?.Properties.FirstOrDefault()?.Name;
                if (pkName != null)
                {
                    //finds pk value
                    var idObj = audit.EntryReference.Property(pkName).CurrentValue;
                    audit.EntityId = StringifyObjectValue(idObj);
                }

                //update inner Audit object (Changes) to reflect/save correct PK
                foreach (var change in audit.Changes)
                {
                    if (change.Property == pkName)
                    {
                        change.NewValue = audit.EntityId;
                    }
                }
            }

            audit.EntryReference = null;
        }

        await AuditLogRepo.BulkSaveAsync(audits);
    }

    public static string StringifyObjectValue(object value)
    {
        string result = value switch
        {
            null               => null,
            string s           => s,
            // this covers string, all numeric IFormattable types (int, long, decimal, etc.),
            // and falls back to ToString() for anything else
            IFormattable form  => form.ToString(null, CultureInfo.InvariantCulture),
            _                   => value.ToString()
        };

        return result;
    }
}