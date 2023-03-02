﻿using Amazon.Runtime.Internal.Util;
using HlidacStatu.Entities;
using HlidacStatu.Extensions;
using System;
using System.Collections.Generic;
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

        public static void RecalculateTasks(int? threads = null,
            Action<string> outputWriter = null, 
            Action<Devmasters.Batch.ActionProgressData> progressWriter = null
            )
        {
            threads = threads ?? 20;

            log.Info("{method} Starting with {numOfThreads} threads", MethodBase.GetCurrentMethod().Name, threads.Value);
            var queueItems = GetFromProcessingQueue(threads.Value* threads.Value, threads.Value);

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
                            _ = o.StatistikaRegistrSmluv(DS.Graphs.Relation.AktualnostType.Nedavny, forceUpdateCache: true);
                            _ = o.InfoFactsCached(forceUpdateCache: true);
                        }

                        return new Devmasters.Batch.ActionOutputData();
                    },
                    outputWriter, progressWriter,
                    !System.Diagnostics.Debugger.IsAttached, maxDegreeOfParallelism: threads,
                    monitor: new MonitoredTaskRepo.ForBatch()
                    );


                queueItems = GetFromProcessingQueue(100000, threads.Value);
            }

            log.Info("Ends RecalculateTasks with {numOfThreads} threads", threads.Value);

        }

        private static List<RecalculateItem> CascadeItems(RecalculateItem item)
        {
            List<RecalculateItem> list = new List<RecalculateItem>();
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


            var it = new RecalculateItem(f, statsType, provokeBy);
            if (list.Contains(it, comparer) == false)
                list.Add(it);

            //StackOverflow defense
            if (deep > 50)
                return list;


            foreach (var ff in f.ParentFirmy(DS.Graphs.Relation.AktualnostType.Nedavny))
            {
                FirmaForQueue(list, ff, statsType, provokeBy, deep + 1);
            }

            foreach (var vaz in f.Osoby_v_OR(DS.Graphs.Relation.AktualnostType.Nedavny))
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

        public static IEnumerable<RecalculateItem> GetFromProcessingQueue(int count, int threads,
            Action<string> outputWriter = null, Action<Devmasters.Batch.ActionProgressData> progressWriter = null)
        {
            log.Debug("{method} Starting for {records} records with {numOfThreads} threads", MethodBase.GetCurrentMethod().Name, count, threads);

            System.Collections.Concurrent.ConcurrentQueue<RecalculateItem> res = new();
            Devmasters.Batch.Manager.DoActionForAll<int>(Enumerable.Range(0, count),
                _ =>
                {
                    var item = GetOneFromProcessingQueue();
                    if (item == null)
                        return new Devmasters.Batch.ActionOutputData() { CancelRunning = true };

                    res.Enqueue(item);

                    return new Devmasters.Batch.ActionOutputData() { CancelRunning = res.Count >= count };
                },
                outputWriter, progressWriter,
                !System.Diagnostics.Debugger.IsAttached, maxDegreeOfParallelism: Math.Min(threads, 10), monitor: new MonitoredTaskRepo.ForBatch()
            );

            log.Debug("{method} get {records} records from ProcessingQueue", MethodBase.GetCurrentMethod().Name, count);

            System.Collections.Concurrent.ConcurrentBag<RecalculateItem> list = new();

            Devmasters.Batch.Manager.DoActionForAll<RecalculateItem>(res,
                item =>
                {
                    List<RecalculateItem> cascade = CascadeItems(item);

                    foreach (var i in cascade)
                        list.Add(i);

                    return new Devmasters.Batch.ActionOutputData();
                },
                outputWriter, progressWriter,
                !System.Diagnostics.Debugger.IsAttached, maxDegreeOfParallelism: threads,
                monitor: new MonitoredTaskRepo.ForBatch(logger:log)
                );

            log.Debug("{method} gets {records} records containing parents and owners", MethodBase.GetCurrentMethod().Name, list.Count);

            return list.OrderBy(o => o.Created)
                .Distinct()
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