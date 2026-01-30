using HlidacStatu.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace HlidacStatu.Repositories
{
    public class QAITaskRepo
    {
        public static async Task<Tuple<bool, QAITask>> CreateNewAsync(QAITask tbl, bool addDuplicated = false, CancellationToken cancellationToken = default)
        {

            await using DbEntities db = new DbEntities();
            long? existing = null;

            if (addDuplicated == false)
            {
                existing = await GetDuplicatedAsync(tbl.CallerId, tbl.CallerTaskId, tbl.CallerTaskType, tbl.Source);
                if (existing != null)
                    return new Tuple<bool, QAITask>(false, await GetOnlySpecificAsync(existing.Value));
            }

            db.QAITask.Add(tbl);

            await db.SaveChangesAsync(cancellationToken);
            return new Tuple<bool, QAITask>(true, tbl);
        }

        public static async Task<QAITask> SaveExistingAsync(QAITask tbl, CancellationToken cancellationToken = default)
        {
            if (tbl.QId == 0)
                throw new ArgumentOutOfRangeException(nameof(tbl.QId), "Save only existing records");

            await using DbEntities db = new DbEntities();

            db.QAITask.Attach(tbl);
            db.Entry(tbl).State = EntityState.Modified;

            await db.SaveChangesAsync(cancellationToken);
            return tbl;
        }


        public static async Task<QAITask> GetNextToProcessAsync(string processEngine, 
            string callerId = null,
            string callerTaskId = null,
            string callerTaskType = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            await using (DbEntities db = new DbEntities())
            {

                var tbl = await db.QAITask
                    .AsQueryable()
                    .Where(m => 
                        m.Status == (int)HlidacStatu.DS.Api.AITask.Task.CheckState.WaitingInQueue
                        && (m.CallerId == callerId || callerId==null)
                        && (m.CallerTaskId == callerTaskId || callerTaskId == null)
                        && (m.CallerTaskType == callerTaskType || callerTaskType == null)
                    )
                    .OrderByDescending(m => m.Priority).ThenBy(m=>m.Created)
                    .FirstOrDefaultAsync(cancellationToken);

                if (tbl is null)
                    return null;

                tbl.Status = (int)HlidacStatu.DS.Api.AITask.Task.CheckState.InProgress;
                tbl.Started = DateTime.Now;
                tbl.LastUpdate = tbl.Started;
                tbl.ProcessEngine = processEngine;

                await db.SaveChangesAsync(cancellationToken);

                return tbl;
            }
        }


        public static async Task<QAITask> FinishAsync(HlidacStatu.DS.Api.AITask.Task task,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var q = await GetOnlySpecificAsync(task.QId, cancellationToken);
            if (q != null)
            {
                q.Status = (int)task.Status;
                q.Done = DateTime.Now;
                q.Result = task.ResultSerialized;
                q.ResultType = task.ResultType;
                q.LastUpdate = DateTime.Now;
                await SaveExistingAsync(q, cancellationToken: cancellationToken);

                //TODO save to await HlidacStatu.Repositories.PermanentLLMRepo.SaveAsync(llmmm);
                if (
                    task.ResultType == typeof(HlidacStatu.Entities.AI.ApiAITaskSummary).FullName
                    )
                {
                    try
                    {
                        HlidacStatu.Entities.AI.ApiAITaskSummary summ =
                            Newtonsoft.Json.JsonConvert.DeserializeObject<HlidacStatu.Entities.AI.ApiAITaskSummary>(task.ResultSerialized);
                        if (summ?.Summaries?.Length > 0)
                        {
                            foreach (var sum in summ.Summaries)
                            {
                                await HlidacStatu.Repositories.PermanentLLMRepo.SaveAsync(sum);
                            }
                        }
                        if (task.CallerId == "smlouva")
                        {
                            var s = await SmlouvaRepo.LoadAsync(task.CallerTaskId);
                            if (s != null && s.AIready != 1)
                            {
                                s.AIready = 1;
                                await SmlouvaRepo.SaveAsync(s);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        q.Status = (int)HlidacStatu.DS.Api.AITask.Task.CheckState.ErrorDuringDeSerialization;
                        await SaveExistingAsync(q, cancellationToken: cancellationToken);

                    }
                }
            }
            return q;
        }
        public static async Task<QAITask> SetStatusAsync(long qId, HlidacStatu.DS.Api.AITask.Task.CheckState status, CancellationToken cancellationToken = default(CancellationToken))
        {
            var q = await GetOnlySpecificAsync(qId, cancellationToken);
            if (q != null)
            {
                q.Status = (int)status;
                q.LastUpdate = DateTime.Now;
                await SaveExistingAsync(q, cancellationToken: cancellationToken);
            }
            return q;
        }

        public static async Task<bool> IsDuplicatedAsync(string callerId, string callerTaskId, string callerTaskType, string sourceUri)
        {
            long? exists = await GetDuplicatedAsync(callerId, callerTaskId, callerTaskType, sourceUri);

            return exists != null;

        }


        public static async Task<long?> GetDuplicatedAsync(string callerId, string callerTaskId, string callerTaskType, string sourceUri)
        {

            await using (DbEntities db = new DbEntities())
            {
                var filter = db.QAITask
                    .AsQueryable()
                    .Where(m => m.CallerId == callerId
                        && m.CallerTaskId == callerTaskId
                        && m.CallerTaskType == callerTaskType
                    );
                if (Uri.IsWellFormedUriString(sourceUri, UriKind.Absolute))
                    filter = filter.Where(m => m.Source == sourceUri);

                var q1 = await filter
                    .Where(m =>
                            m.Status == (int)HlidacStatu.DS.Api.AITask.Task.CheckState.WaitingInQueue
                    )
                    .Select(m => new { id = m.QId, status = m.Status })
                    .FirstOrDefaultAsync();

                if (q1 != null)
                    return q1.id;

                var q2 = await filter
                .AsQueryable()
                .Where(m =>
                        m.Status != (int)HlidacStatu.DS.Api.AITask.Task.CheckState.Error
                //&& m.Result != "[]"
                )
                .Select(m => new { id = m.QId, status = m.Status })
                .FirstOrDefaultAsync();

                if (q2 != null)
                    return q2.id;

                return null;
            }
        }


        public static async Task<QAITask[]> GetByParametersAsync(int maxItems = 1000,
        string? callerId = null, string? callerTaskId = null, string? callerTaskType = null,
        string source = null,
        HlidacStatu.DS.Api.AITask.Task.CheckState? status = null,
        CancellationToken cancellationToken = default(CancellationToken))
        {
            await using (DbEntities db = new DbEntities())
            {
                var query = db.QAITask
                    .AsQueryable();
                if (!string.IsNullOrEmpty(callerId))
                    query = query.Where(m => m.CallerId == callerId);
                if (!string.IsNullOrEmpty(callerTaskId))
                    query = query.Where(m => m.CallerTaskId == callerTaskId);
                if (!string.IsNullOrEmpty(callerTaskType))
                    query = query.Where(m => m.CallerTaskType == callerTaskType);
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

        public static async Task<QAITask> GetOnlySpecificAsync(long qId, CancellationToken cancellationToken = default(CancellationToken))
        {
            await using (DbEntities db = new DbEntities())
            {
                var tbl = await db.QAITask
                    .AsQueryable()
                    .Where(m => m.QId == qId)
                    .FirstOrDefaultAsync(cancellationToken);

                return tbl;
            }
        }

        public static List<QAITask> GetAll()
        {
            using DbEntities db = new DbEntities();
            return db.QAITask
                .AsNoTracking()
                .ToList();
        }

        public static async Task<int> WaitingInQueueCountAsync(CancellationToken cancellationToken)
        {
            await using var db = new DbEntities();
            var count = await db.QAITask
                .AsNoTracking()
                .Where(m => m.Status == (int)InDocTables.CheckState.WaitingInQueue)
                .CountAsync(cancellationToken);

            return count;
        }

        public static async Task<int> WaitingInQueueBeforeIdCountAsync(long qId, CancellationToken cancellationToken)
        {
            await using var db = new DbEntities();
            var count = (await db.QAITask
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
