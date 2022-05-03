using HlidacStatu.Entities;

using System;
using System.Threading.Tasks;

namespace HlidacStatu.XLib.Watchdogs
{
    public interface IWatchdogProcessor
        : IEmailFormatter
    {
        WatchDog OrigWD { get; }
        Task<Results> GetResultsAsync(DateTime? fromDate = null, DateTime? toDate = null, int? maxItems = null,
            string order = null);
        Task<DateTime> GetLatestRecAsync(DateTime toDate);

    }
    public interface IEmailFormatter
    {
        Task<RenderedContent> RenderResultsAsync(Results data, long numOfListed = 5);
    }
}
