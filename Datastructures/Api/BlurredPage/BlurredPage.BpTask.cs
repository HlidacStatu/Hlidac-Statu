namespace HlidacStatu.DS.Api
{
    public partial class BlurredPage
    {

        public class BpTask
        {
            public class BpGPriloha
            {
                public string uniqueId { get; set; }
                public string url { get; set; }
            }
            public string smlouvaId { get; set; }
            public BpGPriloha[] prilohy { get; set; }

        }

    }
}
