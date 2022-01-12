using System;
using HlidacStatu.Entities;
using Devmasters;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HlidacStatu.Entities.Views;
using Microsoft.EntityFrameworkCore;


namespace HlidacStatu.Repositories
{
    public partial class CenyRepo
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