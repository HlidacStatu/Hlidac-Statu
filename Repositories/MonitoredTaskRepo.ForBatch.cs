using HlidacStatu.Entities;
using System;
using System.Runtime.CompilerServices;

namespace HlidacStatu.Repositories
{
    public static partial class MonitoredTaskRepo
    {
        public class ForBatch : MonitoredTask, Devmasters.Batch.IMonitor
        {


            public ForBatch(
                string application = null,
                string part = null,
                [CallerMemberName] string callerMemberName = "",
                [CallerFilePath] string sourceFilePath = "",
                [CallerLineNumber] int sourceLineNumber = 0
)
                : base(application ?? Devmasters.IO.IOTools.GetExecutingFileName(), part ?? "")
            {
                if (string.IsNullOrEmpty(this.Part))
                {
                    var fn = System.IO.Path.GetFileName(sourceFilePath);
                    this.Part = Devmasters.TextUtil.ShortenText($"{fn}: {callerMemberName} ({sourceLineNumber})", 500, "", "");
                }
            }

            public void Start()
            {
                this.Started = DateTime.Now;
                _ = MonitoredTaskRepo.Create(this);
            }

            public void SetProgress(decimal inPercent)
            {
                _ = MonitoredTaskRepo.SetProgress(this, inPercent);
            }


            public void Finish(bool success, Exception exception)
            {
                _ = MonitoredTaskRepo.Finish(this, success, exception);
            }

            public void Finish(params Exception[] exceptions)
            {
                bool success = exceptions == null || exceptions.Length == 0;
                _ = MonitoredTaskRepo.Finish(this, success , success ? null : new AggregateException(exceptions));
            }
        }

    }
}