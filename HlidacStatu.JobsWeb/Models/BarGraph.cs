using System.Collections.Generic;

namespace HlidacStatu.Ceny.Models
{
    public class BarGraph
    {
        public List<JobPrecalculated> Data { get; set; }
        public string Title { get; set; } = "Histogram";
        public string Description { get; set; }

    }
}