using System.Collections.Generic;

namespace HlidacStatu.JobsWeb.Models
{
    public class BarGraph
    {
        public List<JobPrecalculated> Data { get; set; }
        public string Title { get; set; } = "Histogram";
        
    }
}