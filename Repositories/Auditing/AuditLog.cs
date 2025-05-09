using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace HlidacStatu.Repositories.Auditing;

public class AuditLog
{
    public int Id { get; set; }
    public object EntityId { get; set; }
    public string EntityName { get; set; }
    public EntityState Action { get; set; }
    public string Username { get; set; }
    public DateTime Timestamp { get; set; }
    public List<AuditChange> Changes { get; set; }
    
    //this reference is for setting proper Id
    [Nest.Ignore]
    public Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry EntryReference { get; set; }

    public class AuditChange
    {
        public string Property { get; set; } = null;
        public object OldValue { get; set; }
        public object NewValue { get; set; }
        
        public bool IsProperChange => OldValue is null && NewValue is null;
    }
}

