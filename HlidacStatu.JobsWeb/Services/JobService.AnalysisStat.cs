using System.Linq;
using WatchdogAnalytics.Models;

namespace WatchdogAnalytics.Services
{
    public static partial class JobService
    {
        public class AnalysisStat
        {
            public AnalysisStat(YearlyStatisticsGroup.Key oblast)
            {
                var stat = JobService.GetStatistics(oblast);

                var distinctJobsForYearAndSubject = JobService.DistinctJobsForYearAndSubject(oblast);
                PocetSmluv = distinctJobsForYearAndSubject
                    .Select(m => m.SmlouvaId).Distinct().Count();
                PocetZadavatelu = distinctJobsForYearAndSubject
                    .Select(m => m.IcoOdberatele).Distinct().Count();
                PocetDodavatelu = distinctJobsForYearAndSubject
                    .SelectMany(m => m.IcaDodavatelu).Distinct().Count();
                PocetCen = distinctJobsForYearAndSubject
                    .Count();

            }

            public int PocetSmluv { get; }
            public int PocetZadavatelu { get; }
            public int PocetDodavatelu { get; }
            public int PocetCen { get; }
        }
    }
}
