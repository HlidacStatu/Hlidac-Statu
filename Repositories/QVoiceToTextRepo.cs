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
        public static async Task<QVoiceToText> SaveAsync(QVoiceToText tbl, bool checkExisting = true, CancellationToken cancellationToken = default)
        {
            await using DbEntities db = new DbEntities();

            var exists = db.QVoiceToText.AsNoTracking().FirstOrDefault(m =>
                    m.CallerId == tbl.CallerId
                    && m.CallerTaskId == tbl.CallerTaskId
                    && m.Status == (int)HlidacStatu.DS.Api.Voice2Text.Task.CheckState.WaitingInQueue
                    );


            if (checkExisting && exists != null)
                tbl.QId = exists.QId;


            db.QVoiceToText.Attach(tbl);
            if (tbl.QId == 0)
                db.Entry(tbl).State = EntityState.Added;
            else
                db.Entry(tbl).State = EntityState.Modified;

            await db.SaveChangesAsync(cancellationToken);
            return tbl;
        }


        public static async Task<QVoiceToText> GetNextToProcessAsync(string processEngine, CancellationToken cancellationToken = default(CancellationToken))
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
                tbl.ProcessEngine = processEngine;

                await db.SaveChangesAsync(cancellationToken);

                return tbl;
            }
        }


        public static async Task<QVoiceToText> FinishAsync(long qId, string result,
            HlidacStatu.DS.Api.Voice2Text.Task.CheckState status,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var q = await GetOnlySpecificAsync(qId, cancellationToken);
            if (q != null)
            {
                q.Status = (int)status;
                q.Done = DateTime.Now;
                q.Result = result;
                q.LastUpdate = DateTime.Now;
                await SaveAsync(q, cancellationToken: cancellationToken);
            }
            return q;
        }
        public static async Task<QVoiceToText> SetStatusAsync(long qId, HlidacStatu.DS.Api.Voice2Text.Task.CheckState status, CancellationToken cancellationToken = default(CancellationToken))
        {
            var q = await GetOnlySpecificAsync(qId, cancellationToken);
            if (q != null)
            {
                q.Status = (int)status;
                q.LastUpdate = DateTime.Now;
                await SaveAsync(q, cancellationToken: cancellationToken);
            }
            return q;
        }

        public static async Task<bool> IsDuplicatedBySourceAsync(string sourceUri)
        {
            if (!Uri.IsWellFormedUriString(sourceUri, UriKind.Absolute))
                return true;


            await using (DbEntities db = new DbEntities())
            {
                var q1 = await db.QVoiceToText
                    .AsQueryable()
                    .Where(m =>
                            m.Source == sourceUri &&
                            m.Status == (int)HlidacStatu.DS.Api.Voice2Text.Task.CheckState.WaitingInQueue
                    )
                    .AnyAsync();

                if ( q1)
                    return true;

                var q2 = await db.QVoiceToText
                .AsQueryable()
                .Where(m =>
                        m.Source == sourceUri
                        && m.Status != (int)HlidacStatu.DS.Api.Voice2Text.Task.CheckState.Error
                        //&& m.Result != "[]"
                )
                .AnyAsync();
                if (q2)
                    return true;

                return false;
            }
        }
        public static async Task<QVoiceToText[]> GetByParametersAsync(int maxItems = 1000,
        string? callerId = null, string? callerTaskId = null,
        string source = null,
        HlidacStatu.DS.Api.Voice2Text.Task.CheckState? status = null,
        CancellationToken cancellationToken = default(CancellationToken))
        {
            await using (DbEntities db = new DbEntities())
            {
                var query = db.QVoiceToText
                    .AsQueryable();
                if (!string.IsNullOrEmpty(callerId))
                    query = query.Where(m => m.CallerId == callerId);
                if (!string.IsNullOrEmpty(callerTaskId))
                    query = query.Where(m => m.CallerTaskId == callerTaskId);
                if (source != null)
                    query = query.Where(m => m.Source == source);
                if (status.HasValue)
                    query = query.Where(m => m.Status == (int)status.Value);

                var tbl = await query
                    .OrderByDescending(o => (o.LastUpdate ?? o.Created))
                    .Take(maxItems)
                    .ToArrayAsync(cancellationToken);

                return tbl;
            }
        }

        public static async Task<QVoiceToText> GetOnlySpecificAsync(long qId, CancellationToken cancellationToken = default(CancellationToken))
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

        public static async Task<int> WaitingInQueueCountAsync(CancellationToken cancellationToken)
        {
            await using var db = new DbEntities();
            var count = await db.QVoiceToText
                .AsNoTracking()
                .Where(m => m.Status == (int)InDocTables.CheckState.WaitingInQueue)
                .CountAsync(cancellationToken);

            return count;
        }

        public static async Task<int> WaitingInQueueBeforeIdCountAsync(long qId, CancellationToken cancellationToken)
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
