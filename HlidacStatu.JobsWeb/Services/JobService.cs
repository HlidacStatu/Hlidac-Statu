using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using HlidacStatu.Ceny.Models;
using HlidacStatu.Entities;
using HlidacStatu.Repositories;
using Microsoft.AspNetCore.Http;

namespace HlidacStatu.Ceny.Services
{
    public static partial class JobService
    {
        private const string OstatniName = "-ostatní-";

        // make it "in memory", load it asynchronously, recalculate once a day?
        //private static List<JobPrecalculated> DistinctJobs { get; set; }
        private static readonly int _minimumPriceCount = 0;// 5;
        public static readonly int _minimumPriceCountInList = 5;

        public static List<JobPrecalculated> DistinctJobs { get; set; }

        private static Dictionary<YearlyStatisticsGroup.Key, YearlyStatisticsGroup> GlobalStats { get; } = new();


        //Meta informations
        public static DateTime LastRecalculationStarted { get; private set; }
        public static DateTime JobRecalculationEnd { get; private set; }
        public static DateTime TagRecalculationEnd { get; private set; }
        public static DateTime OdberateleRecalculationEnd { get; private set; }
        public static DateTime DodavateleRecalculationEnd { get; private set; }
        public static long RecalculationTimeMs { get; private set; }

        public static bool IsRecalculating { get; private set; }
        public static string LastError { get; private set; }


        public static async Task RecalculateAsync()
        {
            IsRecalculating = true;
            LastRecalculationStarted = DateTime.Now;

            var stopWatch = new Stopwatch();
            stopWatch.Start();

            try
            {
                LastError = "";
                var allJobs = await CenyRepo.GetAllCenyAsync();

                // Připraví plochou strukturu a odstraní případné duplicity u jobů, kde může být více dodavatelů
                // Také je možné rozšířit v tomto místě o odstranění duplicit
                // important to filter duplicates here, can be extended in future
                DistinctJobs = allJobs
                    .GroupBy(j => j.JobPk)
                    .Select(g =>
                    {
                        Entities.Cena firstJobOverview = g.FirstOrDefault();
                        string[] tags = firstJobOverview.Tags?.Split("|",
                            StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                        if (tags == null || tags.Length == 0)
                            tags = new[] { OstatniName };
                        return new JobPrecalculated()
                        {
                            AnalyzaName = firstJobOverview.AnalyzaName,
                            Year = firstJobOverview.Year,
                            IcoOdberatele = firstJobOverview.IcoOdberatele,
                            Polozka = firstJobOverview.Polozka,
                            SmlouvaId = firstJobOverview.SmlouvaId,
                            Tags = tags,
                            JobPk = g.Key,
                            // cenu bez daně nemáme vždy a nejsme ji schopni vždy správně spočítat
                            //PricePerUnit = firstJobOverview.PricePerUnit ?? (firstJobOverview.PricePerUnitVat / 1.21m),
                            PricePerUnitVat = firstJobOverview.PricePerUnitVat,
                            IcaDodavatelu = g.Select(i => i.IcoDodavatele).ToArray(),
                            ItemInAnalyseCreated = g.Max(max => max.Created),
                        };
                    }).ToList();

                //pro každou kombinaci roku a subjektu
                var uniqueYearSubjectCombinations = DistinctJobs.Select(x => new YearlyStatisticsGroup.Key()
                {
                    Obor = x.AnalyzaName,
                    Rok = x.Year
                }).Distinct().ToList();

                foreach (var key in uniqueYearSubjectCombinations)
                {
                    var yearlyStatisticsGroup = new YearlyStatisticsGroup()
                    {
                        KeyInfo = key,
                        JobStatistics = CalculateJobs(DistinctJobs, key),
                        TagStatistics = CalculateTags(DistinctJobs, key),
                        OdberateleStatistics = CalculateOdberatele(DistinctJobs, key),
                        DodavateleStatistics = CalculateDodavatele(DistinctJobs, key),
                    };

                    GlobalStats.TryAdd(key, yearlyStatisticsGroup);
                }
            }
            catch (Exception e)
            {
                LastError = e.Message;
            }

            stopWatch.Stop();
            RecalculationTimeMs = stopWatch.ElapsedMilliseconds;
            IsRecalculating = false;
        }

        public static SubjectYearDescription PerSubjectQuery(string subject, int rok)
        {
            if (string.IsNullOrEmpty(subject))
                return null;
            switch (subject.ToUpper())
            {
                case "IT":
                    return new SubjectYearDescription()
                    {
                        Query = $"oblast:IT AND podepsano:[{rok}-01-01 TO {rok + 1}-01-01]",
                        NiceName = $"IT služby za rok {rok}"
                    };
                case "DEMO":
                    return new SubjectYearDescription()
                    {
                        Query = $"oblast:IT AND podepsano:[2018-01-01 TO 2018-07-01]",
                        NiceName = "IT služby 01-06/2018 - DEMO "
                    };
                default:
                    return null;
            }
        }


        private static List<JobStatistics> CalculateJobs(List<JobPrecalculated> distinctJobs,
            YearlyStatisticsGroup.Key key)
        {
            return distinctJobs
                .Where(x => x.AnalyzaName == key.Obor && x.Year == key.Rok)
                .GroupBy(j => j.Polozka)
                .Select(g => new JobStatistics(g, g.Key))
                .Where(s => s.PriceCount >= _minimumPriceCount)
                .ToList();
        }

        private static Dictionary<string, List<JobStatistics>> CalculateTags(List<JobPrecalculated> distinctJobs,
            YearlyStatisticsGroup.Key key)
        {
            Dictionary<string, List<JobStatistics>> ret = distinctJobs
                .Where(x => x.AnalyzaName == key.Obor && x.Year == key.Rok) //filtruj analyzu
                .SelectMany(j => j.Tags, (precalculated, tag) => new { precalculated, tag = tag }) // vem vsechny tagy
                .GroupBy(j => j.precalculated.Polozka) //groupuj podle pracovni pozici
                .ToDictionary(g => g.Key, g =>
                    g.GroupBy(i => i.tag) //groupuj podle tabu u pracovni pozice
                        .Select(ig => new JobStatistics(ig.Select(x => x.precalculated), ig.Key))
                        .Where(s => s.PriceCount >= _minimumPriceCount)
                        .ToList()
                );

            return ret;
        }

        private static Dictionary<string, List<JobStatistics>> CalculateOdberatele(List<JobPrecalculated> distinctJobs,
            YearlyStatisticsGroup.Key key)
        {
            var results = distinctJobs
                .Where(x => x.AnalyzaName == key.Obor && x.Year == key.Rok)
                .GroupBy(j => j.IcoOdberatele)
                .ToDictionary(g => g.Key, g =>
                    g.GroupBy(i => i.Polozka)
                        .Select(ig => new JobStatistics(ig, ig.Key))
                        .Where(s => s.PriceCount >= _minimumPriceCount)
                        .ToList()
                );

            // clear empty records 
            return results.Where(x => x.Value.Count > 0).ToDictionary(x => x.Key, x => x.Value);
        }

        private static Dictionary<string, List<JobStatistics>> CalculateDodavatele(List<JobPrecalculated> distinctJobs,
            YearlyStatisticsGroup.Key key)
        {
            var results = distinctJobs
                .Where(x => x.AnalyzaName == key.Obor && x.Year == key.Rok)
                .SelectMany(j => j.IcaDodavatelu, (precalculated, dodavatel) => new { precalculated, dodavatel })
                .GroupBy(j => j.dodavatel)
                .ToDictionary(g => g.Key, g =>
                    g.GroupBy(i => i.precalculated.Polozka)
                        .Select(ig => new JobStatistics(ig.Select(x => x.precalculated), ig.Key))
                        .Where(s => s.PriceCount >= _minimumPriceCount)
                        .ToList()
                );
            return results.Where(x => x.Value.Count > 0).ToDictionary(x => x.Key, x => x.Value);
        }

        public static IEnumerable<JobPrecalculated> DistinctJobsForYearAndSubject(YearlyStatisticsGroup.Key key)
        {
            var distinctJobsForYearAndSubject = JobService.DistinctJobs
                .Where(x => x.AnalyzaName == key.Obor && x.Year == key.Rok);
            return distinctJobsForYearAndSubject;
        }
        public static List<JobStatistics> GetStatistics(YearlyStatisticsGroup.Key key)
        {
            return GlobalStats[key].JobStatistics;
        }

        public static List<JobStatistics> GetTagStatistics(string jobName, YearlyStatisticsGroup.Key key)
        {
            if (GlobalStats[key].TagStatistics.TryGetValue(jobName, out var result))
            {
                return result;
            }

            return result;
        }

        // jak to udělat správně? protože je to po letech? Jak to bude na FE?
        // a) zobrazovat roky a po vybrání roku obory a po vybrání oboru firmy?
        // b) zobrazovat firmy a po vybrání firmy nabídnout roky?
        public static List<(string ico, string nazev, int pocetCen)> GetOdberateleList(YearlyStatisticsGroup.Key key)
        {
            return GlobalStats[key].OdberateleStatistics.Keys.Select(k =>
                (
                    k,
                    FirmaRepo.NameFromIco(k, true),
                    DistinctJobs.Where(x => x.IcoOdberatele == k && x.AnalyzaName == key.Obor && x.Year == key.Rok)
                        .Select(x => x.JobPk).Distinct().Count()))
                .ToList();
        }

        public static List<JobStatistics> GetOdberatelStatistics(string ico, YearlyStatisticsGroup.Key key)
        {
            if (GlobalStats[key].OdberateleStatistics.TryGetValue(ico, out var result))
            {
                return result;
            }

            return result;
        }


        public static List<(string ico, string nazev, int pocetCen)> GetDodavateleList(YearlyStatisticsGroup.Key key)
        {
            return GlobalStats[key].DodavateleStatistics.Keys.Select(k =>
                (
                    k,
                    FirmaRepo.NameFromIco(k, true),
                    DistinctJobs.Where(x => x.IcaDodavatelu.Any(i => i == k) && x.AnalyzaName == key.Obor && x.Year == key.Rok)
                        .Select(x => x.JobPk).Distinct().Count()
                ))
                .ToList();
        }

        public static List<JobStatistics> GetDodavatelStatistics(string ico, YearlyStatisticsGroup.Key key)
        {
            if (GlobalStats[key].DodavateleStatistics.TryGetValue(ico, out var result))
            {
                return result;
            }

            return result;
        }



        public static List<JobStatistics> GetDodavatelForOdberatelStatistics(string icoDodavatel, string icoOdberatel, YearlyStatisticsGroup.Key key)
        {
            List<JobPrecalculated> jobs = GetDistinctJobs(key);
            IEnumerable<JobPrecalculated> jobsBetweenThem = jobs
                .Where(j => j.IcoOdberatele == icoOdberatel)
                .Where(j => j.IcaDodavatelu.Contains(icoDodavatel));

            List<JobStatistics> perPolozka = jobsBetweenThem
                .GroupBy(m => m.Polozka)
                .Select(ig => new JobStatistics(ig, ig.Key))
                .Where(s => s.PriceCount >= _minimumPriceCount)
                .ToList();

            return perPolozka;
        }

        public static List<JobPrecalculated> GetDistinctJobs(YearlyStatisticsGroup.Key key)
        {
            return DistinctJobs
                .Where(x => x.AnalyzaName == key.Obor && x.Year == key.Rok)
                .ToList();
        }

        public static List<JobPrecalculated> GetDistinctJobsAllAnalysis()
        {
            return DistinctJobs;
        }

        public static async Task<CenyCustomer.AccessDetail> HasAccess(this HttpContext context)
        {
            var key = TryFindKey(context);
            if (key?.IsDemo == true)
            {
                return new CenyCustomer.AccessDetail(CenyCustomer.AccessDetail.AccessDetailLevel.PRO);
            }

            if (context.User?.Identity?.IsAuthenticated == false)
                return CenyCustomer.AccessDetail.NoAccess();
            var username = context.User?.Identity?.Name;

            if (string.IsNullOrEmpty(username))
                return CenyCustomer.AccessDetail.NoAccess();
            if (key == null)
                return CenyCustomer.AccessDetail.NoAccess();

            return await HasAccess(username, key?.Obor, key.Value.Rok);
        }
        public static async Task<CenyCustomer.AccessDetail> HasAccess(this HttpContext context, string obor, int rok)
        {
            if (context.User?.Identity?.IsAuthenticated == false)
                return CenyCustomer.AccessDetail.NoAccess();
            var username = context.User?.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return CenyCustomer.AccessDetail.NoAccess();

            return await HasAccess(username, obor, rok);
        }
        public static async Task<CenyCustomer.AccessDetail> HasAccess(string username, string obor, int rok)
        {

            return await CenyCustomerRepo.HasAccessAsync(username, obor, rok).ConfigureAwait(false);
        }

        public static YearlyStatisticsGroup.Key? TryFindKey(this HttpContext context)
        {
            if (context.Items.TryGetValue("obor", out var oborObject))
            {
                if (context.Items.TryGetValue("rok", out var rokObject))
                {
                    string obor = oborObject as string;
                    int rok = rokObject as int? ?? 0;
                    if (!string.IsNullOrEmpty(obor) && rok > 0)
                    {
                        var key = new YearlyStatisticsGroup.Key()
                        {
                            Obor = obor,
                            Rok = rok
                        };
                        return key;
                    }
                }
            }

            return null;
        }
    }
}