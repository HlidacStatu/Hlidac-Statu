using Microsoft.EntityFrameworkCore;

namespace HlidacStatu.Entities.Views
{
    [Keyless]
    public class JobOverview
    {
        public string SmlouvaId { get; set; }
        public string IcoOdberatele { get; set; }
        public string IcoDodavatele { get; set; }
        
        public int Year { get; set; }
        
        public int JobPk { get; set; }
        public string JobGrouped { get; set; }
        public decimal? SalaryMd { get; set; }
        public decimal? SalaryMdVat { get; set; }
        public string Subject { get; set; }
        public string Tags { get; set; }
        
    }
}