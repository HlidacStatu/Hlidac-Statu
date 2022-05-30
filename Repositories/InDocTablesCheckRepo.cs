using HlidacStatu.Entities;
using System.Linq;
using Microsoft.EntityFrameworkCore;


namespace HlidacStatu.Repositories
{
    public class InDocTablesCheckRepo
    {
        public static void Save(InDocTablesCheck tbl)
        {
            using (DbEntities db = new DbEntities())
            {
                db.InDocTablesChecks.Attach(tbl);
                if (tbl.Pk == 0)
                    db.Entry(tbl).State = EntityState.Added;
                else
                    db.Entry(tbl).State = EntityState.Modified;

                db.SaveChanges();
            }
        }

        public static long? Exists(InDocTablesCheck tbl)
        {
            using (DbEntities db = new DbEntities())
            {
                if (tbl.Pk > 0)
                    return db.InDocTablesChecks
                        .AsNoTracking()
                        .Where(m => m.Pk == tbl.Pk)
                        .FirstOrDefault()
                        ?.Pk;

                else
                    return
                            db.InDocTablesChecks
                                .AsNoTracking()
                                .Where(m =>
                                    m.SmlouvaID == tbl.SmlouvaID
                                    && m.PrilohaHash == tbl.PrilohaHash
                                    && m.Page == tbl.Page
                                    && m.TableOnPage == tbl.TableOnPage
                                    && m.Algorithm == tbl.Algorithm
                                    && m.SubjectCheck == tbl.SubjectCheck
                                    && m.AlgorithmCheck == tbl.AlgorithmCheck
                                )
                                .FirstOrDefault()
                                ?.Pk;
            }
        }
    }
}
