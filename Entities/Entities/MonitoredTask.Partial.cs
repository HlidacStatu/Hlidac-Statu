using System;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace HlidacStatu.Entities
{
    public partial class MonitoredTask
    {
        [NotMapped()]
        public TimeSpan MinIntervalBetweenUpdates { get; set; } = TimeSpan.FromSeconds(5);
        [NotMapped()]
        public DateTime LastTimeProgressUpdated { get; protected set; }  = DateTime.MinValue;

        protected MonitoredTask() { }
        
        public MonitoredTask(string application, string part)
        {
            this.Application = Devmasters.TextUtil.ShortenText(application, 200, "", "");
            this.Part = Devmasters.TextUtil.ShortenText(part, 500, "", "");
            this.ItemUpdated = this.Started.Value;
            this.Progress = 0;
            this.CallingStack = Devmasters.Log.StackReporter.GetCallingMethod(true,
                skipNamespaces: new[] { "System.Runtime.CompilerServices", "HlidacStatu.Entities.MonitoredTask", "MonitoredTask.Repositories.MonitoredTaskRepo" }
                );
        }

        public void ProgressUpdated()
        { 
        this.LastTimeProgressUpdated = DateTime.Now;
        }
    }
}