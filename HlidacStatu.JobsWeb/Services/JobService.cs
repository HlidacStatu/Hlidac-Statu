using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HlidacStatu.JobsWeb.Models;
using HlidacStatu.Repositories;
using MathNet.Numerics.Statistics;

namespace HlidacStatu.JobsWeb.Services
{
    public class JobService
    {
        
        // make it "in memory", load it asynchronously, recalculate once a day?
        private List<JobPrecalculated> DistinctJobs { get; set; }

        public async Task LoadData()
        {
            var allJobs = await InDocJobsRepo.GetAllJobsWithRelatedDataAsync();
            
            // important to filter duplicates here, can be extended in future
            DistinctJobs = allJobs
                .GroupBy(j => j.JobPk)
                .Select(g =>
                {
                    var (net, vat) = FixSalaries(g.FirstOrDefault().SalaryMd, g.FirstOrDefault().SalaryMdVat);
                    return new JobPrecalculated()
                    {
                        Subject = g.FirstOrDefault().Subject,
                        Year = g.FirstOrDefault().Year,
                        IcoOdberatele = g.FirstOrDefault().IcoOdberatele,
                        JobGrouped = g.FirstOrDefault().JobGrouped,
                        SmlouvaId = g.FirstOrDefault().SmlouvaId,
                        JobPk = g.Key,
                        SalaryMd = net,
                        SalaryMdVat = vat,
                        IcoDodavatele = g.Select(i => i.IcoOdberatele).ToArray()
                    };
                }).ToList();

        }

        private (decimal net, decimal vat) FixSalaries(decimal? net, decimal? vat)
        {
            decimal finalNet = net ?? 0;
            decimal finalVat = vat ?? 0;

            if (finalNet == 0)
                finalNet = finalVat / 1.21m;
            if (finalVat == 0)
                finalVat = finalNet * 1.21m;

            if (finalNet > finalVat)
                return (finalVat, finalNet);

            return (finalNet, finalVat);
        }
        
        public List<JobStatistics> GetStatitstics()
        {
            var result = DistinctJobs
                .GroupBy(j => j.JobGrouped)
                .Select(g =>
                {
                    var salaryd = g.Select(x => (double)x.SalaryMd).ToList();
                    
                    return new JobStatistics()
                    {
                        JobName = g.Key,
                        Average = g.Average(x => x.SalaryMd),
                        Maximum = g.Max(x => x.SalaryMd),
                        Minimum = g.Min(x => x.SalaryMd),
                        Median = (decimal)salaryd.Median(),
                        DolniKvartil = (decimal)salaryd.LowerQuartile(),
                        HorniKvartil = (decimal)salaryd.UpperQuartile(),
                        ContractCount = g.Count(),
                        SupplierCount = g.SelectMany(x=>x.IcoOdberatele).Count()

                    };
                })
                .ToList();

            return result;
        }
    }
}