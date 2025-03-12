using HlidacStatu.Connectors;
using HlidacStatu.Entities;
using HlidacStatu.Extensions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Serilog;
using Devmasters.Collections;
using HlidacStatu.Repositories.Statistics;

namespace HlidacStatu.Repositories
{
    public class RecalculateItemRepo
    {
        private static ILogger _logger = Log.ForContext<RecalculateItemRepo>();

        //public const string RECALCULATIONQUEUENAME = "recalculation2Process";
        static Entities.RecalculateItemEqComparer comparer = new Entities.RecalculateItemEqComparer();

        static Devmasters.Batch.ActionProgressWriter debugProgressWr = new Devmasters.Batch.ActionProgressWriter(0.1f);

        public static int RecalculateQueueLength()
        {
            return DirectDB.GetValue<int>("select count(*) from RecalculateItemQueue with (nolock)");
        }

        public static void RecalculateTasks(int? threads = null, bool debug = false, string[] ids = null,
            Action<string> outputWriter = null,Action<Devmasters.Batch.ActionProgressData> progressWriter = null,
            int? maxItemsInBatch = null, bool allItemsInDb = false,
            bool invalidateOnly = false
            )
        {
                Devmasters.Batch.Manager.DoActionForAll<int>(Enumerable.Range(0, int.MaxValue - 1),
                    xx =>
                    {
                        var items = RecalculateItemRepo.GetFromProcessingQueueWithParents(1, 1, debug: debug);
                        if (items.Count() == 0)
                            return new Devmasters.Batch.ActionOutputData() { CancelRunning = true };

                        Devmasters.Batch.Manager.DoActionForAll<RecalculateItem>(items,
                            item =>
                            {
                                if (debug)
                                    _logger.Debug($"start statistics {item.ItemType.ToString()} {item.Id}");
                                if (item.ItemType == RecalculateItem.ItemTypeEnum.Subjekt)
                                    RecalculateItemRepo.RecalculateFirma(item, false, invalidateOnly);
                                else if (item.ItemType == RecalculateItem.ItemTypeEnum.Person)
                                    RecalculateItemRepo.RecalculateOsoba(item, false, invalidateOnly);
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

        public static void RecalculateOsoba(RecalculateItem item, bool noRebuild, bool invalidateOnly)
        {
            var o = Osoby.GetByNameId.Get(item.Id);
            if (o != null)
            {
                switch (item.StatisticsType)
                {
                    case RecalculateItem.StatisticsTypeEnum.Smlouva:
                        if (invalidateOnly)
                        {
                            OsobaStatistics.RemoveCachedStatistics_Smlouvy(o, null);
                            o.InfoFactsCacheInvalidate();
                        }
                        else
                        {

                            _ = o.StatistikaRegistrSmluv(DS.Graphs.Relation.AktualnostType.Nedavny, forceUpdateCache: noRebuild ? false : true);
                            _ = o.InfoFactsCached(forceUpdateCache: noRebuild ? false : true);
                        }
                        RecalculateItemRepo.Finish(item);
                        break;
                    case RecalculateItem.StatisticsTypeEnum.Dotace:
                        if (invalidateOnly)
                        {
                            OsobaStatistics.RemoveCachedStatistics_Dotace(o);
                            o.InfoFactsCacheInvalidate();
                        }
                        else
                        {

                            _ = o.StatistikaDotace(DS.Graphs.Relation.AktualnostType.Nedavny, forceUpdateCache: noRebuild ? false : true);
                            _ = o.InfoFactsCached(forceUpdateCache: noRebuild ? false : true);
                        }
                        RecalculateItemRepo.Finish(item);
                        break;
                    default:
                        break;
                }

            }
        }

        public static void RecalculateFirma(RecalculateItem item, bool noRebuild, bool invalidateOnly, bool firmaOnly = false, bool holdingOnly = false)
        {
            var f = Firmy.Get(item.Id);
            if (f != null)
            {
                switch (item.StatisticsType)
                {
                    case RecalculateItem.StatisticsTypeEnum.Smlouva:
                        if (invalidateOnly)
                        {
                            Statistics.FirmaStatistics.RemoveStatistics(f, null);
                        }
                        else
                        {
                            if (!holdingOnly || firmaOnly)
                                _ = f.StatistikaRegistruSmluv(forceUpdateCache: true);
                            if (holdingOnly || !firmaOnly)
                                _ = f.HoldingStatisticsRegistrSmluv(DS.Graphs.Relation.AktualnostType.Nedavny,
                                forceUpdateCache: noRebuild ? false : true);
                        }
                        break;
                    case RecalculateItem.StatisticsTypeEnum.VZ:
                        if (invalidateOnly)
                        {
                            Statistics.FirmaStatistics.RemoveStatisticsVZ(f);
                        }
                        else
                        {
                            if (!holdingOnly || firmaOnly)
                                _ = f.StatistikaVerejneZakazky(forceUpdateCache: true);
                            if (holdingOnly || !firmaOnly)
                                _ = f.HoldingStatistikaVerejneZakazky(DS.Graphs.Relation.AktualnostType.Nedavny,
                                forceUpdateCache: noRebuild ? false : true);
                        }
                        break;
                    case RecalculateItem.StatisticsTypeEnum.Dotace:
                        if (invalidateOnly)
                        {
                            Statistics.FirmaStatistics.RemoveStatisticsDotace(f);
                        }
                        else
                        {
                            if (!holdingOnly || firmaOnly)
                                _ = f.StatistikaDotaci(forceUpdateCache: true);
                            if (holdingOnly || !firmaOnly)
                                _ = f.HoldingStatistikaDotaci(DS.Graphs.Relation.AktualnostType.Nedavny,
                                forceUpdateCache: noRebuild ? false : true);
                        }
                        break;
                    default:
                        break;
                }
                if (invalidateOnly)
                    f.InfoFactsInvalidate();
                else
                    _ = f.InfoFacts(forceUpdateCache: noRebuild ? false : true);
                RecalculateItemRepo.Finish(item);
            }
        }

        public static List<RecalculateItem> CascadeItems(RecalculateItem item,
            ref System.Collections.Concurrent.ConcurrentBag<RecalculateItem> alreadyOnList)
        {

            List<RecalculateItem> list = new List<RecalculateItem>(alreadyOnList);
            if (item.ItemType == RecalculateItem.ItemTypeEnum.Subjekt)
            {
                var f = Firmy.Get(item.Id);
                if (f?.Valid == true)
                    list = list.Union(FirmaForQueue(new List<RecalculateItem>(), f, item.StatisticsType, item.ProvokedBy, 0), comparer)
                        .ToList();
            }
            else if (item.ItemType == RecalculateItem.ItemTypeEnum.Person)
            {
                var o = Osoby.GetById.Get(Convert.ToInt32(item.Id));
                if (o != null)
                    list = list.Union(OsobaForQueue(new List<RecalculateItem>(), o, item.StatisticsType, item.ProvokedBy, 0), comparer)
                        .ToList();
            }
            else
                list = list.Union(new[] { item }, comparer).ToList();

            _logger.Debug("{method} expanding " + item.ItemType.ToString() + " {name} to {count} items",
                MethodBase.GetCurrentMethod().Name, item.Id, list.Count);

            return list;
        }


        public static bool AddOsobaToProcessingQueue(string nameId, RecalculateItem.StatisticsTypeEnum statsType, string provokeBy)
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

        public static bool AddFirmaToProcessingQueue(string ico, RecalculateItem.StatisticsTypeEnum statsType, string provokeBy)
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

        private static List<RecalculateItem> OsobaForQueue(List<RecalculateItem> list,
            int osobaInternalId, RecalculateItem.StatisticsTypeEnum statsType, string provokeBy, int deep)
        {
            var o = Osoby.GetById.Get(osobaInternalId);
            if (o != null)
                return OsobaForQueue(list, o, statsType, provokeBy, deep);
            else
                return new List<RecalculateItem>();
        }
        private static List<RecalculateItem> OsobaForQueue(List<RecalculateItem> list,
            Osoba o, RecalculateItem.StatisticsTypeEnum statsType, string provokeBy, int deep)
        {
            _logger?.Verbose("{method} expanding {osoba} {jmeno} vazby, recursive deep {deep}, current list {count} items",
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

            var parents = o.ParentOsoby(DS.Graphs.Relation.AktualnostType.Nedavny);
            foreach (var oo in parents)
            {
                var item = new RecalculateItem(oo, statsType, provokeBy);
                if (list.Contains(item, comparer) == false)
                    list.Add(item);
            }

            return list;
        }

        private static List<RecalculateItem> FirmaForQueue(List<RecalculateItem> list,
            Firma f, RecalculateItem.StatisticsTypeEnum statsType, string provokeBy, int deep)
        {
            _logger?.Verbose("{method} expanding {ico} {subjekt} vazby, recursive deep {deep}, current list {count} items",
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

            var parents = f.ParentFirmy(DS.Graphs.Relation.AktualnostType.Nedavny);
            foreach (var ff in parents)
            {
                var ff_it = new RecalculateItem(ff, statsType, provokeBy);
                if (list.Contains(ff_it, comparer) == false)
                    list = list.Union(FirmaForQueue(list, ff, statsType, provokeBy, deep + 1), comparer).ToList();
            }

            var os_parents = f.Osoby_v_OR(DS.Graphs.Relation.AktualnostType.Nedavny);
            foreach (var vaz in os_parents)
            {
                var item = new RecalculateItem(vaz.o, statsType, provokeBy);
                if (list.Contains(item, comparer) == false)
                {
                    list = list.Union(OsobaForQueue(list, vaz.o, statsType, provokeBy, deep + 1), comparer).ToList();
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

        public static void Debug()
        {
            var fakeList = new System.Collections.Concurrent.ConcurrentBag<RecalculateItem>();
            var item = new RecalculateItem(Firmy.Get("00000205"), RecalculateItem.StatisticsTypeEnum.VZ, "recalculateDebug");
            List<RecalculateItem> cascade = CascadeItems(item, ref fakeList);

        }

        public static IEnumerable<RecalculateItem> GetFromProcessingQueueWithParents(int count, int threads,
            Action<string> outputWriter = null, Action<Devmasters.Batch.ActionProgressData> progressWriter = null,
            bool debug = false)
        {
            if (debug)
                _logger?.Debug($"getting {count} from processing queue");
            IEnumerable<RecalculateItem> res = GetItemsFromProcessingQueue(count);
            if (debug)
                _logger?.Debug($"got {res.Count()} from processing queue. Looking for parents");

            System.Collections.Concurrent.ConcurrentBag<RecalculateItem> list = new(res);

            Devmasters.Batch.Manager.DoActionForAll<RecalculateItem>(res,
                item =>
                {
                    try
                    {

                        if (debug)
                            _logger?.Debug($"GetFromProcessingQueueWithParents getting cascade for {item.UniqueKey}");
                        List<RecalculateItem> cascade = CascadeItems(item, ref list);
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

            _logger?.Debug("{method} gets {records} records containing parents and owners", MethodBase.GetCurrentMethod().Name, list.Count);

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
