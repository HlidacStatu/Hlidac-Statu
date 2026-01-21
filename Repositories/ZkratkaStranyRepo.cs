using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HlidacStatu.Entities;
using HlidacStatu.Repositories.Cache;
using HlidacStatu.Util;
using Microsoft.EntityFrameworkCore;

namespace HlidacStatu.Repositories
{
    public static class ZkratkaStranyRepo
    {

        /// <returns> Dictionary; Key=Ico, Value=Zkratka </returns>
        public static async Task<Dictionary<string, string>> ZkratkyVsechStranAsync()
        {
            await using DbEntities db = new DbEntities();
            return await db.ZkratkaStrany.AsNoTracking().ToDictionaryAsync(z => z.Ico, e => e.KratkyNazev);
        }

        public static async Task<string> IcoStranyAsync(string zkratka)
        {
            if (DataValidators.CheckCZICO(Devmasters.TextUtil.NormalizeToNumbersOnly(zkratka)))
            {
                var f = await FirmaCache.GetAsync(Devmasters.TextUtil.NormalizeToNumbersOnly(zkratka));
                if (f?.Valid == true && f.Kod_PF == 711)
                    return Devmasters.TextUtil.NormalizeToNumbersOnly(zkratka);
                else
                    return zkratka; //TODO co delat, kdyz ICO neni politicka strana
            }

            await using DbEntities db = new DbEntities();
            return await db.ZkratkaStrany
                .AsNoTracking()
                .Where(zs => zs.KratkyNazev.ToLower() == zkratka.Trim().ToLower())
                .Select(zs => zs.Ico)
                .FirstOrDefaultAsync();
        }

        public static async Task<string> NazevStranyForIcoAsync(string ico)
        {
            var normalizedIco = ParseTools.NormalizeIco(ico);
            
            await using DbEntities db = new DbEntities();
            
            var zkratkaStrany = await db.ZkratkaStrany
                .AsNoTracking()
                .Where(zs => zs.Ico == normalizedIco)
                .Select(zs => zs.KratkyNazev)
                .FirstOrDefaultAsync();
            
            if(!string.IsNullOrWhiteSpace(zkratkaStrany))
                return zkratkaStrany;
            
            return await FirmaRepo.NameFromIcoAsync(normalizedIco);
        }
    }
}