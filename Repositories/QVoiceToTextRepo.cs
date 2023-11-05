using HlidacStatu.Entities;
using HlidacStatu.Repositories.Exceptions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace HlidacStatu.Repositories
{
    public class QVoiceToTextRepo
    {
        public static async Task SaveAsync(QVoiceToText tbl, CancellationToken cancellationToken = default)
        {
            await using DbEntities db = new DbEntities();
            db.QVoiceToText.Attach(tbl);
            if (tbl.QId == 0)
                db.Entry(tbl).State = EntityState.Added;
            else
                db.Entry(tbl).State = EntityState.Modified;

            await db.SaveChangesAsync(cancellationToken);
        }


        public static async Task<QVoiceToText> GetNextForCheck(CancellationToken cancellationToken = default(CancellationToken))
        {
            await using (DbEntities db = new DbEntities())
            {

                var tbl = await db.QVoiceToText
                    .AsQueryable()
                    .Where(m => m.Status == (int)QVoiceToText.CheckState.WaitingInQueue)
                    .OrderByDescending(m => m.Priority)
                    .FirstOrDefaultAsync(cancellationToken);

                if (tbl is null)
                    throw new NoDataFoundException("Table QVoiceToText neobsahuje zadna data ke zpracovani.");

                tbl.Status = (int)QVoiceToText.CheckState.InProgress;
                tbl.Started = DateTime.Now;

                await db.SaveChangesAsync(cancellationToken);

                return tbl;
            }
        }


        public static async Task<QVoiceToText> Finish(int qId, string result, QVoiceToText.CheckState status, CancellationToken cancellationToken = default(CancellationToken))
        {
            var q = await GetOnlySpecific(qId, cancellationToken);
            if (q != null)
            {
                q.Status = (int)status;
                q.Done = DateTime.Now;
                q.Result = result;
                q.LastUpdate = DateTime.Now;
                await SaveAsync(q, cancellationToken);
            }
            return q;
        }
        public static async Task<QVoiceToText> SetStatus(int qId, QVoiceToText.CheckState status, CancellationToken cancellationToken = default(CancellationToken))
        {
            var q = await GetOnlySpecific(qId, cancellationToken);
            if (q != null)
            {
                q.Status = (int)status;
                q.LastUpdate = DateTime.Now;
                await SaveAsync(q, cancellationToken);
            }
            return q;
        }

        public static async Task<QVoiceToText[]> GetByCaller(string callerId, string callerTaskId, CancellationToken cancellationToken = default(CancellationToken))
        {
            await using (DbEntities db = new DbEntities())
            {
                var tbl = await db.QVoiceToText
                    .AsQueryable()
                    .Where(m => m.CallerId == callerId && m.CallerTaskId == callerTaskId)
                    .ToArrayAsync(cancellationToken);

                return tbl;
            }
        }

        public static async Task<QVoiceToText> GetOnlySpecific(int qId, CancellationToken cancellationToken = default(CancellationToken))
        {
            await using (DbEntities db = new DbEntities())
            {
                var tbl = await db.QVoiceToText
                    .AsQueryable()
                    .Where(m => m.QId == qId)
                    .FirstOrDefaultAsync(cancellationToken);

                return tbl;
            }
        }

        public static List<InDocTables> GetAll()
        {
            using DbEntities db = new DbEntities();
            return db.InDocTables
                .AsNoTracking()
                .ToList();
        }

        public static async Task<int> WaitingInQueueCount(CancellationToken cancellationToken)
        {
            await using var db = new DbEntities();
            var count = await db.InDocTables
                .AsNoTracking()
                .Where(m => m.Status == (int)InDocTables.CheckState.WaitingInQueue)
                .CountAsync(cancellationToken);

            return count;
        }

    }
}
