using MathNet.Numerics.Statistics;

using System.Collections.Generic;
using System.Linq;

namespace HlidacStatu.JobsWeb.Models
{
    public class JobStatistics
    {
        static Dictionary<string, IEnumerable<Entities.InDocJobNameDescription>> jobDescriptions =
            new Dictionary<string, IEnumerable<Entities.InDocJobNameDescription>>();

        static JobStatistics()
        {
            using (var db = new Entities.DbEntities())
            {
                var subjects = db.InDocJobNameDescription.AsEnumerable().Select(m => m.Analyza.ToLower()).Distinct();
                foreach (var subj in subjects.Where(m => !string.IsNullOrEmpty(m)))
                {
                    jobDescriptions.Add(subj, db.InDocJobNameDescription.AsEnumerable().Where(m => m.Analyza.ToLower() == subj).ToArray());
                }
            }
        }

        public JobStatistics()
        {
        }

        public JobStatistics(IEnumerable<JobPrecalculated> precalculatedJobs, string name)
        {
            var salarydX = precalculatedJobs.Select(x =>new { p = (double)x.PricePerUnitVat, pk = x.JobPk }).ToList();


            var salaryd = precalculatedJobs.Select(x => (double)x.PricePerUnitVat).ToList();
            decimal dolniKvartil = (decimal)salaryd.LowerQuartile();
            decimal horniKvartil = (decimal)salaryd.UpperQuartile();
            //decimal outlierRange = (horniKvartil - dolniKvartil) * 1.5m;
            decimal maximum = (decimal)salaryd.Max(x => x);
            decimal minimum = (decimal)salaryd.Min(x => x); ;
            decimal leftWhisk = (decimal)salaryd.Percentile(10);
            if (leftWhisk < minimum)
                leftWhisk = minimum;
            decimal rightWhisk = (decimal)salaryd.Percentile(90);
            if (rightWhisk > maximum)
                rightWhisk = maximum;


            precalculatedJobs
                .Select(m => m.AnalyzaName)
                .Distinct();
            Name = name;

            Description = "";
            var subj = precalculatedJobs.FirstOrDefault(m => !string.IsNullOrEmpty(m.AnalyzaName))?.AnalyzaName?.ToLower();
            if (subj != null && jobDescriptions.ContainsKey(subj))
            {
                Description = jobDescriptions[subj].FirstOrDefault(m => m.JobGrouped.ToLower() == name.ToLower())?.jobGroupedDescription ?? "";
            }

            Average = precalculatedJobs.Average(x => x.PricePerUnit);
            Maximum = maximum;
            Minimum = minimum;
            Median = (decimal)salaryd.Median();
            DolniKvartil = dolniKvartil;
            HorniKvartil = horniKvartil;
            LeftWhisk = leftWhisk;
            RightWhisk = rightWhisk;
            LowOutliers = precalculatedJobs.Where(x => x.PricePerUnit < leftWhisk).Select(x => x.PricePerUnit)
                .OrderBy(x => x).ToArray();
            HighOutliers = precalculatedJobs.Where(x => x.PricePerUnit > rightWhisk).Select(x => x.PricePerUnit)
                .OrderBy(x => x).ToArray();
            PriceCount = precalculatedJobs.Count();
            SupplierCount = precalculatedJobs.SelectMany(x => x.IcaDodavatelu).Distinct().Count();
            ContractCount = precalculatedJobs.Select(x => x.SmlouvaId).Distinct().Count();

            this.Dodavatele = precalculatedJobs.SelectMany(x => x.IcaDodavatelu).Distinct().ToArray();
            this.Odberatele = precalculatedJobs.Select(x => x.IcoOdberatele).Distinct().ToArray();
            this.Contracts = precalculatedJobs.Select(x => x.SmlouvaId).Distinct().ToArray();

        }

        public string Name { get; set; }
        public string Description { get; set; }
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
        public int PriceCount { get; set; }

        public string[] Contracts { get; set; }
        public string[] Odberatele { get; set; }
        public string[] Dodavatele { get; set; }
    }
}