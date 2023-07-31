namespace HlidacStatu.DS.Api
{
    public partial class BlurredPage
    {
        public class BpSave
        {
            public string smlouvaId { get; set; }

            public AnalyzedPdf[] prilohy { get; set; }

        }

    }
}
