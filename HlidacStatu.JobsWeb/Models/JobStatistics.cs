namespace HlidacStatu.JobsWeb.Models
{
    public class JobStatistics
    {
        public string JobName { get; set; }
        public int DolniKvartil { get; set; }
        public int HorniKvartil { get; set; }
        public int Minimum { get; set; }
        public int Maximum { get; set; }
        public int Average { get; set; }
        public int Median { get; set; }
        public int ContractCount { get; set; }
        public int SupplierCount { get; set; }
    }
}