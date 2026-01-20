using HlidacStatu.Connectors;
using HlidacStatu.Entities;
using HlidacStatu.Extensions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Serilog;
using HlidacStatu.Repositories.Cache;

namespace HlidacStatu.Repositories
{
    public class RecalculateItemRepo
    {
        private static ILogger _logger = Log.ForContext<RecalculateItemRepo>();

        //public const string RECALCULATIONQUEUENAME = "recalculation2Process";
        static Entities.RecalculateItemEqComparer comparer = new();

        static Devmasters.Batch.ActionProgressWriter debugProgressWr = new Devmasters.Batch.ActionProgressWriter(0.1f);

        public static int RecalculateQueueLength()
        {
            return DirectDB.Instance.GetValue<int>("select count(*) from RecalculateItemQueue with (nolock)");
        }

        public static async Task RecalculateTasksAsync(int? threads = null, bool debug = false, string[] ids = null,
            Action<string> outputWriter = null, Devmasters.Batch.IProgressWriter  progressWriter = null,
            int? maxItemsInBatch = null, bool allItemsInDb = false,
            bool invalidateOnly = false
        )
        {
            await Devmasters.Batch.Manager.DoActionForAllAsync<int>(Enumerable.Range(0, int.MaxValue - 1), async xx =>
                {
                    var items = await RecalculateItemRepo.GetFromProcessingQueueWithParentsAsync(1, 1, debug: debug);
                    if (items.Count() == 0)
                        return new Devmasters.Batch.ActionOutputData() { CancelRunning = true };

                    await Devmasters.Batch.Manager.DoActionForAllAsync<RecalculateItem>(items, async item =>
                        {
                            if (debug)
                                _logger.Debug($"start statistics {item.ItemType.ToString()} {item.Id}");
                            if (item.ItemType == RecalculateItem.ItemTypeEnum.Subjekt)
                                await RecalculateItemRepo.RecalculateFirmaAsync(item, false, invalidateOnly);
                            else if (item.ItemType == RecalculateItem.ItemTypeEnum.Person)
                                await RecalculateItemRepo.RecalculateOsobaAsync(item, false, invalidateOnly);
                            if (debug)
                                _logger.Debug($"end statistics {item.ItemType.ToString()} {item.Id}");

                            return new Devmasters.Batch.ActionOutputData();
                        },
                        null, null,
                        !System.Diagnostics.Debugger.IsAttached, maxDegreeOfParallelism: 2,
                        monitor: null
                    );
                    return new Devmasters.Batch.ActionOutputData();
                },
                null, null,
                !System.Diagnostics.Debugger.IsAttached, maxDegreeOfParallelism: threads,
                monitor: new MonitoredTaskRepo.ForBatch("Downloader ", "RecalculateTasks from queue ")
            );

            _logger.Information("Ends RecalculateTasks with {numOfThreads} threads", threads.Value);
        }

        public static async Task RecalculateOsobaAsync(RecalculateItem item, bool noRebuild, bool invalidateOnly)
        {
            var o = await OsobaCache.GetPersonByNameIdAsync(item.Id);
            if (o != null)
            {
                switch (item.StatisticsType)
                {
                    case RecalculateItem.StatisticsTypeEnum.Smlouva:
                        if (invalidateOnly)
                        {
                            await StatisticsCache.InvalidateOsobaSmlouvyStatisticsAsync(o, null);
                            await OsobaCache.InvalidateInfoFactsAsync(o);
                        }
                        else
                        {
                            _ = await o.StatistikaRegistrSmluvAsync(forceUpdateCache: noRebuild ? false : true);
                            _ = await o.InfoFactsCachedAsync(forceUpdateCache: noRebuild ? false : true);
                        }

                        RecalculateItemRepo.Finish(item);
                        break;
                    case RecalculateItem.StatisticsTypeEnum.Dotace:
                        if (invalidateOnly)
                        {
                            await StatisticsCache.InvalidateOsobaDotaceStatisticsAsync(o);
                            await OsobaCache.InvalidateInfoFactsAsync(o);
                        }
                        else
                        {
                            _ = await o.StatistikaDotaceAsync(forceUpdateCache: noRebuild ? false : true);
                            _ = await o.InfoFactsCachedAsync(forceUpdateCache: noRebuild ? false : true);
                        }

                        RecalculateItemRepo.Finish(item);
                        break;
                    default:
                        break;
                }
            }
        }

        public static async Task RecalculateFirmaAsync(RecalculateItem item, bool noRebuild, bool invalidateOnly,
            bool firmaOnly = false, bool holdingOnly = false)
        {
            var f = await Firmy.GetAsync(item.Id);
            if (f != null)
            {
                switch (item.StatisticsType)
                {
                    case RecalculateItem.StatisticsTypeEnum.Smlouva:
                        if (invalidateOnly)
                        {
                            await Statistics.FirmaStatistics.RemoveStatisticsAsync(f, null);
                        }
                        else
                        {
                            if (!holdingOnly || firmaOnly)
                                _ = await f.StatistikaRegistruSmluvAsync(forceUpdateCache: true);
                            if (holdingOnly || !firmaOnly)
                                _ = await f.HoldingStatisticsRegistrSmluvAsync(forceUpdateCache: noRebuild ? false : true);
                        }

                        break;
                    case RecalculateItem.StatisticsTypeEnum.VZ:
                        if (invalidateOnly)
                        {
                            await StatisticsCache.InvalidateHoldingVzStatisticsAsync(f);
                            await StatisticsCache.InvalidateFirmaVzStatisticsAsync(f);
                        }
                        else
                        {
                            if (!holdingOnly || firmaOnly)
                                _ = await f.StatistikaVerejneZakazkyAsync(forceUpdateCache: true);
                            if (holdingOnly || !firmaOnly)
                                _ = await f.HoldingStatistikaVerejneZakazkyAsync(forceUpdateCache: noRebuild ? false : true);
                        }

                        break;
                    case RecalculateItem.StatisticsTypeEnum.Dotace:
                        if (invalidateOnly)
                        {
                            await StatisticsCache.InvalidateHoldingDotaceStatisticsAsync(f);
                            await StatisticsCache.InvalidateFirmaDotaceStatisticsAsync(f);
                        }
                        else
                        {
                            if (!holdingOnly || firmaOnly)
                                _ = await f.StatistikaDotaciAsync(forceUpdateCache: true);
                            if (holdingOnly || !firmaOnly)
                                _ = await f.HoldingStatistikaDotaciAsync(forceUpdateCache: noRebuild ? false : true);
                        }

                        break;
                    default:
                        break;
                }

                if (invalidateOnly)
                    await FirmaCache.InvalidateInfoFactsAsync(f);
                else
                    await f.InfoFactsAsync(forceUpdateCache: noRebuild ? false : true);
                RecalculateItemRepo.Finish(item);
            }
        }

        public static async Task<List<RecalculateItem>> CascadeItemsAsync(RecalculateItem item,
            System.Collections.Concurrent.ConcurrentBag<RecalculateItem> alreadyOnList)
        {
            List<RecalculateItem> list = new List<RecalculateItem>(alreadyOnList);
            if (item.ItemType == RecalculateItem.ItemTypeEnum.Subjekt)
            {
                var f = await Firmy.GetAsync(item.Id);
                if (f?.Valid == true)
                    list = list.Union(
                            await FirmaForQueueAsync(new List<RecalculateItem>(), f, item.StatisticsType, item.ProvokedBy, 0),
                            comparer)
                        .ToList();
            }
            else if (item.ItemType == RecalculateItem.ItemTypeEnum.Person)
            {
                var o = await OsobaCache.GetPersonByIdAsync(Convert.ToInt32(item.Id));
                if (o != null)
                    list = list.Union(
                            await OsobaForQueueAsync(new List<RecalculateItem>(), o, item.StatisticsType, item.ProvokedBy, 0),
                            comparer)
                        .ToList();
            }
            else
                list = list.Union(new[] { item }, comparer).ToList();

            _logger.Debug("{method} expanding " + item.ItemType.ToString() + " {name} to {count} items",
                MethodBase.GetCurrentMethod().Name, item.Id, list.Count);

            return list;
        }


        public static bool AddOsobaToProcessingQueue(string nameId, RecalculateItem.StatisticsTypeEnum statsType,
            string provokeBy)
        {
            return AddToProcessingQueue(
                new RecalculateItem()
                {
                    Id = nameId,
                    ItemType = RecalculateItem.ItemTypeEnum.Person,
                    StatisticsType = statsType,
                    ProvokedBy = provokeBy
                });
        }

        public static bool AddToProcessingQueue(Osoba o, RecalculateItem.StatisticsTypeEnum statsType, string provokeBy)
        {
            return AddToProcessingQueue(new RecalculateItem(o, statsType, provokeBy));
        }

        public static bool AddFirmaToProcessingQueue(string ico, RecalculateItem.StatisticsTypeEnum statsType,
            string provokeBy)
        {
            if (string.IsNullOrWhiteSpace(ico))
                return true;
            return AddToProcessingQueue(
                new RecalculateItem()
                {
                    Id = ico,
                    ItemType = RecalculateItem.ItemTypeEnum.Subjekt,
                    StatisticsType = statsType,
                    ProvokedBy = provokeBy
                });
        }

        public static bool AddToProcessingQueue(Firma f, RecalculateItem.StatisticsTypeEnum statsType, string provokeBy)
        {
            return AddToProcessingQueue(new RecalculateItem(f, statsType, provokeBy));
        }

        private static async Task<List<RecalculateItem>> OsobaForQueueAsync(List<RecalculateItem> list,
            int osobaInternalId, RecalculateItem.StatisticsTypeEnum statsType, string provokeBy, int deep)
        {
            var o = await OsobaCache.GetPersonByIdAsync(osobaInternalId);
            if (o != null)
                return await OsobaForQueueAsync(list, o, statsType, provokeBy, deep);
            else
                return new List<RecalculateItem>();
        }

        private static async Task<List<RecalculateItem>> OsobaForQueueAsync(List<RecalculateItem> list,
            Osoba o, RecalculateItem.StatisticsTypeEnum statsType, string provokeBy, int deep)
        {
            _logger?.Verbose(
                "{method} expanding {osoba} {jmeno} vazby, recursive deep {deep}, current list {count} items",
                MethodBase.GetCurrentMethod().Name, o.NameId, o.FullName(), deep, list.Count);

            //StackOverflow defense
            if (list.Count > 10_000)
                return list;

            var it = new RecalculateItem(o, statsType, provokeBy);
            if (list.Contains(it, comparer) == false)
                list.Add(it);

            //StackOverflow defense
            if (deep > 50)
                return list;

            var parents = await o.ParentOsobyAsync(DS.Graphs.Relation.AktualnostType.Nedavny);
            foreach (var oo in parents)
            {
                var item = new RecalculateItem(oo, statsType, provokeBy);
                if (list.Contains(item, comparer) == false)
                    list.Add(item);
            }

            return list;
        }

        private static async Task<List<RecalculateItem>> FirmaForQueueAsync(List<RecalculateItem> list,
            Firma f, RecalculateItem.StatisticsTypeEnum statsType, string provokeBy, int deep)
        {
            _logger?.Verbose(
                "{method} expanding {ico} {subjekt} vazby, recursive deep {deep}, current list {count} items",
                MethodBase.GetCurrentMethod().Name, f.ICO, f.Jmeno, deep, list.Count);

            //StackOverflow defense
            if (list.Count > 10_000)
                return list;

            var it = new RecalculateItem(f, statsType, provokeBy);
            if (list.Contains(it, comparer) == false)
                list.Add(it);

            //StackOverflow defense
            if (deep > 50)
                return list;

            var parents = f.ParentFirmy(DS.Graphs.Relation.CharakterVazbyEnum.VlastnictviKontrola, DS.Graphs.Relation.AktualnostType.Nedavny)
                .UnionBy(f.ParentFirmy(DS.Graphs.Relation.CharakterVazbyEnum.Uredni, DS.Graphs.Relation.AktualnostType.Nedavny), m => m.ICO);

            foreach (var ff in parents)
            {
                var ff_it = new RecalculateItem(ff, statsType, provokeBy);
                if (list.Contains(ff_it, comparer) == false)
                    list = list.Union(await FirmaForQueueAsync(list, ff, statsType, provokeBy, deep + 1), comparer).ToList();
            }

            var os_parents = f.Osoby_v_OR(DS.Graphs.Relation.AktualnostType.Nedavny);
            foreach (var vaz in os_parents)
            {
                var item = new RecalculateItem(vaz.o, statsType, provokeBy);
                if (list.Contains(item, comparer) == false)
                {
                    list = list.Union(await OsobaForQueueAsync(list, vaz.o, statsType, provokeBy, deep + 1), comparer).ToList();
                }
            }

            return list;
        }

        public static bool Finish(RecalculateItem item)
        {
            return Finish(item.Pk);
        }

        public static bool Finish(long pk)
        {
            using (DbEntities db = new DbEntities())
            {
                var dbItem = db.RecalculateItem.FirstOrDefault(m => m.Pk == pk);
                if (dbItem == null) return false;

                dbItem.Finished = DateTime.Now;

                _ = db.SaveChanges();
                return true;
            }
        }

        public static bool AddToProcessingQueue(RecalculateItem item)
        {
            if (string.IsNullOrWhiteSpace(item.Id))
                return true;
            try
            {
                using (DbEntities db = new DbEntities())
                {
                    var strategy = db.Database.CreateExecutionStrategy();
                    bool res = strategy.Execute<bool>(() =>
                    {
                        using (var dbTran = db.Database.BeginTransaction(System.Data.IsolationLevel.RepeatableRead))
                        {
                            try
                            {
                                var exists = db.RecalculateItem.Any(m =>
                                    m.Id == item.Id
                                    && m.ItemType == item.ItemType
                                    && m.StatisticsType == item.StatisticsType
                                    && (m.Finished == null)
                                );
                                if (exists)
                                {
                                    dbTran.Rollback();
                                    return false;
                                }

                                _ = db.RecalculateItem.Add(item);
                                _ = db.SaveChanges();
                                dbTran.Commit();
                                return true;
                            }
                            catch (Exception e)
                            {
                                dbTran.Rollback();
                                throw;
                            }
                        }
                    });

                    return res;
                }
                /*using HlidacStatu.Q.Simple.Queue<RecalculateItem> q = new Q.Simple.Queue<RecalculateItem>(
                        RECALCULATIONQUEUENAME,
                        Devmasters.Config.GetWebConfigValue("RabbitMqConnectionString")
                        );
                q.Send(item);
                return true;*/
            }
            catch (Exception)
            {
                //TODO log
                return false;
            }
        }

        public static async Task DebugAsync()
        {
            var fakeList = new System.Collections.Concurrent.ConcurrentBag<RecalculateItem>();
            var item = new RecalculateItem(await Firmy.GetAsync("00000205"), RecalculateItem.StatisticsTypeEnum.VZ,
                "recalculateDebug");
            List<RecalculateItem> cascade = await CascadeItemsAsync(item, fakeList);
        }

        public static async Task<IEnumerable<RecalculateItem>> GetFromProcessingQueueWithParentsAsync(int count, int threads,
            Action<string> outputWriter = null, Devmasters.Batch.IProgressWriter  progressWriter = null,
            bool debug = false)
        {
            if (debug)
                _logger?.Debug($"getting {count} from processing queue");
            IEnumerable<RecalculateItem> res = GetItemsFromProcessingQueue(count);
            if (debug)
                _logger?.Debug($"got {res.Count()} from processing queue. Looking for parents");

            System.Collections.Concurrent.ConcurrentBag<RecalculateItem> list = new(res);

            await Devmasters.Batch.Manager.DoActionForAllAsync<RecalculateItem>(res, async item =>
                {
                    try
                    {
                        if (debug)
                            _logger?.Debug($"GetFromProcessingQueueWithParents getting cascade for {item.UniqueKey}");
                        List<RecalculateItem> cascade = await CascadeItemsAsync(item, list);
                        if (debug)
                            _logger?.Debug($"GetFromProcessingQueueWithParents got cascade for {item.UniqueKey}");
                        foreach (var i in cascade)
                        {
                            if (list.Contains(i, comparer) == false)
                                list.Add(i);
                        }
                    }
                    catch (Exception e)
                    {
                        _logger?.Error(e, "CascadeItems error");
                        throw;
                    }


                    return new Devmasters.Batch.ActionOutputData();
                },
                outputWriter, progressWriter,
                !System.Diagnostics.Debugger.IsAttached, maxDegreeOfParallelism: threads
            );

            _logger?.Debug("{method} gets {records} records containing parents and owners",
                MethodBase.GetCurrentMethod().Name, list.Count);

            return list.OrderBy(o => o.Created)
                .Distinct(comparer)
                .OrderBy(o => o.Created);
        }

        public static IEnumerable<RecalculateItem> GetItemsFromProcessingQueue(int count)
        {
            try
            {
                using (Entities.DbEntities db = new DbEntities())
                {
                    db.Database.SetCommandTimeout(120); //120 secs

                    var res = db.RecalculateItem
                        .AsNoTracking()
                        .Where(m => m.Started == null)
                        .OrderBy(o => o.Created)
                        .Take(count)
                        .ToArray();

                    if (res.Any())
                    {
                        var ids = string.Join(",", res.Select(m => m.Pk));
                        var sql = $"update recalculateItemQueue set started=GETUTCDATE() where pk in ({ids})";
                        db.Database.ExecuteSqlRaw(sql);
                        _logger.Debug("Setting recalculateItemQueue status {sql}", sql);
                    }

                    return res;
                }
            }

            catch
            {
                return Array.Empty<RecalculateItem>();
            }
        }
    }
}