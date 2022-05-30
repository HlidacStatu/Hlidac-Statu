using HlidacStatu.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace HlidacStatu.Repositories
{
    public static class FirmaVlastnenaStatemRepo
    {
        public static List<string> IcaStatnichFirem(int statniPodil)
        {
            using (DbEntities db = new DbEntities())
            {
                return db.FirmyVlastneneStatem
                    .AsNoTracking()
                    .Where(m => m.Podil >= statniPodil)
                    .Select(m => m.Ico)
                    .ToList();
            }
        }

        public static List<string> IcaStatnichFirem()
        {
            using (DbEntities db = new DbEntities())
            {
                return db.FirmyVlastneneStatem
                    .AsNoTracking()
                    .Select(m => m.Ico)
                    .ToList();
            }
        }
        

        public static void Repopulate(IEnumerable<FirmaVlastnenaStatem> percList)
        {
            using (DbEntities db = new DbEntities())
            {
                db.Database.ExecuteSqlRaw("TRUNCATE TABLE [FirmyVlastneneStatem]");
                db.FirmyVlastneneStatem.AddRange(percList);
                db.SaveChanges();
            }
            
        }
    }
}
