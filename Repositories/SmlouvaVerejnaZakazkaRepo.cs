using System;
using System.Collections.Generic;
using System.Linq;
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

    public static async Task<List<SmlouvaVerejnaZakazka>> GetSmlouvyForVz(string idVz)
    {
        await using DbEntities db = new DbEntities();
        
        var smlouvy = await db.SmlouvaVerejnaZakazka.Where( s => s.VzId == idVz && s.CosineSimilarity > 0.5d )
            .ToListAsync();

        return smlouvy;
    }
    
    public static async Task<SmlouvaVerejnaZakazka> GetVzForSmlouva(string idsmlouvy)
    {
        await using DbEntities db = new DbEntities();
        
        var vz = await db.SmlouvaVerejnaZakazka
            .Where( s => s.IdSmlouvy == idsmlouvy && s.CosineSimilarity > 0.5d )
            .OrderByDescending(x => x.CosineSimilarity)
            .FirstOrDefaultAsync();

        return vz;
    }
}