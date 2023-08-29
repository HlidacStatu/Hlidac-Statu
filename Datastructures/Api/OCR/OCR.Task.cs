using System;
using System.Collections.Generic;
using System.Linq;

namespace HlidacStatu.DS.Api
{
    public partial class OcrWork
    {
        public class ItemOption
        {
            public bool force { get; set; } = false;
            public bool missingOnly { get; set; } = true;
            public int? lengthLessThan { get; set; } = null;

            private static ItemOption _default = new ItemOption();
            public static ItemOption Default { get => _default; }

        }

        public enum DocTypes
        {
            Smlouva = 0,
            VerejnaZakazka = 3,
            Dataset = 2,
            Insolvence = 1
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