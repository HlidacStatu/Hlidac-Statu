using System;
using System.Collections.Generic;
using System.Linq;

namespace HlidacStatu.Lib.Data.External.Zabbix
{
    public class ZabAvailabilityStatistics
    {


        public DateTime MinDate { get; private set; } = DateTime.MinValue;
        public DateTime MaxDate { get; private set; } = DateTime.MaxValue;

        public ZabDataPerStatus<TimeSpan> DurationTotal { get; private set; } = new ZabDataPerStatus<TimeSpan>(TimeSpan.Zero, TimeSpan.Zero,TimeSpan.Zero);
        public ZabDataPerStatus<TimeSpan> LongestDuration { get; private set; } = new ZabDataPerStatus<TimeSpan>(TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero);
        public ZabDataPerStatus<decimal> PercentOfTime { get; private set; } = new ZabDataPerStatus<decimal>(0,0,0);


        IEnumerable<ZabAvailability> data;
        public ZabAvailabilityStatistics(IEnumerable<ZabAvailability> availData)
        {
            if (availData == null)
                throw new ArgumentNullException("data");
            data = availData;

            if (data.Count() > 0)
            {
                Calculate();
            }
        }

        private void Calculate()
        {
            MinDate = data.Min(m => m.Time);
            MaxDate = data.Max(m => m.Time);
            PercentOfTime = new ZabDataPerStatus<decimal>(
                StatusPerData(Statuses.OK),
                StatusPerData(Statuses.Pomalé),
                StatusPerData(Statuses.Nedostupné),
                StatusPerData(Statuses.Unknown)
                );
            decimal totalSec = (decimal)(MaxDate - MinDate).TotalSeconds;

            DurationTotal = new ZabDataPerStatus<TimeSpan>(
                TimeSpan.FromSeconds((double)(PercentOfTime.OK * totalSec)),
                TimeSpan.FromSeconds((double)(PercentOfTime.Pomale * totalSec)),
                TimeSpan.FromSeconds((double)(PercentOfTime.Nedostupne * totalSec)),
                TimeSpan.FromSeconds((double)(PercentOfTime.Unknown * totalSec))
                );

            var intervals = CalculateIntervals();
            LongestDuration = new ZabDataPerStatus<TimeSpan>();
            if (intervals.Any(i => i.Status == Statuses.OK))
            {
                LongestDuration.OK = TimeSpan.FromSeconds(
                            intervals
                                .Where(i => i.Status == Statuses.OK)
                                .Select(s => (s.To - s.From).TotalSeconds)
                                .Max()
                    );
            }
            else
                LongestDuration.OK = TimeSpan.FromSeconds(0);

            if (intervals.Any(i => i.Status == Statuses.Pomalé))
            {
                LongestDuration.Pomale = TimeSpan.FromSeconds(
                            intervals
                                .Where(i => i.Status == Statuses.Pomalé)
                                .Select(s => (s.To - s.From).TotalSeconds)
                                .Max()
                    );
            }
            else
                LongestDuration.Pomale = TimeSpan.FromSeconds(0);

            if (intervals.Any(i => i.Status == Statuses.Nedostupné))
            {
                LongestDuration.Nedostupne = TimeSpan.FromSeconds(
                            intervals
                                .Where(i => i.Status == Statuses.Nedostupné)
                                .Select(s => (s.To - s.From).TotalSeconds)
                                .Max()
                    );
            }
            else
                LongestDuration.Nedostupne = TimeSpan.FromSeconds(0);

            if (intervals.Any(i => i.Status == Statuses.Unknown))
            {
                LongestDuration.Unknown = TimeSpan.FromSeconds(
                            intervals
                                .Where(i => i.Status == Statuses.Unknown)
                                .Select(s => (s.To - s.From).TotalSeconds)
                                .Max()
                    );
            }
            else
                LongestDuration.Unknown = TimeSpan.FromSeconds(0);

        }


        private List<ZabAvailabilityInterval> CalculateIntervals()
        {
            List<ZabAvailabilityInterval> interv = new List<ZabAvailabilityInterval>();

            var d = data.OrderBy(m => m.Time).ToArray();
            for (int i = 0; i < d.Length - 1; i++)
            {
                var m = d[i];
                Statuses status = m.Status();

                if (i > 0 && (m.Time - d[i - 1].Time).TotalMinutes > 3) //pokud chybi data za vice nez 3 min, oznat je cervene
                {
                    interv.Add(new ZabAvailabilityInterval(d[i - 1].Time, m.Time, Statuses.Nedostupné));
                }

                //hledej kdy tento status konci
                var j = 1;
                while (
                    i + j < d.Length - 1
                    && d[i + j].Status() == status
                    && (i > 0 && (d[i + j].Time - d[i + j - 1].Time).TotalMinutes < 3)
                    )
                {
                    j++;
                }
                interv.Add(new ZabAvailabilityInterval(m.Time, d[i + j].Time, status));
                i = i + (j - 1);



            }
            return interv;

        }
        private decimal StatusPerData(Statuses status)
        {
            decimal perc100 = data.Count();
            if (perc100 == 0)
                return 0;
            var num = data.Count(d => d.Status() == status);

            return (decimal)num / perc100;
        }

    }
}