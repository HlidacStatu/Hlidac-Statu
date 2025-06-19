using System;
using System.Text;
using System.Threading.Tasks;
using HlidacStatu.Entities;
using Microsoft.AspNetCore.Components;

namespace HlidacStatu.Repositories.Auditing.Strategies;

public class PpPrijemStrategy : IAuditStrategy
{
    public async Task<MarkupString> DisplayChanges(AuditLog auditLog)
    {
        if (!int.TryParse(auditLog.EntityId, out var id))
            return (MarkupString)$"<span class='text-danger'>Změnu {auditLog.Id} nelze zobrazit, nejde parsovat id</span>";

        var ppPrijem = await PpRepo.GetPrijemAsync(id);
        if (ppPrijem == null)
            return (MarkupString)$"<span class='text-danger'>Změnu {auditLog.Id} nelze zobrazit, Prijem {id} nebyl nalezen</span>";
        
        var sb = new StringBuilder();
        sb.Append("<ul class='list-group'>");

        foreach (var change in auditLog.Changes)
        {
            if (!change.IsProperChange)
                continue;

            sb.AppendLine(await DisplayProperty(change, ppPrijem));
        }

        sb.Append("</ul>");
        return (MarkupString)sb.ToString();
    }

    private async Task<string> DisplayProperty(AuditLog.AuditChange change, PpPrijem currentPpPrijem)
    {
        var sb = new StringBuilder();
        sb.Append("<li class='list-group-item'>");
        
        switch (change.Property)
        {
            case nameof(PpPrijem.IdOrganizace):
            {
                sb.Append(
                    $"<span class='badge bg-primary'>Organizace</span> (<span class='badge bg-light text-dark'>{currentPpPrijem.Organizace.Nazev}</span>) : ");
                sb.Append($"<span class='badge bg-secondary'>");
                if (change.OldValue is not null && int.TryParse(change.OldValue.ToString(), out var oldId))
                {
                    var nazev = await PpRepo.GetOrganizaceNameAsync(oldId);
                    sb.Append(nazev);
                }
                else
                {
                    sb.Append(" ");
                }
                sb.Append("</span> => <span class='badge bg-success'>");

                if (change.NewValue is not null && int.TryParse(change.NewValue.ToString(), out var newId))
                {
                    var nazev = await PpRepo.GetOrganizaceNameAsync(newId);
                    sb.Append(nazev);
                }
                else
                {
                    sb.Append(" ");
                }
                
                
                sb.Append("</span>");
            }
                break;

            case nameof(PpPrijem.Status):
            {
                sb.Append(
                    $"<span class='badge bg-primary'>{change.Property}</span> (<span class='badge bg-light text-dark'>{currentPpPrijem.Status.ToString("G")}</span>) : ");
                sb.Append("<span class='badge bg-secondary'>");
                if (change.OldValue is not null && Enum.TryParse<PpPrijem.StatusPlatu>(change.OldValue.ToString(), true, out var oldStatus))
                {
                    sb.Append(oldStatus.ToString("G"));
                }
                else
                {
                    sb.Append(" ");
                }
                sb.Append("</span> => <span class='badge bg-success'>");

                if (change.NewValue is not null && Enum.TryParse<PpPrijem.StatusPlatu>(change.NewValue.ToString(), true, out var newStatus))
                {
                    sb.Append(newStatus.ToString("G"));
                }
                else
                {
                    sb.Append(" ");
                }
                sb.Append("</span>");
            }
                break;
            
            default:
                sb.Append($"<span class='badge bg-primary'>{change.Property}</span> (<span class='badge bg-light text-dark'>{GetValueForProperty(currentPpPrijem, change.Property)}</span>) : ")
                    .Append($"<span class='badge bg-secondary'>{change.OldValue ?? " "}</span> => ")
                    .Append($"<span class='badge bg-success'>{change.NewValue ?? " "}</span>");
                break;
        }

        sb.Append("</li>");
        return sb.ToString();
    }

    private string GetValueForProperty(PpPrijem prijem, string property)
    {
        try
        {
            return prijem?.GetType().GetProperty(property)?.GetValue(prijem)?.ToString();
        }
        catch
        {
            return null;
        }
    }

    public void RevertChange(AuditLog log)
    {
        throw new NotImplementedException();
    }
}