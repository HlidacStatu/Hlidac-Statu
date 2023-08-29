using System;
using System.Collections.Generic;
using System.Linq;

namespace HlidacStatu.DS.Api
{
    public partial class OcrWork
    {
        public enum DocTypes
        {
            Smlouva = 0,
            VerejnaZakazka = 1,
            Dataset = 2,
            Insolvence = 3
        }
        public class Task
        {
            public class Doc
            {
                public string prilohaId { get; set; }
                public string url { get; set; }
                public string origFilename { get; set; } = "file.bin";

                public Result result { get; set; } = null;
            }
            public string taskId { get; set; }
            public string parentDocId { get; set; }
            public Doc[] docs { get; set; }

            public DocTypes type { get; set; }

        }
    }
}