namespace HlidacStatu.JobsWeb.Models
{
    public class JobStatistics
    {
        public string JobName { get; set; }
        public decimal DolniKvartil { get; set; }
        public decimal HorniKvartil { get; set; }
        public decimal Minimum { get; set; }
        public decimal Maximum { get; set; }
        public decimal Average { get; set; }
        public decimal Median { get; set; }
        public int ContractCount { get; set; }
        public int SupplierCount { get; set; }
    }

    public class JobPrecalculated
    {
        public string SmlouvaId { get; set; }
        public string IcoOdberatele { get; set; }
        public string[] IcoDodavatele { get; set; }
    
        public int Year { get; set; }
    
        public int JobPk { get; set; }
        public string JobGrouped { get; set; }
        public decimal SalaryMd { get; set; }
        public decimal SalaryMdVat { get; set; }
        public string Subject { get; set; }
        
        
    }
}