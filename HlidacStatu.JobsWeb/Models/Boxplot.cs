using System.Collections.Generic;

namespace HlidacStatu.JobsWeb.Models
{
    public class Boxplot
    {
        public int? Height { get; set; }
        public List<JobStatistics> BasicData { get; set; }
        public List<JobStatistics> CompareWith { get; set; }
    }
}