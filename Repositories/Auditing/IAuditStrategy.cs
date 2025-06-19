using System.Threading.Tasks;
using HlidacStatu.Entities;
using Microsoft.AspNetCore.Components;

namespace HlidacStatu.Repositories.Auditing;

public interface IAuditStrategy
{
    Task<MarkupString> DisplayChanges(AuditLog auditLog);
    void RevertChange(AuditLog auditLog);
}