using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.Statistics;

namespace HlidacStatu.JobsWeb.Models
{
    public class JobStatistics
    {
        public JobStatistics()
        {
        }
        
        public JobStatistics(IEnumerable<JobPrecalculated> precalculatedJobs, string name)
        {
            var salaryd = precalculatedJobs.Select(x => (double)x.SalaryMd).ToList();
            decimal dolniKvartil = (decimal)salaryd.LowerQuartile();
            decimal horniKvartil = (decimal)salaryd.UpperQuartile();
            decimal outlierRange = (horniKvartil - dolniKvartil) * 1.5m;
            decimal maximum = precalculatedJobs.Max(x => x.SalaryMd);
            decimal minimum = precalculatedJobs.Min(x => x.SalaryMd);
            decimal leftWhisk = dolniKvartil - outlierRange;
            if (leftWhisk < minimum)
                leftWhisk = minimum;
            decimal rightWhisk = horniKvartil + outlierRange;
            if (rightWhisk > maximum)
                rightWhisk = maximum;

            
            Name = name;
            Average = precalculatedJobs.Average(x => x.SalaryMd);
            Maximum = maximum;
            Minimum = minimum;
            Median = (decimal)salaryd.Median();
            DolniKvartil = dolniKvartil;
            HorniKvartil = horniKvartil;
            LeftWhisk = leftWhisk;
            RightWhisk = rightWhisk;
            LowOutliers = precalculatedJobs.Where(x => x.SalaryMd < leftWhisk).Select(x => x.SalaryMd)
                .OrderBy(x => x).ToArray();
            HighOutliers = precalculatedJobs.Where(x => x.SalaryMd > rightWhisk).Select(x => x.SalaryMd)
                .OrderBy(x => x).ToArray();
            ContractCount = precalculatedJobs.Count();
            SupplierCount = precalculatedJobs.Select(x => x.IcoOdberatele).Distinct().Count();
            
        }
        
        public string Name { get; set; }
        public decimal DolniKvartil { get; set; }
        public decimal HorniKvartil { get; set; }
        public decimal Minimum { get; set; }
        public decimal Maximum { get; set; }
        public decimal Average { get; set; }
        public decimal Median { get; set; }
        
        public decimal LeftWhisk { get; set; }
        public decimal RightWhisk { get; set; }
        public decimal[] LowOutliers { get; set; }
        public decimal[] HighOutliers { get; set; }
        public int ContractCount { get; set; }
        public int SupplierCount { get; set; }
    }

    public class JobPrecalculated
    {
        public string SmlouvaId { get; set; }
        public string IcoOdberatele { get; set; }
        public string[] IcoDodavatele { get; set; }
    
        public int Year { get; set; }
    
        public long JobPk { get; set; }
        public string JobGrouped { get; set; }
        public decimal SalaryMd { get; set; }
        public decimal SalaryMdVat { get; set; }
        public string Subject { get; set; }
        public string[] Tags { get; set; }
        
        
    }
}