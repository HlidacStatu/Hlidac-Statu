using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Nest;

namespace HlidacStatu.Entities;

[ElasticsearchType(IdProperty = nameof(Id))]
public class AuditLog
{
    [Keyword]
    public int Id { get; set; }
    public string EntityId { get; set; }
    public string EntityName { get; set; }
    public EntityState Action { get; set; }
    [Text]
    public string Username { get; set; }
    [Date]
    public DateTime Timestamp { get; set; }
    public List<AuditChange> Changes { get; set; }
    
    //this reference is for setting proper Id
    [Ignore]
    public Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry EntryReference { get; set; }

    public class AuditChange
    {
        [Keyword]
        public string Property { get; set; } = null;
        public object OldValue { get; set; }
        public object NewValue { get; set; }
        
        public bool IsProperChange => OldValue is not null || NewValue is not null;
    }
}

