using System;

namespace WatchdogAnalytics.Models
{
    public class JobPrecalculated
    {
        public string SmlouvaId { get; set; }
        public string IcoOdberatele { get; set; }
        public string[] IcaDodavatelu { get; set; }
    
        public int Year { get; set; }
    
        public long JobPk { get; set; }
        public string Polozka { get; set; }
        public decimal PricePerUnit { get; set; }
        public decimal PricePerUnitVat { get; set; }
        public string AnalyzaName { get; set; }
        public string[] Tags { get; set; }

        public DateTime? ItemInAnalyseCreated { get; set; }
    }
}