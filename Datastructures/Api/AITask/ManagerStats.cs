using System;

namespace HlidacStatu.DS.Api.AITask
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
