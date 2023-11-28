using System.Collections.Generic;

namespace WatchdogAnalytics.Models
{
    public class BarGraph
    {
        public List<JobPrecalculated> Data { get; set; }
        public string Title { get; set; } = "Histogram";
        public string Description { get; set; }
        public int MaxWidth { get; set; } = 400;

    }
}