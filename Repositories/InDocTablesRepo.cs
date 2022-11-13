using System;
using System.Collections.Generic;
using HlidacStatu.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HlidacStatu.Repositories.Exceptions;


namespace HlidacStatu.Repositories
{
    public class InDocTablesRepo
    {
        public static async Task SaveAsync(InDocTables tbl, CancellationToken cancellationToken = default)
        {
            await using DbEntities db = new DbEntities();
            db.InDocTables.Attach(tbl);
            if (tbl.Pk == 0)
                db.Entry(tbl).State = EntityState.Added;
            else
                db.Entry(tbl).State = EntityState.Modified;

            await db.SaveChangesAsync(cancellationToken);
        }
        
        public static async Task SaveIfNotExistsAsync(InDocTables tbl, CancellationToken cancellationToken = default(CancellationToken))
        {
            await using var db = new DbEntities();
                    
            bool exists = db.InDocTables.Any(m =>
                m.Algorithm == tbl.Algorithm
                && m.Page == tbl.Page
                && m.PrilohaHash == tbl.PrilohaHash
                && m.TableOnPage == tbl.TableOnPage
                && m.SmlouvaID == tbl.SmlouvaID
            );
            if (!exists)
            {
                db.Add(tbl);
                await db.SaveChangesAsync(cancellationToken);
            }
        }

        public static async Task<InDocTables> GetNextForCheck(string obor, string requestedBy, CancellationToken cancellationToken = default(CancellationToken))
        {
            await using (DbEntities db = new DbEntities())
            {
                //pokud zůstal rozpracovaný úkol, lízne si nejprve ten
                var inProgress = await db.InDocTables
                    .AsQueryable()
                    .Where(m => m.Status == (int)InDocTables.CheckState.InProgress)
                    .Where(m => m.CheckedBy == requestedBy)
                    .FirstOrDefaultAsync(cancellationToken);

                if (inProgress != null)
                    return inProgress;

                var tbl = await db.InDocTables
                    .AsQueryable()
                    .Where(m => m.Status == (int)InDocTables.CheckState.WaitingInQueue
                        && m.Klasifikace.StartsWith(obor))
                    .Where(m => m.Year == 2022) // 2022 priorita, později můžeme smazat
                    .OrderByDescending(m => m.PrecalculatedScore)
                    .FirstOrDefaultAsync(cancellationToken);

                if (tbl is null)
                    throw new NoDataFoundException("Table InDocTables neobsahuje zadna data ke zpracovani.");
                
                tbl.CheckStatus = InDocTables.CheckState.InProgress;
                tbl.CheckedBy = requestedBy;
                tbl.CheckedDate = DateTime.Now;
                
                await db.SaveChangesAsync(cancellationToken);
                
                return tbl;
            }
        }
        
        public static async Task<InDocTables> GetSpecific(int pk, string requestedBy, CancellationToken cancellationToken = default(CancellationToken))
        {
            await using (DbEntities db = new DbEntities())
            {
                var tbl = await db.InDocTables
                    .AsQueryable()
                    .Where(m => m.Pk == pk)
                    .FirstOrDefaultAsync(cancellationToken);
                
                if (tbl is null)
                    throw new NoDataFoundException("Table InDocTables neobsahuje zadna data ke zpracovani.");
                
                tbl.CheckStatus = InDocTables.CheckState.InProgress;
                tbl.CheckedBy = requestedBy;
                tbl.CheckedDate = DateTime.Now;

                return tbl;
            }
        }
        
        public static async Task<List<InDocTables>> GetHistory(string requestedBy, int take,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            await using (DbEntities db = new DbEntities())
            {
                var tbl = await db.InDocTables
                    .AsQueryable()
                    .Where(m => m.CheckedBy == requestedBy)
                    .OrderByDescending(m => m.CheckedDate)
                    .Take(take)
                    .ToListAsync(cancellationToken: cancellationToken);
                
                return tbl;
            }
        }
        
        public static async Task<Dictionary<InDocTables.CheckState, int>> GlobalStatistic(CancellationToken cancellationToken)
        {
            await using (DbEntities db = new DbEntities())
            {
                var data = await db.InDocTables
                    .AsNoTracking()
                    .Select(t => new { t.Status, t.CheckedBy, t.CheckElapsedInMs })
                    .ToListAsync(cancellationToken);
                    
                var stat = data.GroupBy(t => t.Status)
                    .ToDictionary(g => (InDocTables.CheckState)g.Key,
                        g => g.Count());

                return stat;
            }
        }
        
        public static async Task<Dictionary<InDocTables.CheckState, int>> UserStatistic(string user, CancellationToken cancellationToken)
        {
            await using (DbEntities db = new DbEntities())
            {
                var data = await db.InDocTables
                    .AsNoTracking()
                    .Where(t => t.CheckedBy == user)
                    .Select(t => new { t.Status, t.CheckedBy, t.CheckElapsedInMs })
                    .ToListAsync(cancellationToken);
                
                var stat = data.GroupBy(t => t.Status)
                    .ToDictionary(g => (InDocTables.CheckState)g.Key,
                        g => g.Count());

                return stat;
            }
        }
        
        public static List<InDocTables> GetAll()
        {
            using DbEntities db = new DbEntities();
            return db.InDocTables
                .AsNoTracking()
                .ToList();
        }

        public static async Task<int> WaitingInQueue(string obor, CancellationToken cancellationToken)
        {
            await using var db = new DbEntities();
            var count = await db.InDocTables
                .AsNoTracking()
                .Where(m => m.Status == (int)InDocTables.CheckState.WaitingInQueue)
                .Where(t => t.Klasifikace.StartsWith(obor))
                .CountAsync(cancellationToken);
            
            return count;
        }
        
        public static async Task<List<(string Klasifikace, int Pocet)>> WaitingInAllQueuesAsync(CancellationToken cancellationToken)
        {
            await using var db = new DbEntities();
            var count = await db.InDocTables
                .AsNoTracking()
                .Where(m => m.Status == (int)InDocTables.CheckState.WaitingInQueue)
                .Where(m => !string.IsNullOrEmpty(m.Klasifikace))
                .GroupBy(t => t.Klasifikace)
                .Select(g => (new { g.Key, Count = g.Count() }))
                .OrderBy(x => x.Key)
                .ToListAsync(cancellationToken);
                
            
            return count.Select(x => (x.Key, x.Count)).ToList();
        }
    }
}
