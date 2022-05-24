using System;
using HlidacStatu.Entities;
using Devmasters;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HlidacStatu.Entities.Views;
using Microsoft.EntityFrameworkCore;


namespace HlidacStatu.Repositories
{
    public partial class InDocJobsRepo
    {
        static List<InDocJobNames> jobnames = null;
        static InDocJobsRepo()
        {
            if (jobnames == null)
                LoadJobNames();
        }
        static void LoadJobNames()
        {
            using (DbEntities db = new DbEntities())
            {
                jobnames = db.InDocJobNames
                    .ToArray()
                    .Select(m => new InDocJobNames()
                    {
                        Pk = m.Pk,
                        JobGrouped = m.JobGrouped,
                        Subject = m.Subject,
                        Tags = m.Tags,
                        JobRaw = NormalizeTextNoDiacriticsLower(m.JobRaw)
                    })
                    .ToList();
            }
        }

        public static string NormalizeTextNoDiacriticsLower(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "";

            string normalizedFromText = TextUtil
                .RemoveDiacritics(TextUtil.NormalizeToBlockText(text))
                .KeepLettersNumbersAndSpace()
                .ToLower();
            normalizedFromText = Devmasters.TextUtil.ReplaceDuplicates(normalizedFromText, " ").Trim();

            return normalizedFromText;

        }

        static InDocJobNames FindSimilar(string subject, string jobraw)
        {
            string subjectNormalized = subject?.ToUpper() ?? ""; 
            jobraw = NormalizeTextNoDiacriticsLower(jobraw);

            foreach (var jobname in jobnames.Where(m => m.Subject == subjectNormalized))
            {
                if (jobraw.Equals(jobname.JobRaw, StringComparison.Ordinal))
                    return jobname;
            }

            var bestDistance = int.MaxValue;
            InDocJobNames bestJob = null;
            int acceptableDistnace = 2;
            foreach (var jobname in jobnames.Where(m => m.Subject == subjectNormalized))
            {
                if (Math.Abs(jobraw.Length - jobname.JobRaw.Length) > acceptableDistnace + 2)
                    continue;

                var distance = Validators.LevenshteinDistanceCompute(jobraw, jobname.JobRaw);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestJob = jobname;
                }
            }
            if (bestDistance <= acceptableDistnace)
                return bestJob;

            return null;
        }

        public static async Task<InDocJobs> LoadAsync(long jobPk)
        {
            await using (DbEntities db = new DbEntities())
            {
                var found = await db.InDocJobs.AsNoTracking().AsAsyncEnumerable().FirstOrDefaultAsync(m => m.Pk == jobPk);
                return found;
            }
        }


        public static async Task SaveAsync(InDocJobs job, bool dontChangeDates = false, bool rewriteAll = false, string forceSubject = null)
        {
            await using (DbEntities db = new DbEntities())
            {
                //find jobGroup
                if (string.IsNullOrEmpty(job.JobGrouped) || rewriteAll)
                {
                    string jobSubject = db.InDocTables.AsQueryable().FirstOrDefault(m => m.Pk == job.TablePk)?.Klasifikace;
                    if (forceSubject != null)
                        jobSubject = forceSubject;

                    var classif = HlidacStatu.Connectors.External.TablePolozkaClassif.GetClassification(job.JobRaw);
                    if (classif?.Count() > 0)
                    {
                        job.JobGrouped = classif.First().Class;
                        job.Tags = null;
                        if (classif.First().Tags?.Count()>0)
                            job.Tags = String.Join('|', classif.First().Tags);
                    }
                    else
                    {
                        var jobGroup = FindSimilar(jobSubject, job.JobRaw);
                        if (jobGroup != null)
                        {
                            job.JobGrouped = jobGroup.JobGrouped;
                            job.Tags = jobGroup.Tags;
                        }
                        else
                        {
                            job.JobGrouped = null;
                            job.Tags = null;
                        }
                    }
                }
                if (dontChangeDates == false)
                    job.Created = DateTime.Now;

                
                job.NormalizePrices();

                db.InDocJobs.Attach(job);
                if (job.Pk == 0)
                {
                    db.Entry(job).State = EntityState.Added;
                }
                else
                    db.Entry(job).State = EntityState.Modified;
                try
                {
                    await db.SaveChangesAsync();

                }
                catch (Exception e)
                {

                    throw;
                }
            }
        }

        public static async Task SaveAsync(IEnumerable<InDocJobs> jobs, bool dontChangeDates = false, bool rewriteAll = false)
        {
            foreach (var job in jobs)
            {
                await SaveAsync(job,dontChangeDates,rewriteAll);
            }

        }

        public static async Task RemoveAsync(long tablePk)
        {
            await using (DbEntities db = new DbEntities())
            {
                await db.Database.ExecuteSqlInterpolatedAsync($"Delete from InDocJobs where tablepk = {tablePk}");
            }
        }
        
        
    }
}