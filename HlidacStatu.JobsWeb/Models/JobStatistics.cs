using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.Statistics;

namespace HlidacStatu.Ceny.Models
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
            var precalculatedJobsList = precalculatedJobs.ToList(); // aby nedocházelo k několikanásobné enumeraci
            
            //todo: salarydX se nikde nepouziva  dal v kodu - bezi tu zbytecne enumerace
            //var salarydX = precalculatedJobsList.Select(x =>new { p = (double)x.PricePerUnitVat, pk = x.JobPk }).ToList();

            var salaryd = precalculatedJobsList.Select(x => (double)x.PricePerUnitVat).ToList();
            decimal[] kvartily = Enumerable.Range(1, 100).Select(x => (decimal)salaryd.Percentile(x)).ToArray();
            double dolniKvartil = salaryd.LowerQuartile();
            double horniKvartil = salaryd.UpperQuartile();
            //decimal outlierRange = (horniKvartil - dolniKvartil) * 1.5m;
            double maximum = salaryd.Max(x => x);
            double minimum = salaryd.Min(x => x); ;
            double leftWhisk = salaryd.Percentile(10);
            if (leftWhisk < minimum)
                leftWhisk = minimum;
            double rightWhisk = salaryd.Percentile(90);
            if (rightWhisk > maximum)
                rightWhisk = maximum;

            //todo: vystup z tohohle volani se nikde nepouziva dal v kodu - bezi tu zbytecne enumerace
            precalculatedJobsList
                .Select(m => m.AnalyzaName)
                .Distinct();
            Name = name;

            Description = "";
            var subj = precalculatedJobsList.FirstOrDefault(m => !string.IsNullOrEmpty(m.AnalyzaName))?.AnalyzaName?.ToLower();
            if (subj != null && jobDescriptions.ContainsKey(subj))
            {
                Description = jobDescriptions[subj].FirstOrDefault(m => m.JobGrouped.ToLower() == name.ToLower())?.jobGroupedDescription ?? "";
            }

            Average = (decimal)salaryd.Average();
            Maximum = (decimal)maximum;
            Minimum = (decimal)minimum;
            Median = (decimal)salaryd.Median();
            DolniKvartil = (decimal)dolniKvartil;
            HorniKvartil = (decimal)horniKvartil;
            LeftWhisk = (decimal)leftWhisk;
            RightWhisk = (decimal)rightWhisk;
            LowOutliers = salaryd.Where(x => x < leftWhisk).OrderBy(x => x).Select(x => (decimal)x).ToArray();
            HighOutliers = salaryd.Where(x => x > rightWhisk).OrderBy(x => x).Select(x => (decimal)x).ToArray();
            PriceCount = salaryd.Count();
            Kvartily = kvartily;
            
            SupplierCount = precalculatedJobsList.SelectMany(x => x.IcaDodavatelu).Distinct().Count();
            ContractCount = precalculatedJobsList.Select(x => x.SmlouvaId).Distinct().Count();
            Dodavatele = precalculatedJobsList.SelectMany(x => x.IcaDodavatelu).Distinct().ToArray();
            Odberatele = precalculatedJobsList.Select(x => x.IcoOdberatele).Distinct().ToArray();
            Contracts = precalculatedJobsList.Select(x => x.SmlouvaId).Distinct().ToArray();
            Jobs = precalculatedJobs.ToList();
        }

        public string Name { get; set; }
        public string Description { get; set; }
        public decimal DolniKvartil { get; set; }
        public decimal HorniKvartil { get; set; }
        
        public decimal[] Kvartily { get; set; }
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

        public List<JobPrecalculated> Jobs { get; set; }
        public string[] Contracts { get; set; }
        public string[] Odberatele { get; set; }
        public string[] Dodavatele { get; set; }

        /// <summary>
        /// Vrátí kvartil
        /// </summary>
        /// <param name="kvartil">číslo kvartilu od 1 do 100</param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public decimal Kvartil(int kvartil)
        {
            if (kvartil < 1 || kvartil > 100)
                throw new ArgumentOutOfRangeException();

            return Kvartily[kvartil - 1];
        }
    }
}