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
                loadJobNames();
        }
        static void loadJobNames()
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

        public static async Task<List<JobOverview>> GetAllJobsWithRelatedDataAsync()
        {
            using (var db = new DbEntities())
            {
                return await db.JobsOverviewView.FromSqlInterpolated(
                        $@"SELECT t.smlouvaID, s.IcoOdberatele, d.Ico as IcoDodavatele, t.year, j.pk as JobPk, 
                                  j.jobGrouped, j.salaryMD, j.salaryMDVat, t.subject, j.tags
                             FROM InDocTables t
                             join InDocJobs j on t.pk = j.tablePK
                             join SmlouvyIds s on s.Id = t.smlouvaID
                             join SmlouvyDodavatele d on s.Id = d.SmlouvaId
                            where j.jobGrouped is not null")
                    .ToListAsync();
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
            jobraw = NormalizeTextNoDiacriticsLower(jobraw);

            foreach (var jobname in jobnames.Where(m => m.Subject == subject))
            {
                if (jobraw.Equals(jobname.JobRaw, StringComparison.Ordinal))
                    return jobname;
            }

            var bestDistance = int.MaxValue;
            InDocJobNames bestJob = null;
            int acceptableDistnace = 2;
            foreach (var jobname in jobnames.Where(m => m.Subject == subject))
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

        public static async Task<InDocJobs> Load(long jobPk)
        {
            await using (DbEntities db = new DbEntities())
            {
                var found = await db.InDocJobs.AsAsyncEnumerable().FirstOrDefaultAsync(m => m.Pk == jobPk);
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

                    var jobGroup = FindSimilar(jobSubject.ToUpper(), job.JobRaw);
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

                await db.SaveChangesAsync();
            }
        }

        public static async Task SaveAsync(IEnumerable<InDocJobs> jobs, bool dontChangeDates = false, bool rewriteAll = false)
        {
            foreach (var job in jobs)
            {
                await SaveAsync(job,dontChangeDates,rewriteAll);
            }

        }

        public static async Task Remove(long tablePk)
        {
            await using (DbEntities db = new DbEntities())
            {
                await db.Database.ExecuteSqlInterpolatedAsync($"Delete from InDocJobs where tablepk = {tablePk}");
            }
        }
        
        
    }
}