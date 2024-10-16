﻿using System;

namespace HlidacStatu.DS.Api
{
    public partial class OcrWork
    {
        public class OcrWorkStat
        {
            public Devmasters.DT.DateInterval TimePeriod { get; set; }
            public TimeSpan Duration { get; set; }
            public OcrWork.DocTypes? Type { get; set; }
            public long Tasks { get; set; }
            public long Files { get; set; }
            public long FullOCR { get; set; 
            }
            public long ExternalErrors { get; set; }
            public long InternalErrors { get; set; }

        }
        public class TaskOptions
        {
            public bool forceOCR { get; set; } = false;
            public bool ocrMissingOnly { get; set; } = true;
            public int? ocrLengthLessThan { get; set; } = null;

            public bool forceClassification { get; set; } = false;
            public bool forceTablesMining { get; set; } = false;
            public bool forceBlurredPages { get; set; } = false;


            private static TaskOptions _default = new TaskOptions();
            public static TaskOptions Default { get => _default; }

        }

        public enum TaskPriority
        {
            Lowest = 0,
            Low = 5,
            Standard = 10,
            High = 20,
            Immediate = 100,
            Critical = 999,
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
            public TaskOptions options { get; set; } = TaskOptions.Default;
            public DocTypes type { get; set; }

        }
    }
}