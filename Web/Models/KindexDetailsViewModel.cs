using HlidacStatu.Lib.Analysis.KorupcniRiziko;

namespace HlidacStatu.Web.Models
{
    public class KindexDetailsViewModel
    {
        public KIndexData.Annual CurrYear { get; set; } 
        public KIndexData.KIndexParts Part { get; set; }
        public bool PercentValue { get; set; } = true;
        
        public string Jmeno { get; set; }
        public string Ico { get; set; }

        public KindexDetailsViewModel(KIndexData.Annual currYear, KIndexData.KIndexParts part, bool percentValue, string jmeno, string ico)
        {
            CurrYear = currYear;
            Part = part;
            PercentValue = percentValue;
            Jmeno = jmeno;
            Ico = ico;
        }
    }
}