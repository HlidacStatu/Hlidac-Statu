using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using HlidacStatu.JobsWeb.Models;
using HlidacStatu.Repositories;
using MathNet.Numerics.Statistics;

namespace HlidacStatu.JobsWeb.Services
{
    public static class JobService
    {
        // make it "in memory", load it asynchronously, recalculate once a day?
        //private static List<JobPrecalculated> DistinctJobs { get; set; }

        private static List<JobStatistics> JobOverview { get; set; }

        private static Dictionary<string, List<JobStatistics>> TagOverview { get; set; }

        private static Dictionary<string, List<JobStatistics>> OdberatelOverview { get; set; }
        private static Dictionary<string, List<JobStatistics>> DodavatelOverview { get; set; }

        //Meta informations
        public static DateTime LastRecalculationStarted { get; private set; }
        public static DateTime JobRecalculationEnd { get; private set; }
        public static DateTime TagRecalculationEnd { get; private set; }
        public static DateTime OdberateleRecalculationEnd { get; private set; }
        public static DateTime DodavateleRecalculationEnd { get; private set; }
        public static long RecalculationTimeMs { get; private set; }

        public static bool IsRecalculating { get; private set; }
        public static string LastError { get; private set; }

        static JobService()
        {
            RecalculateAsync().ConfigureAwait(false);
        }

        public static async Task RecalculateAsync()
        {
            IsRecalculating = true;
            LastRecalculationStarted = DateTime.Now;

            var stopWatch = new Stopwatch();
            stopWatch.Start();

            try
            {
                LastError = "";
                var allJobs = await InDocJobsRepo.GetAllJobsWithRelatedDataAsync();

                // important to filter duplicates here, can be extended in future
                var distinctJobs = allJobs
                    .GroupBy(j => j.JobPk)
                    .Select(g =>
                    {
                        var (net, vat) = FixSalaries(g.FirstOrDefault().SalaryMd, g.FirstOrDefault().SalaryMdVat);
                        var tags = g.FirstOrDefault().Tags?.Split("|",
                            StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                        if (tags == null || tags.Length == 0)
                            tags = new[] { "-" };
                        return new JobPrecalculated()
                        {
                            Subject = g.FirstOrDefault().Subject,
                            Year = g.FirstOrDefault().Year,
                            IcoOdberatele = g.FirstOrDefault().IcoOdberatele,
                            JobGrouped = g.FirstOrDefault().JobGrouped,
                            SmlouvaId = g.FirstOrDefault().SmlouvaId,
                            Tags = tags,
                            JobPk = g.Key,
                            SalaryMd = net,
                            SalaryMdVat = vat,
                            IcoDodavatele = g.Select(i => i.IcoOdberatele).ToArray(),
                        };
                    }).ToList();

                CalculateJobs(distinctJobs);

                CalculateTags(distinctJobs);

                CalculateOdberatele(distinctJobs);

                CalculateDodavatele(distinctJobs);
            }
            catch (Exception e)
            {
                LastError = e.Message;
            }

            stopWatch.Stop();
            RecalculationTimeMs = stopWatch.ElapsedMilliseconds;
            IsRecalculating = false;
        }

        private static void CalculateJobs(List<JobPrecalculated> distinctJobs)
        {
            JobOverview = distinctJobs
                .GroupBy(j => j.JobGrouped)
                .Select(g =>
                {
                    var salaryd = g.Select(x => (double)x.SalaryMd).ToList();
                    decimal dolniKvartil = (decimal)salaryd.LowerQuartile();
                    decimal horniKvartil = (decimal)salaryd.UpperQuartile();
                    decimal outlierRange = (horniKvartil - dolniKvartil) * 1.5m;
                    decimal maximum = g.Max(x => x.SalaryMd);
                    decimal minimum = g.Min(x => x.SalaryMd);
                    decimal leftWhisk = dolniKvartil - outlierRange;
                    if (leftWhisk < minimum)
                        leftWhisk = minimum;
                    decimal rightWhisk = horniKvartil + outlierRange;
                    if (rightWhisk > maximum)
                        rightWhisk = maximum;

                    return new JobStatistics()
                    {
                        Name = g.Key,
                        Average = g.Average(x => x.SalaryMd),
                        Maximum = maximum,
                        Minimum = minimum,
                        Median = (decimal)salaryd.Median(),
                        DolniKvartil = dolniKvartil,
                        HorniKvartil = horniKvartil,
                        LeftWhisk = leftWhisk,
                        RightWhisk = rightWhisk,
                        LowOutliers = g.Where(x => x.SalaryMd < leftWhisk).Select(x => x.SalaryMd)
                            .OrderBy(x => x).ToArray(),
                        HighOutliers = g.Where(x => x.SalaryMd > rightWhisk).Select(x => x.SalaryMd)
                            .OrderBy(x => x).ToArray(),
                        ContractCount = g.Count(),
                        SupplierCount = g.Select(x => x.IcoOdberatele).Distinct().Count()
                    };
                })
                .ToList();
            JobRecalculationEnd = DateTime.Now;
        }

        private static void CalculateTags(List<JobPrecalculated> distinctJobs)
        {
            TagOverview = distinctJobs
                .SelectMany(j => j.Tags, (precalculated, tag) => new { precalculated, dodavatel = tag })
                .GroupBy(j => j.precalculated.JobGrouped)
                .ToDictionary(g => g.Key, g =>
                    g.GroupBy(i => i.dodavatel)
                        .Select(ig =>
                        {
                            var salaryd = ig.Select(x => (double)x.precalculated.SalaryMd).ToList();
                            decimal dolniKvartil = (decimal)salaryd.LowerQuartile();
                            decimal horniKvartil = (decimal)salaryd.UpperQuartile();
                            decimal outlierRange = (horniKvartil - dolniKvartil) * 1.5m;
                            decimal maximum = g.Max(x => x.precalculated.SalaryMd);
                            decimal minimum = g.Min(x => x.precalculated.SalaryMd);
                            decimal leftWhisk = dolniKvartil - outlierRange;
                            if (leftWhisk < minimum)
                                leftWhisk = minimum;
                            decimal rightWhisk = horniKvartil + outlierRange;
                            if (rightWhisk > maximum)
                                rightWhisk = maximum;

                            return new JobStatistics()
                            {
                                Name = ig.Key,
                                Average = ig.Average(x => x.precalculated.SalaryMd),
                                Maximum = maximum,
                                Minimum = minimum,
                                Median = (decimal)salaryd.Median(),
                                DolniKvartil = (decimal)salaryd.LowerQuartile(),
                                HorniKvartil = (decimal)salaryd.UpperQuartile(),
                                LeftWhisk = leftWhisk,
                                RightWhisk = rightWhisk,
                                LowOutliers = g.Where(x => x.precalculated.SalaryMd < leftWhisk)
                                    .Select(x => x.precalculated.SalaryMd)
                                    .OrderBy(x => x).ToArray(),
                                HighOutliers = g.Where(x => x.precalculated.SalaryMd > rightWhisk)
                                    .Select(x => x.precalculated.SalaryMd)
                                    .OrderBy(x => x).ToArray(),
                                ContractCount = ig.Count(),
                                SupplierCount = ig.Select(x => x.precalculated.IcoOdberatele).Distinct().Count()
                            };
                        })
                        .ToList()
                );
            TagRecalculationEnd = DateTime.Now;
        }

        private static void CalculateOdberatele(List<JobPrecalculated> distinctJobs)
        {
            OdberatelOverview = distinctJobs
                .GroupBy(j => j.IcoOdberatele)
                .ToDictionary(g => g.Key, g =>
                    g.GroupBy(i => i.JobGrouped)
                        .Select(ig =>
                        {
                            var salaryd = ig.Select(x => (double)x.SalaryMd).ToList();
                            decimal dolniKvartil = (decimal)salaryd.LowerQuartile();
                            decimal horniKvartil = (decimal)salaryd.UpperQuartile();
                            decimal outlierRange = (horniKvartil - dolniKvartil) * 1.5m;
                            decimal maximum = g.Max(x => x.SalaryMd);
                            decimal minimum = g.Min(x => x.SalaryMd);
                            decimal leftWhisk = dolniKvartil - outlierRange;
                            if (leftWhisk < minimum)
                                leftWhisk = minimum;
                            decimal rightWhisk = horniKvartil + outlierRange;
                            if (rightWhisk > maximum)
                                rightWhisk = maximum;

                            return new JobStatistics()
                            {
                                Name = ig.Key,
                                Average = ig.Average(x => x.SalaryMd),
                                Maximum = maximum,
                                Minimum = minimum,
                                Median = (decimal)salaryd.Median(),
                                DolniKvartil = (decimal)salaryd.LowerQuartile(),
                                HorniKvartil = (decimal)salaryd.UpperQuartile(),
                                LeftWhisk = leftWhisk,
                                RightWhisk = rightWhisk,
                                LowOutliers = g.Where(x => x.SalaryMd < leftWhisk).Select(x => x.SalaryMd)
                                    .OrderBy(x => x).ToArray(),
                                HighOutliers = g.Where(x => x.SalaryMd > rightWhisk).Select(x => x.SalaryMd)
                                    .OrderBy(x => x).ToArray(),
                                ContractCount = ig.Count(),
                                SupplierCount = ig.Select(x => x.IcoOdberatele).Distinct().Count()
                            };
                        })
                        .ToList()
                );
            OdberateleRecalculationEnd = DateTime.Now;
        }

        private static void CalculateDodavatele(List<JobPrecalculated> distinctJobs)
        {
            DodavatelOverview = distinctJobs
                .SelectMany(j => j.IcoDodavatele, (precalculated, dodavatel) => new { precalculated, dodavatel })
                .GroupBy(j => j.dodavatel)
                .ToDictionary(g => g.Key, g =>
                    g.GroupBy(i => i.precalculated.JobGrouped)
                        .Select(ig =>
                        {
                            var salaryd = ig.Select(x => (double)x.precalculated.SalaryMd).ToList();
                            decimal dolniKvartil = (decimal)salaryd.LowerQuartile();
                            decimal horniKvartil = (decimal)salaryd.UpperQuartile();
                            decimal outlierRange = (horniKvartil - dolniKvartil) * 1.5m;
                            decimal maximum = g.Max(x => x.precalculated.SalaryMd);
                            decimal minimum = g.Min(x => x.precalculated.SalaryMd);
                            decimal leftWhisk = dolniKvartil - outlierRange;
                            if (leftWhisk < minimum)
                                leftWhisk = minimum;
                            decimal rightWhisk = horniKvartil + outlierRange;
                            if (rightWhisk > maximum)
                                rightWhisk = maximum;

                            return new JobStatistics()
                            {
                                Name = ig.Key,
                                Average = ig.Average(x => x.precalculated.SalaryMd),
                                Maximum = maximum,
                                Minimum = minimum,
                                Median = (decimal)salaryd.Median(),
                                DolniKvartil = (decimal)salaryd.LowerQuartile(),
                                HorniKvartil = (decimal)salaryd.UpperQuartile(),
                                LeftWhisk = leftWhisk,
                                RightWhisk = rightWhisk,
                                LowOutliers = g.Where(x => x.precalculated.SalaryMd < leftWhisk)
                                    .Select(x => x.precalculated.SalaryMd)
                                    .OrderBy(x => x).ToArray(),
                                HighOutliers = g.Where(x => x.precalculated.SalaryMd > rightWhisk)
                                    .Select(x => x.precalculated.SalaryMd)
                                    .OrderBy(x => x).ToArray(),
                                ContractCount = ig.Count(),
                                SupplierCount = ig.Select(x => x.precalculated.IcoOdberatele).Distinct().Count()
                            };
                        })
                        .ToList()
                );
            DodavateleRecalculationEnd = DateTime.Now;
        }

        private static (decimal net, decimal vat) FixSalaries(decimal? net, decimal? vat)
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

        public static List<JobStatistics> GetStatitstics()
        {
            return JobOverview;
        }

        public static List<JobStatistics> GetTagStatitstics(string jobName)
        {
            if (TagOverview.TryGetValue(jobName, out var result))
            {
                return result;
            }

            return result;
        }

        public static List<JobStatistics> GetOdberatelStatitstics(string ico)
        {
            if (OdberatelOverview.TryGetValue(ico, out var result))
            {
                return result;
            }

            return result;
        }

        public static List<JobStatistics> GetDodavatelStatistics(string ico)
        {
            if (DodavatelOverview.TryGetValue(ico, out var result))
            {
                return result;
            }

            return result;
        }
    }
}