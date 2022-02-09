using HlidacStatu.Entities;

using Microsoft.EntityFrameworkCore;

using System.Collections.Generic;

namespace HlidacStatu.Repositories
{
    public static class UptimeServerRepo
    {
        public static void Save(UptimeServer uptimeServer)
        {
            using (DbEntities db = new DbEntities())
            {
                db.UptimeServers.Attach(uptimeServer);
                if (string.IsNullOrEmpty(uptimeServer.Id))
                    db.Entry(uptimeServer).State = EntityState.Added;
                else
                    db.Entry(uptimeServer).State = EntityState.Modified;

                db.SaveChanges();
            }
        }

        public static List<UptimeServer> GetServersToCheck(int numOfServers=30)
        {
            using (DbEntities db = new DbEntities())
            {
                var list = db.UptimeServers.FromSqlInterpolated($"exec GetUptimeServers {numOfServers}")
                    .AsNoTracking()
                    .ToListAsync()
                    .Result;

                return list;
        }


    }
}