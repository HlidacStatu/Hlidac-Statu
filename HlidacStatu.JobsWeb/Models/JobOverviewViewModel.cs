namespace HlidacStatu.Ceny.Models
{
    public class JobOverviewViewModel
    {
        public JobStatistics Statistics { get; set; }
        public string CustomText { get; set; }
        public YearlyStatisticsGroup.Key? Key { get; set; }
    }
}