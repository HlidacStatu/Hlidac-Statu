using System;
using HlidacStatu.Entities;

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;


namespace HlidacStatu.Repositories
{
    public partial class InDocJobsRepo
    {
        public static async Task Save(List<InDocJobs> jobs)
        {
            await using (DbEntities db = new DbEntities())
            {
                foreach (var job in jobs)
                {
                    job.Created = DateTime.Now;
                }
                
                db.InDocJobs.AddRange(jobs);
                await db.SaveChangesAsync();
            }
        }
        
        public static async Task Remove(long tablePk)
        {
            await using (DbEntities db = new DbEntities())
            {
                await db.Database.ExecuteSqlInterpolatedAsync($"Delete from InDocJobs where tablepk = {tablePk}");
            }
        }
    }
}