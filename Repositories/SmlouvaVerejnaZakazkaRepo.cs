using System;
using System.Threading.Tasks;
using HlidacStatu.Entities;
using HlidacStatu.Entities.Entities;
using Microsoft.EntityFrameworkCore;

namespace HlidacStatu.Repositories;

public static class SmlouvaVerejnaZakazkaRepo
{
    public static async Task Upsert(SmlouvaVerejnaZakazka smlouvaVerejnaZakazka)
    {
        await using DbEntities db = new DbEntities();

        var existingEntity = await db.SmlouvaVerejnaZakazka.FirstOrDefaultAsync(o =>
            o.IdSmlouvy == smlouvaVerejnaZakazka.IdSmlouvy 
            && o.VzId == smlouvaVerejnaZakazka.VzId);

        if (existingEntity == null)
        {
            db.SmlouvaVerejnaZakazka.Add(smlouvaVerejnaZakazka);
        }
        else
        {
            existingEntity.CosineSimilarity = smlouvaVerejnaZakazka.CosineSimilarity;
            existingEntity.ModifiedDate = DateTime.Now;
        }
        
        await db.SaveChangesAsync();
    }
}