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
    public partial class InDocTablesRepo
    {
        public static void Save(InDocTables tbl)
        {
            using (DbEntities db = new DbEntities())
            {
                db.InDocTables.Attach(tbl);
                if (tbl.Pk == 0)
                    db.Entry(tbl).State = EntityState.Added;
                else
                    db.Entry(tbl).State = EntityState.Modified;

                db.SaveChanges();
            }
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
                        && m.Subject.StartsWith(obor))
                    .Where(m => m.Year == 2018) // tohle pak smazat, až doděláme demodata
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

        //todo: change this so it is more obvious it saves all the table
        // it is important to save category here! (or somewhere else)
        // but we cant forget about this
        public static async Task ChangeStatus(InDocTables tbl, InDocTables.CheckState status, string checkedBy,
            long checkElapsedTimeInMS)
        {
            await using (DbEntities db = new DbEntities())
            {
                db.InDocTables.Attach(tbl);
                if (tbl.Pk == 0)
                    db.Entry(tbl).State = EntityState.Added;
                else
                    db.Entry(tbl).State = EntityState.Modified;

                tbl.CheckStatus = status;
                tbl.CheckedBy = checkedBy;
                tbl.CheckedDate = System.DateTime.Now;
                tbl.CheckElapsedInMs = (int)checkElapsedTimeInMS;

                await db.SaveChangesAsync();

            }
        }

        public static async Task ChangeCategoryAsync(InDocTables tbl, string category)
        {
            await using (DbEntities db = new DbEntities())
            {
                db.InDocTables.Attach(tbl);
                if (tbl.Pk == 0)
                    db.Entry(tbl).State = EntityState.Added;
                else
                    db.Entry(tbl).State = EntityState.Modified;

                tbl.Subject = category;
                
                await db.SaveChangesAsync();

            }
        }
        
        public static List<InDocTables> GetAll()
        {
            using DbEntities db = new DbEntities();
            return db.InDocTables
                .AsNoTracking()
                .ToList();
        }


    }
}
