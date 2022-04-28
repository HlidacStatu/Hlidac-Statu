using HlidacStatu.Entities;

using Microsoft.EntityFrameworkCore;

using System;
using System.Linq;

namespace HlidacStatu.Repositories
{
    public static class FirmaHintRepo
    {
        public static FirmaHint Load(string ico)
        {
            using (DbEntities db = new DbEntities())
            {
                return db.FirmaHint
                    .AsNoTracking()
                    .Where(m => m.Ico == ico)
                    .FirstOrDefault() ?? new FirmaHint() { Ico = ico };
                ;
            }
        }

        public static void Recalculate(this FirmaHint firmaHint)
        {
            var resMinDate = SmlouvaRepo.Searching.SimpleSearchAsync("ico:" + firmaHint.Ico, 1, 1, SmlouvaRepo.Searching.OrderResult.DateSignedAsc, platnyZaznam: true);
            if (resMinDate.Total > 0)
            {
                DateTime firstSmlouva = resMinDate.Results.First().datumUzavreni;
                DateTime zalozena = Firmy.Get(firmaHint.Ico).Datum_Zapisu_OR ?? new DateTime(1990, 1, 1);
                firmaHint.PocetDniKPrvniSmlouve = (int)((firstSmlouva - zalozena).TotalDays);
            }

        }

        public static void Save(this FirmaHint firmaHint)
        {
            using (DbEntities db = new DbEntities())
            {
                db.FirmaHint.Attach(firmaHint);
                if (db.FirmaHint.Any(m => m.Ico == firmaHint.Ico))
                    db.Entry(firmaHint).State = EntityState.Modified;
                else
                    db.Entry(firmaHint).State = EntityState.Added;

                db.SaveChanges();
            }
        }
    }
}
