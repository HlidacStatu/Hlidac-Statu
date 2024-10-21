using HlidacStatu.Entities;
using LinqToTwitter;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace HlidacStatu.Repositories
{
    public class QTblsInDocRepo
    {


        public static async Task<bool> CreateNewTaskAsync(string smlouvaId, bool force, int priority = 10)
        {
            var sml = await SmlouvaRepo.LoadAsync(smlouvaId, includePrilohy: false);
            if (sml != null)
            {
                return await CreateNewTaskAsync(sml, force, priority);
            }
            return false;
        }

        public static async Task<bool> CreateNewTaskAsync(Smlouva sml, bool force, int priority = 10)
        {
            var ret = false;
            foreach (var pri in sml.Prilohy)
            {
                //var tbls = HlidacStatu.Lib.Data.External.Tables.SmlouvaPrilohaExtension.GetTablesFromPriloha(sml, pri);


                if (force
                    || (await DocTablesRepo.ExistsAsync(sml.Id, pri.UniqueHash())) == false
                    )
                {
                    var request = new HlidacStatu.DS.Api.TablesInDoc.Task()
                    {
                        smlouvaId = sml.Id,
                        prilohaId = pri.UniqueHash(),
                        url = pri.LocalCopyUrl(sml.Id, true)
                    };

                    await CreateNewTaskAsync(request, priority: priority);
                }
            }
            return ret;
        }
        public static async Task<QTblsInDoc> CreateNewTaskAsync(HlidacStatu.DS.Api.TablesInDoc.Task req, bool checkExisting = true, int priority = 10, CancellationToken cancellationToken = default)
        {
            var item = new QTblsInDoc();
            item.SmlouvaId = req.smlouvaId;
            item.PrilohaId = req.prilohaId;
            item.Url = req.url;
            item.Priority = priority;
            return await CreateNewTaskAsync(item, checkExisting, priority);
        }
        public static async Task<QTblsInDoc> CreateNewTaskAsync(QTblsInDoc tbl, bool checkExisting = true, int priority = 10, CancellationToken cancellationToken = default)
        {
            await using DbEntities db = new DbEntities();

            var exists = db.QTblsInDoc.AsNoTracking().FirstOrDefault(m =>
                    m.SmlouvaId == tbl.SmlouvaId
                    && m.PrilohaId == tbl.PrilohaId
                    && m.Done == null
                    );


            if (checkExisting && exists != null)
                return exists;


            db.QTblsInDoc.Attach(tbl);
            if (tbl.Pk == 0)
                db.Entry(tbl).State = EntityState.Added;
            else
                db.Entry(tbl).State = EntityState.Modified;

            await db.SaveChangesAsync(cancellationToken);
            return tbl;
        }
        public static async Task<QTblsInDoc> AddOrUpdateTaskAsync(QTblsInDoc tbl, CancellationToken cancellationToken = default)
        {
            await using DbEntities db = new DbEntities();

            var exists = db.QTblsInDoc.AsNoTracking().FirstOrDefault(m =>
                    m.SmlouvaId == tbl.SmlouvaId
                    && m.PrilohaId == tbl.PrilohaId
                    && m.Done == null
                    );


            if (exists != null)
                tbl.Pk = exists.Pk;


            db.QTblsInDoc.Attach(tbl);
            if (tbl.Pk == 0)
                db.Entry(tbl).State = EntityState.Added;
            else
                db.Entry(tbl).State = EntityState.Modified;

            await db.SaveChangesAsync(cancellationToken);
            return tbl;
        }

        public static async Task<QTblsInDoc> GetNextToProcessAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            await using (DbEntities db = new DbEntities())
            {

                var tbl = await db.QTblsInDoc
                    .AsQueryable()
                    .Where(m => m.Started == null)
                    .OrderByDescending(m => m.Priority).ThenBy(m => m.Created)
                    .FirstOrDefaultAsync(cancellationToken);

                if (tbl is null)
                    return null;

                tbl.Started = DateTime.Now;

                await db.SaveChangesAsync(cancellationToken);

                return tbl;
            }
        }
        public static async Task<List<QTblsInDoc>> GetNextToProcessAsync(int count, CancellationToken cancellationToken = default(CancellationToken))
        {
            List<QTblsInDoc> items = new();
            for (int i = 0; i < count; i++)
            {
                var q = await GetNextToProcessAsync(cancellationToken);
                if (q == null)
                    break;
                else
                    items.Add(q);
            }
            return items;
        }


        public static async Task<QTblsInDoc> SetDoneAsync(string smlouvaId, string prilohaId,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            await using (DbEntities db = new DbEntities())
            {

                var tbl = await db.QTblsInDoc
                    .AsQueryable()
                    .Where(m => m.SmlouvaId == smlouvaId && m.PrilohaId == prilohaId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (tbl is null)
                    return null;

                tbl.Done = DateTime.Now;

                _ = await db.SaveChangesAsync(cancellationToken);

                return tbl;
            }
        }



        public static async Task<int> WaitingInQueueCountAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                await using var db = new DbEntities();

                var count = await db.QTblsInDoc
                .AsNoTracking()
                .Where(m => m.Started == null)
                .CountAsync(cancellationToken);

                return count;
            }
            catch (Exception e)
            {

                throw;
            }
        }
        public static int WaitingInQueueCount()
        {
            try
            {
                using var db = new DbEntities();

                var count = db.QTblsInDoc
                .AsNoTracking()
                .Where(m => m.Started == null)
                .Count();

                return count;
            }
            catch (Exception e)
            {

                throw;
            }
        }

    }
}
