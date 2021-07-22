using System;
using System.Collections.Generic;

namespace HlidacStatu.Repositories.ProfilZadavatelu
{
    public class DateTimeInterval
    {
        public enum Interval
        {
            Minutes,
            Hours,
            Days,
            Weeks,
            //Months,
            //Quarter,
            Year
        }

        DateTimeRange range;
        public DateTimeInterval(DateTime start, DateTime end)
            : this(new DateTimeRange(start, end))
        {
        }

        public DateTimeInterval(DateTimeRange daterange)
        {
            range = daterange;
        }

        public IEnumerable<DateTimeRange> Split(Interval interval, double number, bool overlapEndAndNextStart)
        {
            TimeSpan ts;            
            switch (interval)
            {
                case Interval.Minutes:
                    ts = TimeSpan.FromMinutes(number);
                    break;
                case Interval.Hours:
                    ts = TimeSpan.FromHours(number);
                    break;
                case Interval.Days:
                    ts = TimeSpan.FromDays(number);
                    break;
                case Interval.Weeks:
                    ts = TimeSpan.FromDays(number * 7);
                    break;
                case Interval.Year:
                    ts = TimeSpan.FromDays(365);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("Interval");
            }
            return Split(ts, overlapEndAndNextStart);
        }
        public IEnumerable<DateTimeRange> Split(TimeSpan interval, bool overlapEndAndNextStart)
        {
            if (interval > range.Length)
                return new DateTimeRange[] { range };
            else
            {
                List<DateTimeRange> ranges = new List<DateTimeRange>();
                DateTime curr = range.Start;
                do
                {

                    DateTime rangeEnd = curr.Add(interval);
                    if (!overlapEndAndNextStart)
                        ranges.Add(new DateTimeRange(curr, rangeEnd.AddTicks(-1)));
                    else
                        ranges.Add(new DateTimeRange(curr, rangeEnd));

                    curr = rangeEnd;

                } while (curr < range.End);
                return ranges;
            }
        }
    }
}
