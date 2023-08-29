using HlidacStatu.DS.Api;
using Microsoft.EntityFrameworkCore;

using System;
using System.Collections.Generic;
using System.Linq;

namespace HlidacStatu.Entities
{
    public partial class ItemToOcrQueue
    {
        public void SetOptions(ItemOption option)
        {
            if (option == null)
                this.Options = null;
            else
                this.Options = Newtonsoft.Json.JsonConvert.SerializeObject(option);

            _options = option;
        }

        ItemOption _options = null;
        public ItemOption GetOptions()
        {
            if (_options == null)
            {
                var itemOptions = ItemOption.Default;
                try
                {
                    if (string.IsNullOrEmpty(this.Options))
                        return ItemOption.Default;

                    itemOptions = Newtonsoft.Json.JsonConvert.DeserializeObject<ItemOption>(this.Options);

                }
                catch (Exception)
                {
                }
                _options = itemOptions;
            }
            return _options;
        }

        public enum ItemToOcrType
        {
            Smlouva,
            Insolvence,
            Dataset,
            VerejnaZakazka
        }

        public ItemToOcrType OcrWorkToItemToOcrType(OcrWork.DocTypes doctype)
        {
            switch (doctype)
            {
                case OcrWork.DocTypes.Smlouva:
                    return ItemToOcrType.Smlouva;
                case OcrWork.DocTypes.VerejnaZakazka:
                    return ItemToOcrType.VerejnaZakazka;
                case OcrWork.DocTypes.Dataset:
                    return ItemToOcrType.Dataset;
                case OcrWork.DocTypes.Insolvence:
                    return ItemToOcrType.Insolvence;
                default:
                    return ItemToOcrType.Smlouva;
            }
        }

        public OcrWork.DocTypes ItemToOcrTypeToOcrWork(ItemToOcrType ocrType)
        {
            switch (ocrType)
            {
                case ItemToOcrType.Smlouva:
                    return OcrWork.DocTypes.Smlouva;
                case ItemToOcrType.VerejnaZakazka:
                    return OcrWork.DocTypes.VerejnaZakazka;
                case ItemToOcrType.Dataset:
                    return OcrWork.DocTypes.Dataset;
                case ItemToOcrType.Insolvence:
                    return OcrWork.DocTypes.Insolvence;
                default:
                    return OcrWork.DocTypes.Smlouva;
            }
        }

        private static IQueryable<ItemToOcrQueue> CreateQuery(DbEntities db, ItemToOcrType? itemType, string itemSubType)
        {
            IQueryable<ItemToOcrQueue> sql = null;
            sql = db.ItemToOcrQueue.AsQueryable()
                .Where(m => m.Done == null
                        && m.Started == null);

            if (itemType != null)
                sql = sql.Where(m => m.ItemType == itemType.ToString());

            if (!string.IsNullOrEmpty(itemSubType))
                sql = sql.Where(m => m.ItemSubType == itemSubType);

            return sql;
        }
        public static bool AreThereItemsToProcess(ItemToOcrType? itemType = null, string itemSubType = null)
        {
            using (DbEntities db = new DbEntities())
            {
                var sql = CreateQuery(db, itemType, itemSubType);
                return sql.Any();
            }
        }

        static object lockTakeFromQueue = new object();
        public static IEnumerable<ItemToOcrQueue> TakeFromQueue(ItemToOcrType? itemType = null, string itemSubType = null, int maxItems = 30)
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


        public class ItemOption
        {
            public bool force { get; set; } = false;
            public bool missingOnly { get; set; } = true;
            public int? lengthLessThan { get; set; } = null;

            private static ItemOption _default = new ItemOption();
            public static ItemOption Default { get => _default; }

        }


        public static Lib.OCR.Api.Result AddNewTask(ItemToOcrType itemType, string itemId, string itemSubType = null,
            Lib.OCR.Api.Client.TaskPriority priority = Lib.OCR.Api.Client.TaskPriority.Standard,
            ItemOption options = null
            )
        {
            return AddNewTask(itemType, itemId, itemSubType, (int)priority, options);
        }
        public static Lib.OCR.Api.Result AddNewTask(ItemToOcrType itemType, string itemId,
            string itemSubType = null, int priority = 5,
            ItemOption options = null
            )
        {
            options = options ?? ItemOption.Default;

            using (DbEntities db = new DbEntities())
            {
                IQueryable<ItemToOcrQueue> sql = CreateQuery(db, itemType, itemSubType);
                sql = sql.Where(m => m.ItemId == itemId);
                var _sql = sql.ToQueryString();
                if (sql.Any()) //already in the queue
                    return new Lib.OCR.Api.Result()
                    {
                        IsValid = Lib.OCR.Api.Result.ResultStatus.InQueueWithCallback,
                        Id = "uknown"
                    };

                ItemToOcrQueue i = new ItemToOcrQueue();
                i.Created = DateTime.Now;
                i.ItemId = itemId;
                i.ItemType = itemType.ToString();
                i.ItemSubType = itemSubType;
                i.Priority = priority;
                i.SetOptions(options);
                db.ItemToOcrQueue.Add(i);
                db.SaveChanges();
                return new Lib.OCR.Api.Result()
                {
                    IsValid = Lib.OCR.Api.Result.ResultStatus.InQueueWithCallback,
                    Id = "uknown"
                };
            }
        }

        public static void ResetTask(int taskItemId, bool decreasePriority = true)
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


        public static void SetDone(int taskItemId, bool success, string result = null)
        {
            using (DbEntities db = new DbEntities())
            {

                ItemToOcrQueue i = db.ItemToOcrQueue.AsQueryable().Where(m => m.Pk == taskItemId).FirstOrDefault();
                if (i != null)
                {
                    i.Done = DateTime.Now;
                    i.Success = success ? 1 : 0;
                    i.Result = result;
                    db.SaveChanges();
                }
            }
        }

    }
}
