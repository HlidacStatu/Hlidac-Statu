using System;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace HlidacStatu.Util
{
    public class InfinitiveTask
    {
        private static readonly ILogger _logger = Log.ForContext(typeof(InfinitiveTask));
        public enum TaskNextRunStatus
        {
            Continue,
            Stop
        }
        public static async Task RunAsync(Func<string, TaskNextRunStatus> action, string tag, TimeSpan period, CancellationToken cancellationToken)
        {
            try
            {
                TaskNextRunStatus status = TaskNextRunStatus.Continue;

                while (!cancellationToken.IsCancellationRequested && status == TaskNextRunStatus.Continue)
                {
                    if (!cancellationToken.IsCancellationRequested)
                        status = action(tag);
                    else
                        status = TaskNextRunStatus.Stop;

                    if (status == TaskNextRunStatus.Stop)
                        break;
                    await Task.Delay(period, cancellationToken);

                }
            }
            catch (Exception e)
            {
                _logger.Error(e, "Run got exception ");
                return;

                //throw;
            }
        }

        public static void Run(Func<string, TaskNextRunStatus> action, string tag, TimeSpan period, CancellationToken cancellationToken)
        {
            try
            {
                TaskNextRunStatus status = TaskNextRunStatus.Continue;
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (!cancellationToken.IsCancellationRequested)
                        status = action(tag);
                    else
                        status = TaskNextRunStatus.Stop;

                    if (status == TaskNextRunStatus.Stop)
                    {
                        _logger.Information($"InfinitiveTask.Run: ending {tag} because of status STOP");
                        break;
                    }
                    Thread.Sleep(period);
                }

            }
            catch (Exception e)
            {
                _logger.Error(e, "RunSync got exception ");
                return;

                //throw;
            }
            finally
            {
                _logger.Information($"InfinitiveTask.Run: ended {tag} ");
            }
        }


        public static CancellationTokenSource CreateAndStartTasks(int numberOfThreads,
            string tag, TimeSpan period,
            Func<string, TaskNextRunStatus> action,
            ThreadPriority threadPriority = ThreadPriority.BelowNormal
            )
        {
            var cancelSource = new CancellationTokenSource();
            for (int i = 0; i < numberOfThreads; i++)
            {
                string ttag = tag;
                if (ttag.Contains("{0}"))
                    ttag = string.Format(ttag, i);
                Thread t = new Thread(
                    () => Run(action, ttag, period, cancelSource.Token)
                );
                t.Priority = threadPriority;
                t.Start();
            }
            return cancelSource;
        }
    }
}
