using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HlidacStatu.Lib.Data.External.Camelot
{
    public class CamelotStatistics
    {
        public long MaxThreads { get; set; } 
        public long Calls { get; set; }
        public long ParsedFiles { get; set; }
        public DateTime Started { get; set; }
        public long CurrentThreads { get; set; }
    }
}
