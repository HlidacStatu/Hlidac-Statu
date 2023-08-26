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
            VerejnaZakazka = 1
        }
        public class Task
        {
            
            public string prilohaId { get; set; }
            public string url { get; set; }
            public string origFilename { get; set; } = "file.bin";
            public string parentDocId { get; set; }

            public DocTypes type { get; set; }

        }
    }
}