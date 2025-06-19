using System.Text;
using System.Threading.Tasks;
using HlidacStatu.Entities;
using Microsoft.AspNetCore.Components;

namespace HlidacStatu.Repositories.Auditing.Strategies;

public class DefultStrategy : IAuditStrategy
{
    public async Task<MarkupString> DisplayChanges(AuditLog auditLog)
    {
        var sb = new StringBuilder();
        sb.Append("<ul class='list-group'>");

        foreach (var change in auditLog.Changes)
        {
            if (!change.IsProperChange)
                continue;

            sb.AppendLine(await DisplayProperty(change));
        }
        sb.Append("</ul>");
        return (MarkupString)sb.ToString();
    }

    public void RevertChange(AuditLog log)
    {
        throw new System.NotImplementedException();
    }

    private Task<string> DisplayProperty(AuditLog.AuditChange change)
    {
        var sb = new StringBuilder();
        sb.Append("<li class='list-group-item'>");
        
        sb.Append($"<span class='badge bg-primary'>{change.Property}</span> : ")
            .Append($"<span class='badge bg-secondary'>{change.OldValue}</span> => ")
            .Append($"<span class='badge bg-success'>{change.NewValue}</span>");
        
        sb.Append("</li>");
        return Task.FromResult(sb.ToString());
    }
}