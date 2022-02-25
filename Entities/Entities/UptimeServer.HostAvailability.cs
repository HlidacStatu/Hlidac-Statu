using System;
using System.Collections.Generic;
using System.Linq;

namespace HlidacStatu.Entities
{
    public partial class UptimeServer
    {


        public class HostAvailability
        {
            private class IgnoreMissingData
            {
                public int hostid { get; set; } = 0;
                public DateTime from { get; set; }
                public DateTime to { get; set; }
                public string info { get; set; }
            }

            //static DateTime fixPolicieFrom = new DateTime(2017, 12, 18, 19, 05, 00);
            //static DateTime fixPolicieTo = new DateTime(2017, 12, 19, 11, 10, 00);
            static List<IgnoreMissingData> ignoreIt = new List<IgnoreMissingData>();
            //{
            //new IgnoreMissingData(){ from = new DateTime(2018, 3, 18, 0, 17, 00, DateTimeKind.Local), to = new DateTime(2018, 3, 18, 00, 23, 00, DateTimeKind.Local), info="Zabbix restart"},
            //new IgnoreMissingData(){ from = new DateTime(2018, 3, 21, 10, 51, 00, DateTimeKind.Local), to = new DateTime(2018, 3, 21, 11, 04, 00, DateTimeKind.Local), info="Zabbix restart"},
            //new IgnoreMissingData(){ from = new DateTime(2018, 12, 16, 13, 35, 00, DateTimeKind.Local), to = new DateTime(2018, 12, 16, 15, 14, 00, DateTimeKind.Local), info="Zabbix restart"},
            //};

            static object zabHostAvailabilityStaticLock = new object();
            static bool inicialized = false;
            static HostAvailability()
            {
                lock (zabHostAvailabilityStaticLock)
                {
                    if (inicialized == false)
                    {
                        var sdata = Devmasters.Config.GetWebConfigValue("ZabbixIgnoreDataIntervals") ?? "";
                        //~2019-08-11T20:10:00~2019-08-11T23:53:00~Zabbix reinstall | hostid~2019-08-11T23:58:00~2019-08-11T23:53:00~Zabbix reinstall |2019-08-11T23:53:00~2019-08-12T10:59:00 enable IP6 
                        try
                        {
                            foreach (var sd in sdata.Split('|'))
                            {
                                var parts = sd.Split(new char[] { '~' }, StringSplitOptions.None).Select(m => m.Trim()).ToArray();
                                string hostid = parts[0].Trim();
                                var from = Devmasters.DT.Util.ToDateTime(parts[1], "yyyy-MM-ddTHH:mm:ss");
                                var to = Devmasters.DT.Util.ToDateTime(parts[2], "yyyy-MM-ddTHH:mm:ss");
                                var descr = parts[3];

                                if (from.HasValue && to.HasValue)
                                    ignoreIt.Add(new IgnoreMissingData()
                                    {
                                        from = from.Value,
                                        to = to.Value,
                                        hostid = Convert.ToInt32(hostid),
                                        info = descr
                                    }
                                        );
                            }

                        }
                        catch (Exception e)
                        {
                            Util.Consts.Logger.Error("zabbix ZabbixIgnoreDataIntervals config error", e);
                        }
                        finally
                        {
                            inicialized = true;
                        }
                    }
                }
            }

            public static bool SkipThisTime(int hostid, DateTime time)
            {
                foreach (var ign in ignoreIt)
                {
                    if (
                        (ign.hostid == hostid || ign.hostid==0)
                        && (ign.from < time && time < ign.to)
                        )

                    {
                        return true;
                    }
                }
                return false;
            }

            public HostAvailability(UptimeServer host, IEnumerable<UptimeMeasure> measures, bool fillMissingWithNull = false, DateTime? lastExpectedTime = null)
            {
                if (lastExpectedTime.HasValue == false)
                    lastExpectedTime = DateTime.Now.AddMinutes(-1);

                Host = host;
                List<Availability> avail = new List<Availability>();
                UptimeMeasure first = measures.First();
                Availability prev = new Availability() { Time = first.clock, Response = first.value };
                avail.Add(prev);
                var data = measures.OrderBy(m => m.clock).ToArray();
                for (int i = 1; i < data.Length; i++)
                {
                    var curr = data[i];

                    if (fillMissingWithNull)
                    {
                        var diffInMin = (curr.clock - prev.Time).TotalMinutes;
                        if (diffInMin > 2.5) //velka mezera, dopln null
                        {
                            for (int j = 1; j < diffInMin - 1; j++)
                            {
                                DateTime prevTime = prev.Time.AddMinutes(j);

                                if (SkipThisTime(Host.Id, prevTime) == false)
                                    avail.Add(new Availability() { Time = prevTime, Response = Availability.TimeOuted });
                            }
                        }
                    }

                    if (SkipThisTime(Host.Id, curr.clock) == false)
                        avail.Add(new Availability() { Time = curr.clock, Response = curr.value, DownloadSpeed = null, HttpStatusCode = null });

                    prev = avail.Last();

                }
                //check last missing value
                var currLast = data[data.Length - 1];
                if ((lastExpectedTime.Value - currLast.clock).TotalMinutes > 5)
                {
                    var diffInMin = (lastExpectedTime.Value - currLast.clock).TotalMinutes;
                    if (diffInMin > 2.5) //velka mezera, dopln null
                    {
                        for (int j = 1; j < diffInMin - 1; j++)
                        {
                            DateTime prevTime = prev.Time.AddMinutes(j);

                            if (SkipThisTime(Host.Id, prevTime) == false)
                                avail.Add(new Availability() { Time = prevTime, Response = Availability.TimeOuted });
                        }
                    }

                }


                Data = avail.ToArray();
            }
            public UptimeServer Host { get; set; }


            public Availability[] Data { get; set; }

            public long ColSize(DateTime fromDate, DateTime toDate)
            {
                //return (long)(toDate - fromDate).TotalMilliseconds;
                return Data.LongLength;
            }

            AvailabilityStatistics _stat = null;
            public AvailabilityStatistics Statistics()
            {
                if (_stat == null)
                    _stat = new AvailabilityStatistics(Data);
                return _stat;
            }

            public UptimeSSL.Statuses WorstStatus(TimeSpan inLastTime)
            {
                if (Data == null)
                    return UptimeSSL.Statuses.Unknown;
                if (Data.Count() == 0)
                    return UptimeSSL.Statuses.Unknown;

                DateTime fromDate = Data.OrderByDescending(o => o.Time).First().Time.AddTicks(-1 * inLastTime.Ticks);

                return Data
                    .Where(d => d.Time >= fromDate)
                    .Where(s => s.Status() < UptimeSSL.Statuses.Unknown)
                    .Max(s => s.Status())
                    //?? Statuses.Unknown
                    ;
            }



            public IEnumerable<decimal?[]> DataForChart(DateTime fromDate, DateTime toDate, int line)
            {
                var d = Data
                    .Where(m => m.Time > fromDate)
                    .Select((v, index) => new[] { (long)(ToEpochLocalTime(v.Time)), (int)line, (decimal?)(v.Response) })
                    .ToArray();

                return d;
            }
            public static long ToEpochLocalTime(DateTime date)
            {
                return (long)((date - new DateTime(1970, 1, 1)).TotalMilliseconds);
            }
        }

    }
}
