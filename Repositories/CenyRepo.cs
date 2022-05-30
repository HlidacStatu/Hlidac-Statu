using HlidacStatu.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;


namespace HlidacStatu.Repositories
{
    public class CenyRepo
    {

        public static async Task<List<Cena>> GetAllCenyAsync()
        {
            using (var db = new DbEntities())
            {
                return await db.Ceny.AsQueryable()
                    .ToListAsync();
            }
        }

        
        
    }
}