using Amazon.Runtime.Internal.Util;
using HlidacStatu.Entities;
using HlidacStatu.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace HlidacStatu.Repositories.Statistics
{
    public class Recalculate
    {
        static Devmasters.Log.Logger log = Devmasters.Log.Logger.CreateLogger<Recalculate>();

        private const string RECALCULATIONQUEUENAME = "recalculation2Process";
        static RecalculateItemEqComparer comparer = new RecalculateItemEqComparer();

        static Devmasters.Batch.ActionProgressWriter debugProgressWr = new Devmasters.Batch.ActionProgressWriter(0.1f);

        public static void RecalculateTasks(int? threads = null, bool debug=false,
            Action<string> outputWriter = null, 
            Action<Devmasters.Batch.ActionProgressData> progressWriter = null
            )
        {
            threads = threads ?? 20;

            int numFromQueue = threads.Value * threads.Value;

            using (HlidacStatu.Q.Simple.Queue<RecalculateItem> q = new Q.Simple.Queue<RecalculateItem>(
    RECALCULATIONQUEUENAME,
    Devmasters.Config.GetWebConfigValue("RabbitMqConnectionString")
    ))
            {
                var numInQ = q.MessageCount();
                if (numInQ > numFromQueue * 2)
                    numFromQueue = (int)(numInQ / 2);
            }



            log.Info("{method} Starting with {numOfThreads} threads", MethodBase.GetCurrentMethod().Name, threads.Value);
            if (debug)
                Console.WriteLine($"getting from queue {numFromQueue} items");


            var queueItems = GetFromProcessingQueue(numFromQueue, threads.Value, outputWriter, progressWriter, debug);
            if (debug)
                Console.WriteLine($"got from queue {queueItems.Count()} items");

            while (queueItems.Count() > 0)
            {
                log.Info("{method} Starting Subjekt statistics recalculate for {count} subjects with {numOfThreads} threads",
                    queueItems.Count(m => m.ItemType == RecalculateItem.ItemTypeEnum.Subjekt),MethodBase.GetCurrentMethod().Name, threads.Value);
                // rebuild cache for subjekt
                Devmasters.Batch.Manager.DoActionForAll<RecalculateItem>(queueItems.Where(m => m.ItemType == RecalculateItem.ItemTypeEnum.Subjekt),
                    item =>
                    {
                        var f = Firmy.Get(item.Id);
                        if (f != null)
                        {
                            if (debug)
                                Console.WriteLine($"start statistics {f.ICO} {f.Jmeno}");
                            switch (item.StatisticsType)
                            {
                                case RecalculateItem.StatisticsTypeEnum.Smlouva:
                                    _ = f.StatistikaRegistruSmluv(forceUpdateCache: true);
                                    break;
                                case RecalculateItem.StatisticsTypeEnum.VZ:
                                    _ = f.StatistikaVerejneZakazky(forceUpdateCache: true);
                                    break;
                                case RecalculateItem.StatisticsTypeEnum.Dotace:
                                    _ = f.StatistikaDotaci(forceUpdateCache: true);
                                    break;
                                default:
                                    break;
                            }
                            if (debug)
                                Console.WriteLine($"end statistics {f.ICO} {f.Jmeno}");
                        }
                        return new Devmasters.Batch.ActionOutputData();
                    },
                    outputWriter, progressWriter,
                    !System.Diagnostics.Debugger.IsAttached, maxDegreeOfParallelism: threads,
                    monitor: new MonitoredTaskRepo.ForBatch()
                    );

                log.Info("{method} Starting Subjekt-Holding statistics recalculate for {count} subjects with {numOfThreads} threads",
                    queueItems.Count(m => m.ItemType == RecalculateItem.ItemTypeEnum.Subjekt), MethodBase.GetCurrentMethod().Name, threads.Value);

                // rebuild cache for subjekt holding
                Devmasters.Batch.Manager.DoActionForAll<RecalculateItem>(queueItems.Where(m => m.ItemType == RecalculateItem.ItemTypeEnum.Subjekt),
                    item =>
                    {
                        var f = Firmy.Get(item.Id);
                        if (f != null)
                        {
                            if (debug)
                                Console.WriteLine($"start holding statistics {f.ICO} {f.Jmeno}");
                            switch (item.StatisticsType)
                            {
                                case RecalculateItem.StatisticsTypeEnum.Smlouva:
                                    _ = f.HoldingStatisticsRegistrSmluv(DS.Graphs.Relation.AktualnostType.Nedavny, forceUpdateCache: true);
                                    break;
                                case RecalculateItem.StatisticsTypeEnum.VZ:
                                    _ = f.HoldingStatistikaVerejneZakazky(DS.Graphs.Relation.AktualnostType.Nedavny, forceUpdateCache: true);
                                    break;
                                case RecalculateItem.StatisticsTypeEnum.Dotace:
                                    _ = f.HoldingStatistikaDotaci(DS.Graphs.Relation.AktualnostType.Nedavny, forceUpdateCache: true);
                                    break;
                                default:
                                    break;
                            }
                            _ = f.InfoFacts(forceUpdateCache: true);

                            if (debug)
                                Console.WriteLine($"end holding statistics {f.ICO} {f.Jmeno}");

                        }
                        return new Devmasters.Batch.ActionOutputData();
                    },
                    outputWriter, progressWriter,
                    !System.Diagnostics.Debugger.IsAttached, maxDegreeOfParallelism: threads,
                    monitor: new MonitoredTaskRepo.ForBatch()
                    );

                log.Info("{method} Starting Osoba statistics recalculate for {count} subjects with {numOfThreads} threads",
                    queueItems.Count(m => m.ItemType == RecalculateItem.ItemTypeEnum.Person), MethodBase.GetCurrentMethod().Name, threads.Value);
                // rebuild cache for person 
                Devmasters.Batch.Manager.DoActionForAll<RecalculateItem>(
                    queueItems.Where(m => m.ItemType == RecalculateItem.ItemTypeEnum.Person),
                    item =>
                    {
                        var o = Osoby.GetByNameId.Get(item.Id);
                        if (o != null)
                        {
                            if (debug)
                                Console.WriteLine($"start statistics osoba {o.NameId}");
                            _ = o.StatistikaRegistrSmluv(DS.Graphs.Relation.AktualnostType.Nedavny, forceUpdateCache: true);
                            _ = o.InfoFactsCached(forceUpdateCache: true);
                            if (debug)
                                Console.WriteLine($"end statistics osoba {o.NameId}");
                        }

                        return new Devmasters.Batch.ActionOutputData();
                    },
                    outputWriter, progressWriter,
                    !System.Diagnostics.Debugger.IsAttached, maxDegreeOfParallelism: threads,
                    monitor: new MonitoredTaskRepo.ForBatch()
                    );


                if (debug)
                    Console.WriteLine($"getting from queue {numFromQueue} items");
                queueItems = GetFromProcessingQueue(numFromQueue, threads.Value,outputWriter,progressWriter,debug);
                if (debug)
                    Console.WriteLine($"got from queue {queueItems.Count()} items");
            }

            log.Info("Ends RecalculateTasks with {numOfThreads} threads", threads.Value);

        }

        private static List<RecalculateItem> CascadeItems(RecalculateItem item, ref System.Collections.Concurrent.ConcurrentBag<RecalculateItem> alreadyOnList)
        {
            List<RecalculateItem> list = new List<RecalculateItem>(alreadyOnList);
            if (item.ItemType == RecalculateItem.ItemTypeEnum.Subjekt)
            {
                var f = Firmy.Get(item.Id);
                if (f?.Valid == true)
                    list.AddRange(FirmaForQueue(new List<RecalculateItem>(), f, item.StatisticsType, item.ProvokedBy, 0));
            }
            else
                list.Add(item);

            log.Debug("{method} expanding "+ item.ItemType.ToString() + " {name} to {count} items",
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

        private static List<RecalculateItem> FirmaForQueue(List<RecalculateItem> list,
            Firma f, RecalculateItem.StatisticsTypeEnum statsType, string provokeBy, int deep)
        {
            log.Verbose("{method} expanding {ico} {subjekt} vazby, recursive deep {deep}, current list {count} items",
                    MethodBase.GetCurrentMethod().Name,f.ICO, f.Jmeno, deep, list.Count);

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
                var ff_it = new RecalculateItem(ff, statsType,provokeBy);
                if (list.Contains(ff_it, comparer) == false)
                    FirmaForQueue(list, ff, statsType, provokeBy, deep + 1);
            }

            var os_parents = f.Osoby_v_OR(DS.Graphs.Relation.AktualnostType.Nedavny);
            foreach (var vaz in os_parents)
            {
                var item = new RecalculateItem(vaz.o, statsType, provokeBy);
                if (list.Contains(item, comparer) == false)
                    list.Add(item);
            }
            return list;
        }

        public static bool AddToProcessingQueue(RecalculateItem item)
        {
            try
            {

                using HlidacStatu.Q.Simple.Queue<RecalculateItem> q = new Q.Simple.Queue<RecalculateItem>(
                    RECALCULATIONQUEUENAME,
                    Devmasters.Config.GetWebConfigValue("RabbitMqConnectionString")
                    );
                q.Send(item);
                return true;
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
            var item = new RecalculateItem(Firmy.Get("00000205"), RecalculateItem.StatisticsTypeEnum.VZ,"recalculateDebug");
            List<RecalculateItem> cascade = CascadeItems(item, ref fakeList);

        }

        public static IEnumerable<RecalculateItem> GetFromProcessingQueue(int count, int threads,
            Action<string> outputWriter = null, Action<Devmasters.Batch.ActionProgressData> progressWriter = null, bool debug=false)
        {
            log.Debug("{method} Starting for {records} records with {numOfThreads} threads", MethodBase.GetCurrentMethod().Name, count, threads);
            if (debug)
                Console.WriteLine($"GetFromProcessingQueue getting from queue {count} items");

            System.Collections.Concurrent.ConcurrentQueue<RecalculateItem> res = new();
            Devmasters.Batch.Manager.DoActionForAll<int>(Enumerable.Range(0, count),
                _ =>
                {
                    try
                    {
                        var item = GetOneFromProcessingQueue();
                        if (item == null)
                            return new Devmasters.Batch.ActionOutputData() { CancelRunning = true };
                        if (res.Contains(item, comparer) == false)
                            res.Enqueue(item);

                    }
                    catch (Exception e)
                    {

                        log.Error("GetOneFromProcessingQueue error", e);
                    }

                    return new Devmasters.Batch.ActionOutputData() { CancelRunning = res.Count >= count };
                },
                outputWriter, progressWriter,
                !System.Diagnostics.Debugger.IsAttached, maxDegreeOfParallelism: Math.Min(threads, 10), monitor: new MonitoredTaskRepo.ForBatch()
            );

            log.Debug("{method} get {records} records from ProcessingQueue", MethodBase.GetCurrentMethod().Name, count);
            if (debug)
                Console.WriteLine($"GetFromProcessingQueue got from queue {count} items");

            System.Collections.Concurrent.ConcurrentBag<RecalculateItem> list = new(res);

            Devmasters.Batch.Manager.DoActionForAll<RecalculateItem>(res,
                item =>
                {
                    try
                    {

                    if (debug)
                        Console.WriteLine($"GetFromProcessingQueue getting cascade for {item.UniqueKey}");
                    List<RecalculateItem> cascade = CascadeItems(item, ref list);
                    if (debug)
                        Console.WriteLine($"GetFromProcessingQueue got cascade for {item.UniqueKey}");
                        foreach (var i in cascade)
                        {
                            if (list.Contains(i, comparer) == false)
                                list.Add(i);
                        }
                    }
                    catch (Exception e)
                    {
                        log.Error("CascadeItems error", e);
                        throw;
                    }


                    return new Devmasters.Batch.ActionOutputData();
                },
                outputWriter, progressWriter,
                !System.Diagnostics.Debugger.IsAttached, maxDegreeOfParallelism: threads,
                monitor: new MonitoredTaskRepo.ForBatch(logger:log)
                );

            log.Debug("{method} gets {records} records containing parents and owners", MethodBase.GetCurrentMethod().Name, list.Count);

            return list.OrderBy(o => o.Created)
                .Distinct(comparer`)
                .OrderBy(o => o.Created);
        }
        public static RecalculateItem GetOneFromProcessingQueue()
        {
            int tries = 3;
        start:
            try
            {
                tries--;
                using (HlidacStatu.Q.Simple.Queue<RecalculateItem> q = new Q.Simple.Queue<RecalculateItem>(
                    RECALCULATIONQUEUENAME,
                    Devmasters.Config.GetWebConfigValue("RabbitMqConnectionString")
                    ))
                {
                    var sq = q.GetAndAck();
                    return sq;
                }
            }
            catch (Exception e)
            {
                if (tries >= 0)
                {
                    System.Threading.Thread.Sleep(200);
                    goto start;
                }
                //TODO log
                return null;

            }
        }

    }
}
