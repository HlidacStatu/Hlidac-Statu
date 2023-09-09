using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;

namespace HlidacStatuApi.Code
{
    public class ShortExpirationTimeAttribute : JobFilterAttribute, Hangfire.States.IApplyStateFilter
    {
        public void OnStateApplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
        {
            
            context.JobExpirationTimeout = TimeSpan.FromHours(12);
        }

        public void OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
        {
            context.JobExpirationTimeout = TimeSpan.FromHours(12);

        }
    }
}
