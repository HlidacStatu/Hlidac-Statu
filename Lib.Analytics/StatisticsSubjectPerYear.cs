using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace HlidacStatu.Lib.Analytics
{
    [JsonConverter(typeof(StatisticsPerYearConverter))]
    public class StatisticsSubjectPerYear<T> : StatisticsPerYear<T>
        where T : CoreStat, IAddable<T>, new()
    {
        public string ICO { get; set; }
        
        [JsonConstructor]
        public StatisticsSubjectPerYear()
        : base()
        { }

        public StatisticsSubjectPerYear(string ico, StatisticsPerYear<T> baseObj)
            : base(baseObj)
        {
            ICO = ico;
        }

        public StatisticsSubjectPerYear(string ico, Func<T, int> yearSelector, IEnumerable<T> data)
            : base(yearSelector, data)
        {
            ICO = ico;
        }

        /// <summary>
        /// Creates new statistics
        /// </summary>
        /// <param name="ico">Subject Ico</param>
        /// <param name="data">Dictionary where key = Year, value = T</param>
        public StatisticsSubjectPerYear(string ico, Dictionary<int, T> data)
            : base(data)
        {
            ICO = ico;
        }


        public static StatisticsSubjectPerYear<T> Aggregate(IEnumerable<StatisticsSubjectPerYear<T>> statistics)
        {
            if (statistics is null)
                return new StatisticsSubjectPerYear<T>();

            var aggregatedStatistics = new StatisticsSubjectPerYear<T>(
                $"{statistics.FirstOrDefault().ICO}",
                AggregateStats(statistics));

            return aggregatedStatistics;
        }

        public static StatisticsSubjectPerYear<T> Subtract(IEnumerable<StatisticsSubjectPerYear<T>> statistics)
        {
            if (statistics is null)
                return new StatisticsSubjectPerYear<T>();

            var aggregatedStatistics = new StatisticsSubjectPerYear<T>(
                $"{statistics.FirstOrDefault().ICO}",
                SubtractFromStats(statistics));

            return aggregatedStatistics;
        }
        

    }
}
