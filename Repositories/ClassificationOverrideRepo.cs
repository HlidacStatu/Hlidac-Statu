using HlidacStatu.Entities;

using Microsoft.EntityFrameworkCore;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HlidacStatu.Repositories
{
    public static class ClassificationOverrideRepo
    {
        public static async Task SaveAsync(string username,
            string idSmlouvy,
            Smlouva.SClassification oldClassification,
            List<Smlouva.SClassification.Classification> newClassification)
        {
            await using DbEntities db = new DbEntities();
            db.ClassificationOverride.Add(
                new ClassificationOverride()
                {
                    IdSmlouvy = idSmlouvy,
                    Created = DateTime.Now,
                    CreatedBy = username,
                    OriginalCat1 = oldClassification.GetClassif().Length > 0
                        ? (int?)oldClassification.GetClassif()[0].TypeValue
                        : null,
                    OriginalCat2 = oldClassification.GetClassif().Length > 1
                        ? (int?)oldClassification.GetClassif()[1].TypeValue
                        : null,
                    CorrectCat1 = newClassification.Count > 0 ? (int?)newClassification[0].TypeValue : null,
                    CorrectCat2 = newClassification.Count > 1 ? (int?)newClassification[1].TypeValue : null
                });
            await db.SaveChangesAsync();
        }

        /// <summary>
        /// Returns null if there is no override for a classification
        /// </summary>
        /// <param name="idSmlouvy"></param>
        /// <returns></returns>
        public static async Task<ClassificationOverride> GetOverridenClassificationAsync(string idSmlouvy)
        {
            await using var db = new DbEntities();
            return await db.ClassificationOverride
                .AsNoTracking()
                .Where(c => c.IdSmlouvy == idSmlouvy)
                .OrderByDescending(c => c.Created)
                .FirstOrDefaultAsync();
        }
    }
}