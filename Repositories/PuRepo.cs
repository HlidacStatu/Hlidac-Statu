using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HlidacStatu.Entities;
using HlidacStatu.Entities.Entities;
using Microsoft.EntityFrameworkCore;

namespace HlidacStatu.Repositories;

public class PuRepo
{
    public static async Task<PuOrganizace> GetDetailEagerAsync(int id)
    {
        await using var db = new DbEntities();

        return await db.PuOrganizace
            .AsNoTracking()
            .Where(pu => pu.Id == id)
            .Include(o => o.Tags) // Include PuOrganizaceTags
            .Include(o => o.Platy) // Include PuPlat
            .Include(o => o.Metadata) // Include PuOranizaceMetadata
            .FirstOrDefaultAsync();
    }
    
    
    
   
}