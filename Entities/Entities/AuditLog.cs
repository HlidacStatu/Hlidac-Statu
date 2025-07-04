using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Nest;

namespace HlidacStatu.Entities;

[ElasticsearchType(IdProperty = nameof(Id))]
public class AuditLog
{
    [Keyword]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string EntityId { get; set; }
    public string EntityName { get; set; }
    public EntityState Action { get; set; }
    public string Username { get; set; }
    [Date]
    public DateTime Timestamp { get; set; }
    public List<AuditChange> Changes { get; set; }
    
    [Keyword]
    public AuditState State { get; set; } = AuditState.New;
    
    //this reference is for setting proper Id
    [Ignore]
    public Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry EntryReference { get; set; }

    public class AuditChange
    {
        [Keyword]
        public string Property { get; set; } = null;
        [Keyword]
        public object OldValue { get; set; }
        [Keyword]
        public object NewValue { get; set; }
        
        public bool IsProperChange => OldValue is not null || NewValue is not null;
    }

    public enum AuditState
    {
        New,
        Approved,
        Reverted,
        Deleted
    }
}

