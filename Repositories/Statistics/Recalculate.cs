using HlidacStatu.Entities;
using HlidacStatu.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HlidacStatu.Repositories.Statistics
{
    public class Recalculate
    {
        private const string RECALCULATIONQUEUENAME = "recalculation2Process";
        static RecalculateItemEqComparer comparer = new RecalculateItemEqComparer();

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

            list = FirmaForQueue(new List<RecalculateItem>(), f,statsType,provokeBy,0);

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
                FirmaForQueue(list,ff,statsType,provokeBy,deep+1);
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
            List<RecalculateItem> res = new List<RecalculateItem>();
            while (res.Count < count)
            {
                var item = GetFromProcessingQueue();
                if (item == null)
                    break;
                if (res.Contains(item, comparer))
                {
                    var old = res.FirstOrDefault(m => comparer.Equals(item, m));
                    if (old != null && old.Created < item.Created) //for sure
                    {
                        _ = res.Remove(old);
                        res.Add(item);
                    }
                }
                else
                    res.Add(item);
            }
            return res.Distinct();
        }
        public static RecalculateItem GetFromProcessingQueue()
        {
            try
            {
                using HlidacStatu.Q.Simple.Queue<RecalculateItem> q = new Q.Simple.Queue<RecalculateItem>(
                    RECALCULATIONQUEUENAME,
                    Devmasters.Config.GetWebConfigValue("RabbitMqConnectionString")
                    );
                var sq = q.GetAndAck();
                return sq;

            }
            catch (Exception)
            {
                //TODO log
                return null;

            }
        }

    }
}
