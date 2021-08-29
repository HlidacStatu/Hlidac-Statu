using HlidacStatu.Entities;

using Microsoft.EntityFrameworkCore;

using System;
using System.Collections.Generic;
using System.Linq;

namespace HlidacStatu.Repositories
{
    public static class ClassificationOverrideRepo
    {
        public static void Save(string username,
            string idSmlouvy,
            Smlouva.SClassification oldClassification,
            List<Smlouva.SClassification.Classification> newClassification)
        {
            using (DbEntities db = new DbEntities())
            {
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
                db.SaveChanges();
            }
        }

        public static bool TryGetOverridenClassification(string idSmlouvy,
            out ClassificationOverride classification)
        {
            using (var db = new DbEntities())
            {
                classification = db.ClassificationOverride.AsNoTracking()
                    .Where(c => c.IdSmlouvy == idSmlouvy)
                    .OrderByDescending(c => c.Created)
                    .FirstOrDefault();
            }

            return classification != null;
        }
    }
}