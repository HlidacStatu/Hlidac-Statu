using System;
using HlidacStatu.Entities;
using Devmasters;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;


namespace HlidacStatu.Repositories
{
    public class InDocJobsRepo
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

        public static InDocJobNames FindSimilar(string subject, string jobraw)
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

                var distance = HlidacStatu.Util.TextTools.LevenshteinDistanceCompute(jobraw, jobname.JobRaw);
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
                var found = await db.InDocJobs.AsNoTracking().AsAsyncEnumerable()
                    .FirstOrDefaultAsync(m => m.Pk == jobPk);
                return found;
            }
        }

        public static bool IsTableHavingItJob(long tablePk)
        {
            using DbEntities db = new DbEntities();

            var job = db.InDocJobs.AsNoTracking()
                .FirstOrDefault(j => j.TablePk == tablePk
                                     && (j.Unit == InDocJobs.MeasureUnit.Day || j.Unit == InDocJobs.MeasureUnit.Hour)
                                     && j.PriceVATCalculated != null);

            if (job is not null)
            {
                return true;
            }

            return false;
        }


        public static async Task SaveAsync(InDocJobs job, bool dontChangeDates = false, bool rewriteAll = false,
            string forceSubject = null)
        {
            await using (DbEntities db = new DbEntities())
            {
                //find jobGroup
                if (string.IsNullOrEmpty(job.JobGrouped) || rewriteAll)
                {
                    string jobSubject = db.InDocTables.AsQueryable().FirstOrDefault(m => m.Pk == job.TablePk)
                        ?.Klasifikace;
                    if (forceSubject != null)
                        jobSubject = forceSubject;

                    var classifications = HlidacStatu.Connectors.External.TablePolozkaClassif.GetClassification(job.JobRaw);
                    if (classifications?.Count() > 0)
                    {
                        var maxScore = classifications.Max(c => c.Prediction);
                        var topClassifications = classifications.Where(c => c.Prediction == maxScore).ToList();
                        
                        if (topClassifications.Count() > 0)
                        {
                            job.JobGrouped = topClassifications[0].Class;
                        }

                        if (topClassifications.Count() > 1)
                        {
                            job.JobGrouped2 = topClassifications[1].Class;
                        }

                        if (topClassifications.Count() > 2)
                        {
                            job.JobGrouped3 = topClassifications[2].Class;
                        }
                        
                        job.Tags = null;
                        if (topClassifications.First().Tags?.Count() > 0)
                            job.Tags = String.Join('|', topClassifications.First().Tags);
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

                await db.SaveChangesAsync();
            }
        }

        public static async Task SaveAsync(IEnumerable<InDocJobs> jobs, bool dontChangeDates = false,
            bool rewriteAll = false)
        {
            foreach (var job in jobs)
            {
                await SaveAsync(job, dontChangeDates, rewriteAll);
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