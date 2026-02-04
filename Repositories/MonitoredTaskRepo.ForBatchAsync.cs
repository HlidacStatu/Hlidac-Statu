using HlidacStatu.Entities;
using System;
using System.Runtime.CompilerServices;
using Serilog;
using System.Threading.Tasks;

namespace HlidacStatu.Repositories
{
    public static partial class MonitoredTaskRepo
    {
        public class ForBatchAsync : MonitoredTask, Devmasters.Batch.IMonitorAsync, IDisposable
        {
            private bool disposedValue;
            private readonly ILogger _logger = Log.ForContext<ForBatch>();

            public ForBatchAsync(
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

            public async Task StartAsync()
            {
                this.Started = DateTime.Now;
                await MonitoredTaskRepo.CreateAsync(this);
                if (_logger != null)
                    _logger.Debug("Starting MonitoredTask in {application} {part}", this.Application, this.Part);
            }

            public async Task SetProgressAsync(decimal inPercent)
            {
                await MonitoredTaskRepo.SetProgressAsync(this, inPercent);
                if ((DateTime.Now - this.LastTimeProgressUpdated) > this.MinIntervalBetweenUpdates)
                {
                    if (_logger != null)
                        _logger.Debug("MonitoredTask in {application} {part} task {progress} % completed", this.Application, this.Part, inPercent);
                }
            }


            public async Task FinishAsync(bool success, Exception exception)
            {
                await MonitoredTaskRepo.FinishAsync(this, success, exception);
                if (_logger != null)
                    _logger.Debug("MonitoredTask in {application} {part} finished", this.Application, this.Part);
            }

            public async Task FinishAsync(params Exception[] exceptions)
            {
                bool success = exceptions == null || exceptions.Length == 0;
                await MonitoredTaskRepo.FinishAsync(this, success, success ? null : new AggregateException(exceptions));
                if (_logger != null)
                    _logger.Debug("MonitoredTask in {application} {part} finished", this.Application, this.Part);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        // TODO: dispose managed state (managed objects)
                        this.Finished = this.Finished ?? DateTime.Now;
                        this.Success = this.Progress < 100 ? false : true;
                        if (this.Success == false)
                            this.Exception = new ApplicationException("Canceled before end").ToString();
                        //_ = MonitoredTaskRepo.Update(this);
                    }

                    // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                    // TODO: set large fields to null
                    disposedValue = true;
                }
            }

            // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
            // ~ForBatch()
            // {
            //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            //     Dispose(disposing: false);
            // }

            public void Dispose()
            {
                // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }
        }

    }
}