using System;
using System.Collections.Generic;
using System.Linq;

namespace HlidacStatu.Entities
{
    public partial class UptimeServer
    {
        public class AvailabilityStatistics
        {


            public DateTime MinDate { get;  set; } = DateTime.MinValue;
            public DateTime MaxDate { get; set; } = DateTime.MaxValue;

            public MeasureStatus<TimeSpan> DurationTotal { get; private set; } = new MeasureStatus<TimeSpan>(TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero);
            public MeasureStatus<TimeSpan> LongestDuration { get; private set; } = new MeasureStatus<TimeSpan>(TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero);
            public MeasureStatus<decimal> PercentOfTime { get; private set; } = new MeasureStatus<decimal>(0, 0, 0);


            IEnumerable<Availability> data;
            public AvailabilityStatistics()
            { }

            public AvailabilityStatistics(IEnumerable<Availability> availData)
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
                PercentOfTime = new MeasureStatus<decimal>(
                    StatusPerData(UptimeSSL.Statuses.OK),
                    StatusPerData(UptimeSSL.Statuses.Pomalé),
                    StatusPerData(UptimeSSL.Statuses.Nedostupné),
                    StatusPerData(UptimeSSL.Statuses.Unknown)
                    );
                decimal totalSec = (decimal)(MaxDate - MinDate).TotalSeconds;

                DurationTotal = new MeasureStatus<TimeSpan>(
                    TimeSpan.FromSeconds((double)(PercentOfTime.OK * totalSec)),
                    TimeSpan.FromSeconds((double)(PercentOfTime.Pomale * totalSec)),
                    TimeSpan.FromSeconds((double)(PercentOfTime.Nedostupne * totalSec)),
                    TimeSpan.FromSeconds((double)(PercentOfTime.Unknown * totalSec))
                    );

                var intervals = CalculateIntervals();
                LongestDuration = new MeasureStatus<TimeSpan>();
                if (intervals.Any(i => i.Status == UptimeSSL.Statuses.OK))
                {
                    LongestDuration.OK = TimeSpan.FromSeconds(
                                intervals
                                    .Where(i => i.Status == UptimeSSL.Statuses.OK)
                                    .Select(s => (s.To - s.From).TotalSeconds)
                                    .Max()
                        );
                }
                else
                    LongestDuration.OK = TimeSpan.FromSeconds(0);

                if (intervals.Any(i => i.Status == UptimeSSL.Statuses.Pomalé))
                {
                    LongestDuration.Pomale = TimeSpan.FromSeconds(
                                intervals
                                    .Where(i => i.Status == UptimeSSL.Statuses.Pomalé)
                                    .Select(s => (s.To - s.From).TotalSeconds)
                                    .Max()
                        );
                }
                else
                    LongestDuration.Pomale = TimeSpan.FromSeconds(0);

                if (intervals.Any(i => i.Status == UptimeSSL.Statuses.Nedostupné))
                {
                    LongestDuration.Nedostupne = TimeSpan.FromSeconds(
                                intervals
                                    .Where(i => i.Status == UptimeSSL.Statuses.Nedostupné)
                                    .Select(s => (s.To - s.From).TotalSeconds)
                                    .Max()
                        );
                }
                else
                    LongestDuration.Nedostupne = TimeSpan.FromSeconds(0);

                if (intervals.Any(i => i.Status == UptimeSSL.Statuses.Unknown))
                {
                    LongestDuration.Unknown = TimeSpan.FromSeconds(
                                intervals
                                    .Where(i => i.Status == UptimeSSL.Statuses.Unknown)
                                    .Select(s => (s.To - s.From).TotalSeconds)
                                    .Max()
                        );
                }
                else
                    LongestDuration.Unknown = TimeSpan.FromSeconds(0);

            }


            private List<AvailabilityInterval> CalculateIntervals()
            {
                List<AvailabilityInterval> interv = new List<AvailabilityInterval>();

                var d = data.OrderBy(m => m.Time).ToArray();
                for (int i = 0; i < d.Length - 1; i++)
                {
                    var m = d[i];
                    UptimeSSL.Statuses status = m.Status();

                    if (i > 0 && (m.Time - d[i - 1].Time).TotalMinutes > 3) //pokud chybi data za vice nez 3 min, oznat je cervene
                    {
                        interv.Add(new AvailabilityInterval(d[i - 1].Time, m.Time, UptimeSSL.Statuses.Nedostupné));
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
                    interv.Add(new AvailabilityInterval(m.Time, d[i + j].Time, status));
                    i = i + (j - 1);



                }
                return interv;

            }
            private decimal StatusPerData(UptimeSSL.Statuses status)
            {
                decimal perc100 = data.Count();
                if (perc100 == 0)
                    return 0;
                var num = data.Count(d => d.Status() == status);

                return (decimal)num / perc100;
            }

        }

    }
}
