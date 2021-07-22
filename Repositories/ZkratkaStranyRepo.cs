using System.Collections.Generic;
using System.Linq;
using HlidacStatu.Entities;

namespace HlidacStatu.Repositories
{
    public static class ZkratkaStranyRepo
    {
        
        /// <returns> Dictionary; Key=Ico, Value=Zkratka </returns>
        public static Dictionary<string, string> ZkratkyVsechStran()
        {
            using (DbEntities db = new DbEntities())
            {
                return db.ZkratkaStrany.ToDictionary(z => z.Ico, e => e.KratkyNazev);
            }
        }
    }
}