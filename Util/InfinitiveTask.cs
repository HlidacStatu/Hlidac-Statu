using System;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace HlidacStatu.Util
{
    public static class InfinitiveTask
    {
        private static readonly ILogger _logger = Log.ForContext(typeof(InfinitiveTask));

        public enum TaskNextRunStatus
        {
            Continue,
            Stop
        }

        public static async Task RunAsync(
            Func<string, CancellationToken, Task<TaskNextRunStatus>> action,
            string tag,
            TimeSpan period,
            CancellationToken cancellationToken)
        {
            try
            {
                var status = TaskNextRunStatus.Continue;

                while (!cancellationToken.IsCancellationRequested && status == TaskNextRunStatus.Continue)
                {
                    status = cancellationToken.IsCancellationRequested
                        ? TaskNextRunStatus.Stop
                        : await action(tag, cancellationToken);

                    if (status == TaskNextRunStatus.Stop)
                        break;

                    await Task.Delay(period, cancellationToken);
                }
            }
            catch (Exception e)
            {
                _logger.Error(e, "Run got exception");
            }
        }

        public static Task[] CreateAndStartTasks(
            int numberOfTasks,
            string tag,
            TimeSpan period,
            Func<string, CancellationToken, Task<TaskNextRunStatus>> actionAsync,
            CancellationToken externalToken)
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
            var tasks = new Task[numberOfTasks];

            for (int i = 0; i < numberOfTasks; i++)
            {
                string ttag = tag.Contains("{0}") ? string.Format(tag, i) : tag;

                tasks[i] = RunAsync(actionAsync, ttag, period, cts.Token);
            }

            return tasks;
        }
    }
}