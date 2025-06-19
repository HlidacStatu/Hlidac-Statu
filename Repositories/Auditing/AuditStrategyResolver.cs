using HlidacStatu.Entities;
using HlidacStatu.Repositories.Auditing.Strategies;

namespace HlidacStatu.Repositories.Auditing;

public static class AuditStrategyResolver
{
    public static IAuditStrategy Get(AuditLog auditLog) =>
         auditLog.EntityName switch
    {
        nameof(PpPrijem) => new PpPrijemStrategy(),
        _ => new DefultStrategy()
        
    };

        
}