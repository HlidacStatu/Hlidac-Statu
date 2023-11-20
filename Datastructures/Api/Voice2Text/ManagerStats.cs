using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HlidacStatu.DS.Api.Voice2Text
{
    public class ManagerStats
    {
        public DateTime Started { get; set; }
        public TimeSpan Duration { get; set; }
        public long NumberOfTasks { get; set; }
        public TimeSpan AverageDuration { get; set; }
        public int ExternalErrors { get; set; }
        public int InternalErrors { get; set; }
    }
}
