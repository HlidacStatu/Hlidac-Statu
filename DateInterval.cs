using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

namespace Devmasters.DT
{
    [System.Diagnostics.DebuggerDisplay("{debuggerdisplay,nq}")]
    public class DateInterval
    {
        private string debuggerdisplay
        {
            get
            {
                var s = string.Format($"{this?.From:yyyy-MM-dd HH:mm:ss.ffff} - {this?.To:yyyy-MM-dd HH:mm:ss.ffff}");
                return s;
            }
        }

        public DateInterval(int year)
            : this(new DateTime(year, 1, 1), new DateTime(year + 1, 1, 1).AddMilliseconds(-1))
        {

        }
        public DateInterval(DateOnly? from, DateOnly? to)
        {
            this.From = from?.ToDateTime(TimeOnly.MinValue); //00:00 time
            //this._fromVal = this.From ?? DateTime.MinValue;
            this.To = to?.ToDateTime(TimeOnly.MinValue); //00:00 time
            //this._toVal = this.To ?? DateTime.MaxValue;

        }

        [JsonConstructor]
        public DateInterval(DateTime? from, DateTime? to)
        {
            this.From = from;
            //this._fromVal = from ?? DateTime.MinValue;
            this.To = to;
            //this._toVal = to ?? DateTime.MaxValue;

        }
        
        //private DateTime _fromVal;
        public DateTime? From { get; private set; }

        //private DateTime _toVal;
        public DateTime? To { get; private set; }

        public bool IsDateInInterval(DateTime? date)
        {
            return Util.IsDateInInterval(this.From, this.To, date);
        }


        public bool IsContinuingIntervalWith(DateInterval dateInterval)
        {
            return IsContinuingIntervals(this, dateInterval);
        }
        public bool IsOverlappingIntervalsWith(DateInterval dateInterval)
        {
            return IsOverlappingIntervals(this, dateInterval);
        }

        public bool Mergeable(DateInterval dateInterval, out DateInterval merged)
        {
            merged = null;
            if (IsOverlappingIntervalsWith(dateInterval))
            {
                merged = new DateInterval(Util.LowerDate(this.From, dateInterval.From), Util.HigherDate(this.To, dateInterval.To));
                return true;
            }

            return false;
        }

        public DateInterval OverlappingInterval(DateInterval dateInterval)
        {
            return GetOverlappingInterval(this, dateInterval);
        }


        public static bool IsOverlappingIntervals(DateInterval dateInterval1, DateInterval dateInterval2)
        {
            return Util.IsOverlappingIntervals(dateInterval1.From, dateInterval1.To, dateInterval2.From, dateInterval2.To);
        }

        public static DateInterval GetOverlappingInterval(DateInterval dateInterval1, DateInterval dateInterval2)
        {
            if (IsOverlappingIntervals(dateInterval1, dateInterval2))
            {

                DateTime? from = Util.HigherDate(dateInterval1.From, dateInterval2.From,false);                

                DateTime? to = Util.LowerDate(dateInterval1.To, dateInterval2.To,false);
                return new DateInterval(from, to);
            }
            return null;
        }

        public static bool IsContinuingIntervals(DateInterval dateInterval1, DateInterval dateInterval2)
        {
            return Util.IsContinuingIntervals(dateInterval1.From, dateInterval1.To, dateInterval2.From, dateInterval2.To);
        }
        public static DateInterval[] MergeDateIntervals(params DateInterval[] intervals)
        {
            if (intervals == null)
                return null;
            if (intervals.Length == 0)
                return null;
            if (intervals.Length == 1)
                return intervals;


            List<DateInterval> res = new List<DateInterval>();
            var sorted = intervals.OrderBy(m => m.From ?? DateTime.MinValue).ToList();
            DateInterval curr = sorted[0];

            for (int i = 1; i < sorted.Count(); i++)
            {
                DateInterval merged;
                if (curr.Mergeable(sorted[i], out merged))
                    curr = merged;
                else
                {
                    res.Add(curr);
                    curr = sorted[i];
                }
            }
            res.Add(curr);

            return res.ToArray();

        }

        public string ToNiceString(string dateDelimiter = "~", string noValue = "-", string dateFormat = "yyyy-MM-dd HH:mm:ss")
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(this.From.HasValue ? this.From.Value.ToString(dateFormat) : noValue);
            sb.Append($" {dateDelimiter} ");
            sb.Append(this.To.HasValue ? this.To.Value.ToString(dateFormat) : noValue);
            return sb.ToString();
        }
    }
}
