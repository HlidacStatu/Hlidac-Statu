using HlidacStatu.Entities;
using HlidacStatu.Repositories.Auditing;
using PoliticiEditor.Components.Toasts;

namespace PoliticiEditor.Components.Pages.Politik;

public static class Helpers
{
    public static async Task Save(DbEntities? db, string? getNameId, ToastService toaster, Serilog.ILogger logger)
    {
        try
        {
            var auditLogs = ChangeTracker.CreateAuditLogs(db, getNameId);
            await db.SaveChangesAsync();
            await ChangeTracker.SaveAuditLogAsync(auditLogs);
            toaster.AddInfoMessage("Uloženo", "Změny byly úspěšně uloženy.");
        }
        catch (Exception e)
        {
            toaster.AddErrorMessage("Chyba", "Během ukládání došlo k chybě.");
            logger.Error(e, "Couldnt save data.");
        }
    }
}