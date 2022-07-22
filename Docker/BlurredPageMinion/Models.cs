using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlurredPageMinion
{
    internal class Models
    {



        public class BpGet
        {
            public class BpGPriloha
            {
                public string uniqueId { get; set; }
                public string url { get; set; }
            }
            public string smlouvaId { get; set; }
            public BpGPriloha[] prilohy { get; set; }

        }

        public class BpSave
        {
            public string smlouvaId { get; set; }

            public HlidacStatu.Analysis.Page.Area.AnalyzedPdf[] prilohy { get; set; }


        }
    }
}
