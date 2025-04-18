using System.Collections.Generic;

namespace WatchdogAnalytics.Models
{
    public class TableViewModel
    {
        public string SubjectName { get; set; }
        public List<JobStatistics> Statistics { get; set; }
        public JobStatistics StatisticsSummary { get; set; }
        public string LinkHref { get; set; }
        public bool HideDodavatelCount { get; set; } = false;
        //public string Caption { get; set; } = "Přehled cen - souhrn";
        public string FirstColumnName { get; set; } = "Pozice";
        public bool ShowFirstColumnNameUnderFirstRow { get; set; }
        public YearlyStatisticsGroup.Key? Key { get; set; }

        public bool LinkSamePage => LinkHref.Equals("#");
        //public bool CompareWithFirstLine { get; set; } = false;
    }
}