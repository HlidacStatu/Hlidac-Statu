using HlidacStatu.DS.Api;
using Microsoft.EntityFrameworkCore;

using System;
using System.Collections.Generic;
using System.Linq;

namespace HlidacStatu.Entities
{
    public partial class ItemToOcrQueue
    {
        public void SetOptions(OcrWork.TaskOptions option)
        {
            if (option == null)
                this.Options = null;
            else
                this.Options = Newtonsoft.Json.JsonConvert.SerializeObject(option);

            _options = option;
        }

        OcrWork.TaskOptions _options = null;
        public OcrWork.TaskOptions GetOptions()
        {
            if (_options == null)
            {
                var itemOptions = OcrWork.TaskOptions.Default;
                try
                {
                    if (string.IsNullOrEmpty(this.Options))
                        return OcrWork.TaskOptions.Default;

                    itemOptions = Newtonsoft.Json.JsonConvert.DeserializeObject<OcrWork.TaskOptions>(this.Options);

                }
                catch (Exception)
                {
                }
                _options = itemOptions;
            }
            return _options;
        }





        private static IQueryable<ItemToOcrQueue> CreateQuery(DbEntities db, OcrWork.DocTypes? itemType, string itemSubType)
        {
            IQueryable<ItemToOcrQueue> sql = null;
            sql = db.ItemToOcrQueue.AsQueryable()
                .Where(m => m.Done == null
                        && m.Started == null
                        && m.WaitForPK == null);

            if (itemType != null)
                sql = sql.Where(m => m.ItemType == itemType.ToString());

            if (!string.IsNullOrEmpty(itemSubType))
                sql = sql.Where(m => m.ItemSubType == itemSubType);

            return sql;
        }
        public static bool AreThereItemsToProcess(OcrWork.DocTypes? itemType = null, string itemSubType = null)
        {
            using (DbEntities db = new DbEntities())
            {
                var sql = CreateQuery(db, itemType, itemSubType);
                return sql.Any();
            }
        }

        static object lockTakeFromQueue = new object();
        public static IEnumerable<ItemToOcrQueue> TakeFromQueue(OcrWork.DocTypes? itemType = null, string itemSubType = null, int maxItems = 30)
        {
            using (DbEntities db = new DbEntities())
            {
                lock (lockTakeFromQueue)
                {
                    var strategy = db.Database.CreateExecutionStrategy();
                    ItemToOcrQueue[] res = strategy.Execute(() =>
                    {

                        using (var dbTran = db.Database.BeginTransaction(System.Data.IsolationLevel.RepeatableRead))
                        {

                            try
                            {
                                IQueryable<ItemToOcrQueue> sql = CreateQuery(db, itemType, itemSubType);

                                sql = sql
                                    .OrderByDescending(m => m.Priority)
                                    .ThenBy(m => m.Created)
                                    .Take(maxItems);
                                var res = sql.ToArray();
                                foreach (var i in res)
                                {
                                    i.Started = DateTime.Now;
                                }
                                db.SaveChanges();
                                dbTran.Commit();
                                return res;
                            }
                            catch (Exception)
                            {
                                dbTran.Rollback();
                                throw;
                            }
                        }
                    });

                    return res;
                }

            }
        }


       


        public static void AddNewTask(OcrWork.DocTypes itemType, string itemId, string itemSubType = null,
            DS.Api.OcrWork.TaskPriority priority = DS.Api.OcrWork.TaskPriority.Standard,
            OcrWork.TaskOptions options = null
            )
        {
             AddNewTask(itemType, itemId, itemSubType, (int)priority, options);
        }
        public static void AddNewTask(OcrWork.DocTypes itemType, string itemId,
            string itemSubType = null, int priority = 5,
            OcrWork.TaskOptions options = null
            )
        {
            options = options ?? OcrWork.TaskOptions.Default;

            using (DbEntities db = new DbEntities())
            {
                IQueryable<ItemToOcrQueue> sql = CreateQuery(db, itemType, itemSubType);
                sql = sql.Where(m => m.ItemId == itemId);
                var _sql = sql.ToQueryString();
               

                ItemToOcrQueue i = new ItemToOcrQueue();
                i.Created = DateTime.Now;
                i.ItemId = itemId;
                i.ItemType = itemType.ToString();
                i.ItemSubType = itemSubType;
                i.Priority = priority;
                i.SetOptions(options);
                db.ItemToOcrQueue.Add(i);
                db.SaveChanges();
                
            }
        }

        public static void ResetTask(long taskItemId, bool decreasePriority = true)
        {
            using (DbEntities db = new DbEntities())
            {

                ItemToOcrQueue i = db.ItemToOcrQueue.AsQueryable().Where(m => m.Pk == taskItemId).FirstOrDefault();
                if (i != null)
                {
                    i.Done = null;
                    i.Started = null;
                    i.Success = null;
                    i.Result = null;
                    if (decreasePriority)
                    {
                        i.Priority--;
                        if (i.Priority < 1)
                            i.Priority = 1;
                    }
                    db.SaveChanges();
                }
            }
        }


        public static ItemToOcrQueue GetTask(long taskItemId)
        {
            using (DbEntities db = new DbEntities())
            {

                ItemToOcrQueue i = db.ItemToOcrQueue
                    .AsNoTracking()
                    .Where(m => m.Pk == taskItemId)
                    .FirstOrDefault();

                return i;
            }
        }

        public static void SetDone(long taskItemId, bool success, string result = null)
        {
            using (DbEntities db = new DbEntities())
            {

                ItemToOcrQueue i = db.ItemToOcrQueue.AsQueryable().FirstOrDefault(m => m.Pk == taskItemId);
                if (i != null)
                {
                    i.Done = DateTime.Now;
                    i.Success = success ? 1 : 0;
                    i.Result = result;
                }

                var waitingTask = db.ItemToOcrQueue.FirstOrDefault(m => m.WaitForPK == taskItemId);
                if (waitingTask != null)
                    waitingTask.WaitForPK = null;

                _= db.SaveChanges();
            }
        }

    }
}
