using HlidacStatu.Entities;
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


        public static async Task<QVoiceToText> GetNextToProcess(CancellationToken cancellationToken = default(CancellationToken))
        {
            await using (DbEntities db = new DbEntities())
            {

                var tbl = await db.QVoiceToText
                    .AsQueryable()
                    .Where(m => m.Status == (int)HlidacStatu.DS.Api.Voice2Text.Task.CheckState.WaitingInQueue)
                    .OrderByDescending(m => m.Priority)
                    .FirstOrDefaultAsync(cancellationToken);

                if (tbl is null)
                    return null;

                tbl.Status = (int)HlidacStatu.DS.Api.Voice2Text.Task.CheckState.InProgress;
                tbl.Started = DateTime.Now;

                await db.SaveChangesAsync(cancellationToken);

                return tbl;
            }
        }


        public static async Task<QVoiceToText> Finish(long qId, string result, HlidacStatu.DS.Api.Voice2Text.Task.CheckState status, CancellationToken cancellationToken = default(CancellationToken))
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
        public static async Task<QVoiceToText> SetStatus(long qId, HlidacStatu.DS.Api.Voice2Text.Task.CheckState status, CancellationToken cancellationToken = default(CancellationToken))
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

        public static async Task<QVoiceToText[]> GetByParameters(int maxItems = 1000, string? callerId=null, string? callerTaskId=null, HlidacStatu.DS.Api.Voice2Text.Task.CheckState? status=null, CancellationToken cancellationToken = default(CancellationToken))
        {
            await using (DbEntities db = new DbEntities())
            {
                var query = db.QVoiceToText
                    .AsQueryable();
                if (!string.IsNullOrEmpty(callerId))
                    query = query.Where(m => m.CallerId == callerId);
                if (!string.IsNullOrEmpty(callerTaskId))
                    query = query.Where(m => m.CallerTaskId == callerTaskId);
                if (status.HasValue)
                    query = query.Where(m => m.Status == (int)status.Value);

                var tbl = await query
                    .OrderByDescending(o=>( o.LastUpdate ?? o.Created))
                    .Take(maxItems)
                    .ToArrayAsync(cancellationToken);

                return tbl;
            }
        }

        public static async Task<QVoiceToText> GetOnlySpecific(long qId, CancellationToken cancellationToken = default(CancellationToken))
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

        public static List<QVoiceToText> GetAll()
        {
            using DbEntities db = new DbEntities();
            return db.QVoiceToText
                .AsNoTracking()
                .ToList();
        }

        public static async Task<int> WaitingInQueueCount(CancellationToken cancellationToken)
        {
            await using var db = new DbEntities();
            var count = await db.QVoiceToText
                .AsNoTracking()
                .Where(m => m.Status == (int)InDocTables.CheckState.WaitingInQueue)
                .CountAsync(cancellationToken);

            return count;
        }

        public static async Task<int> WaitingInQueueBeforeIdCount(long qId, CancellationToken cancellationToken)
        {
            await using var db = new DbEntities();
            var count = (await db.QVoiceToText
                .AsNoTracking()
                .Where(m => m.Status == (int)InDocTables.CheckState.WaitingInQueue)
                .Select((m, i) => new { qid = m.QId, index = i })
                .ToArrayAsync())
                .FirstOrDefault(r => r.qid == qId)
                ?.index ?? 0;

            return count;
        }
    }
}
