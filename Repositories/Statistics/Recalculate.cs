using HlidacStatu.Entities;
using HlidacStatu.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HlidacStatu.Repositories.Statistics
{
    public class Recalculate
    {
        private const string RECALCULATIONQUEUENAME = "recalculation2Process";
        static RecalculateItemEqComparer comparer = new RecalculateItemEqComparer();


        public static void RecalculateTasks()
        {
            var items = GetFromProcessingQueue(100000);
            while (items.Count() > 0)
            {

                foreach (var item in items)
                {
                    System.Diagnostics.Debug.WriteLine($"{item.ItemType}.{item.Id} {item.StatisticsType} starting");
                    Devmasters.DT.StopWatchEx sw = new Devmasters.DT.StopWatchEx();
                    sw.Start();
                    RecalculateTask(item);
                    sw.Stop();
                    System.Diagnostics.Debug.WriteLine($"{item.ItemType}.{item.Id} {item.StatisticsType} elapsed in {sw.ElapsedMilliseconds} ms");
                }
                items = GetFromProcessingQueue(100000);
            }

        }
        public static void RecalculateTask(RecalculateItem item)
        {
            switch (item.ItemType)
            {
                case RecalculateItem.ItemTypeEnum.Subjekt:
                    var f = Firmy.Get(item.Id);
                    if (f != null)
                        switch (item.StatisticsType)
                        {
                            case RecalculateItem.StatisticsTypeEnum.Smlouva:
                                _ = f.StatistikaRegistruSmluv(forceUpdateCache: true);
                                _ = f.HoldingStatisticsRegistrSmluv(DS.Graphs.Relation.AktualnostType.Nedavny, forceUpdateCache: true);
                                break;
                            case RecalculateItem.StatisticsTypeEnum.VZ:
                                _ = f.StatistikaVerejneZakazky(forceUpdateCache: true);
                                _ = f.HoldingStatistikaVerejneZakazky(DS.Graphs.Relation.AktualnostType.Nedavny, forceUpdateCache: true);
                                break;
                            case RecalculateItem.StatisticsTypeEnum.Dotace:
                                _ = f.StatistikaDotaci(forceUpdateCache: true);
                                _ = f.HoldingStatistikaDotaci(DS.Graphs.Relation.AktualnostType.Nedavny, forceUpdateCache: true);

                                break;
                            default:
                                break;
                        }
                    break;
                case RecalculateItem.ItemTypeEnum.Person:
                    var o = Osoby.GetByNameId.Get(item.Id);
                    if (o != null)
                        _ = o.StatistikaRegistrSmluv(DS.Graphs.Relation.AktualnostType.Nedavny, forceUpdateCache: true);
                    break;
                default:
                    break;
            }
        }

        public static void AddOsobaToProcessingQueue(string nameId, RecalculateItem.StatisticsTypeEnum statsType, string provokeBy)
        {
            var o = Osoby.GetByNameId.Get(nameId);
            if (o != null)
                AddToProcessingQueue(o, statsType, provokeBy);
        }
        public static void AddToProcessingQueue(Osoba o, RecalculateItem.StatisticsTypeEnum statsType, string provokeBy)
        {
            List<RecalculateItem> list = new List<RecalculateItem>();
            list.Add(new RecalculateItem(o, statsType, provokeBy));

        }

        public static void AddFirmaToProcessingQueue(string ico, RecalculateItem.StatisticsTypeEnum statsType, string provokeBy)
        {
            var f = Firmy.Get(ico);
            if (f != null)
                AddToProcessingQueue(f, statsType, provokeBy);
        }
        public static void AddToProcessingQueue(Firma f, RecalculateItem.StatisticsTypeEnum statsType, string provokeBy)
        {
            List<RecalculateItem> list = new List<RecalculateItem>();

            list = FirmaForQueue(new List<RecalculateItem>(), f, statsType, provokeBy, 0);

            foreach (var item in list)
                _ = AddToProcessingQueue(item);
        }

        private static List<RecalculateItem> FirmaForQueue(List<RecalculateItem> list,
            Firma f, RecalculateItem.StatisticsTypeEnum statsType, string provokeBy, int deep)
        {

            System.Diagnostics.Debug.WriteLine($"{f.Jmeno} /{deep}/ {list.Count} count");

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

        public static IEnumerable<RecalculateItem> GetFromProcessingQueue(int count)
        {
            System.Collections.Concurrent.ConcurrentQueue<RecalculateItem> res = new();
            Devmasters.Batch.Manager.DoActionForAll<int>(Enumerable.Range(0, count),
                _ =>
                {
                    var item = GetFromProcessingQueue();
                    if (item == null)
                        return new Devmasters.Batch.ActionOutputData() { CancelRunning = true };

                    res.Enqueue(item);

                    return new Devmasters.Batch.ActionOutputData() { CancelRunning = res.Count >= count };
                },
                true,maxDegreeOfParallelism:10
            );
            return res.OrderBy(o=>o.Created)
                .Distinct()
                .OrderBy(o => o.Created);
        }
        public static RecalculateItem GetFromProcessingQueue()
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
