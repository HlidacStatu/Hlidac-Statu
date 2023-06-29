using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using HlidacStatu.Entities;
using HlidacStatu.Entities.Entities;

namespace HlidacStatu.Repositories;

public class PlatyUrednikuRepo
{
    public static async Task<List<PlatUrednika>> GetCenyForIco(string ico)
    {
        await using var db = new DbEntities();

        return await db.PlatyUredniku.AsNoTracking()
            .Where(platUrednika => platUrednika.Ico == ico)
            .ToListAsync();
    }
}