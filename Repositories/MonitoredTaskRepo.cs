using HlidacStatu.Connectors;
using HlidacStatu.Entities;
using System;
using System.Threading.Tasks;

namespace HlidacStatu.Repositories
{
    public static partial class MonitoredTaskRepo
    {
         public static MonitoredTask Create(MonitoredTask monitoredTask)
        {

            using (DbEntities db = new DbEntities())
            {
                _ = db.MonitoredTasks.Add(monitoredTask);
                _ = db.SaveChanges();
            }
            return monitoredTask;

        }

        public static async Task<MonitoredTask> CreateAsync(MonitoredTask monitoredTask)
        {
            using (DbEntities db = new DbEntities())
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
                DirectDB.NoResult("update MonitoredTasks set ItemUpdated = @now, progress=@progress where pk = @pk",
                    new System.Data.SqlClient.SqlParameter("now", task.ItemUpdated),
                    new System.Data.SqlClient.SqlParameter("progress", task.Progress),
                    new System.Data.SqlClient.SqlParameter("pk", task.Pk)
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
            task.Progress = SetProgress(100);
            task.Finished = DateTime.Now;
            task.Success = success;
            task.Exception = exception?.ToString();
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