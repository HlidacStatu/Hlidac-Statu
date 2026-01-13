using HlidacStatu.Connectors;
using HlidacStatu.Entities;
using System;
using System.Text;
using System.Threading.Tasks;

namespace HlidacStatu.Repositories
{
    public static partial class MonitoredTaskRepo
    {
        public static MonitoredTask Create(MonitoredTask monitoredTask)
        {
            if (!string.IsNullOrEmpty(monitoredTask?.Exception))
                if (monitoredTask.Exception.Length > 50000)
                    monitoredTask.Exception = monitoredTask.Exception.Substring(0, 50000);
            using (DbEntities db = new DbEntities())
            {
                _ = db.MonitoredTasks.Add(monitoredTask);
                _ = db.SaveChanges();
            }

            return monitoredTask;
        }

        public static async Task<MonitoredTask> CreateAsync(MonitoredTask monitoredTask)
        {
            if (!string.IsNullOrEmpty(monitoredTask?.Exception))
                if (monitoredTask.Exception.Length > 50000)
                    monitoredTask.Exception = monitoredTask.Exception.Substring(0, 50000);

            await using (DbEntities db = new DbEntities())
            {
                _ = db.MonitoredTasks.Add(monitoredTask);
                _ = await db.SaveChangesAsync();
            }

            return monitoredTask;
        }

        public static MonitoredTask SetProgress(this MonitoredTask task, decimal progress)
        {
            DateTime now = DateTime.Now;
            task.ItemUpdated = now;

            task.Progress = SetProgress(progress);
            if ((now - task.LastTimeProgressUpdated) > task.MinIntervalBetweenUpdates)
            {
                //Console.WriteLine($"{now:mm.ss} in db");
                DirectDB.Instance.NoResult(
                    "update MonitoredTasks set ItemUpdated = @now, progress=@progress where pk = @pk",
                    new Microsoft.Data.SqlClient.SqlParameter("now", task.ItemUpdated),
                    new Microsoft.Data.SqlClient.SqlParameter("progress", task.Progress),
                    new Microsoft.Data.SqlClient.SqlParameter("pk", task.Pk)
                );
                task.ProgressUpdated();
            }

            return task;
        }

        public static MonitoredTask Update(this MonitoredTask task)
        {
            using (DbEntities db = new DbEntities())
            {
                _ = db.MonitoredTasks.Attach(task);
                db.Entry(task).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                _ = db.SaveChanges();
            }

            return task;
        }

        public static MonitoredTask Finish(this MonitoredTask task, bool success = true, Exception exception = null)
        {
            string except = "";
            if (exception?.GetType() == typeof(AggregateException))
            {
                StringBuilder sb = new StringBuilder(1024);
                foreach (Exception exInnerException in ((AggregateException)exception).Flatten().InnerExceptions)
                {
                    Exception exNestedInnerException = exInnerException;
                    do
                    {
                        if (!string.IsNullOrEmpty(exNestedInnerException.Message))
                        {
                            sb.AppendLine(exNestedInnerException.ToString());
                            sb.AppendLine("\n\n----------\n\n");
                        }

                        exNestedInnerException = exNestedInnerException.InnerException;
                    } while (exNestedInnerException != null);
                }

                except = sb.ToString();
            }
            else
                except = exception?.ToString();

            if (!string.IsNullOrEmpty(except))
                if (except.Length > 50000)
                    except = except.Substring(0, 50000);

            task.Progress = SetProgress(100);
            task.Finished = DateTime.Now;
            task.Success = success;
            task.Exception = except;
            task.ItemUpdated = DateTime.Now;

            using (DbEntities db = new DbEntities())
            {
                _ = db.MonitoredTasks.Attach(task);
                db.Entry(task).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                _ = db.SaveChanges();
            }

            return task;
        }

        private static decimal SetProgress(decimal progressInPercent)
        {
            if (progressInPercent < 0)
                progressInPercent = 0;
            if (progressInPercent > 100)
                progressInPercent = 100;

            return progressInPercent;
        }
    }
}