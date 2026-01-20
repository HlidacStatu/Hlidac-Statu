using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HlidacStatu.Entities;
using HlidacStatu.Entities.Entities;
using Microsoft.EntityFrameworkCore;

namespace HlidacStatu.Repositories
{
    public static partial class FirmaRepo
    {
        public static async Task<Dictionary<int, Obor>> LoadAllOboryAsync()
        {
            await using DbEntities db = new DbEntities();
            return await db.Obory.AsNoTracking().ToDictionaryAsync(x => x.Id);
        }

        public static async Task<List<FirmaObor>> LoadOboryFirmyAsync(string ico)
        {
            var normalizedIco = HlidacStatu.Util.ParseTools.NormalizeIco(ico);

            await using DbEntities db = new DbEntities();
            return await db.FirmaObory.AsNoTracking()
                .Where(fo => fo.ICO == normalizedIco)
                .ToListAsync();
        }
        
        public static async Task<Obor> LoadOborDetailAsync(string obor)
        {
            var oborNormalized = obor.ToLower();

            await using DbEntities db = new DbEntities();
            return await db.Obory.AsNoTracking()
                .Where(fo => fo.OborName == oborNormalized)
                .FirstOrDefaultAsync();
        }
        
        public static async Task<List<Firma>> LoadFirmyForOborAsync(string obor)
        {
            var oborNormalized = obor.ToLower();

            await using DbEntities db = new DbEntities();
            
            var result = 
                from f in db.Firma
                join fo in db.FirmaObory on f.ICO equals fo.ICO
                join o in db.Obory on fo.OborId equals o.Id
                where o.OborName.ToLower() == oborNormalized
                select f;
            
            return await result.ToListAsync();
        }

        public static async Task AddOborToFirmaAsync(FirmaObor firmaObor)
        {
            await using DbEntities db = new DbEntities();
            var existingObor =
                await db.FirmaObory.FirstOrDefaultAsync(x => x.ICO == firmaObor.ICO && x.OborId == firmaObor.OborId);
            if (existingObor is not null)
                return;

            db.FirmaObory.Add(firmaObor);
            await db.SaveChangesAsync();
        }

        public static async Task RemoveOborFromFirmaAsync(FirmaObor firmaObor)
        {
            await using DbEntities db = new DbEntities();
            db.FirmaObory.Remove(firmaObor);
            await db.SaveChangesAsync();
        }
        
        public static async Task RemoveOborFromFirmaAsync(string ico, int oborId)
        {
            await using DbEntities db = new DbEntities();
            await db.FirmaObory.Where(f => f.ICO == ico && f.OborId == oborId)
                .ExecuteDeleteAsync();
        }
    }
}