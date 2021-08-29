using HlidacStatu.Entities;

using Microsoft.EntityFrameworkCore;

using System.Linq;


namespace HlidacStatu.Repositories
{
    public partial class InDocTablesRepo
    {
        public static void Save(InDocTables tbl)
        {
            using (DbEntities db = new DbEntities())
            {
                db.InDocTables.Attach(tbl);
                if (tbl.Pk == 0)
                    db.Entry(tbl).State = EntityState.Added;
                else
                    db.Entry(tbl).State = EntityState.Modified;

                db.SaveChanges();
            }
        }

        public static InDocTables GetNextForCheck(string requestedBy)
        {
            using (DbEntities db = new DbEntities())
            {
                var tbl = db.InDocTables
                    .AsNoTracking()
                    .FirstOrDefault(m => m.Status == (int)InDocTables.CheckStatuses.WaitingInQueue);
                return tbl;
            }
        }

        public static void ChangeStatus(long tblPK, InDocTables.CheckStatuses status, string checkedBy, long checkElapsedTimeInMS)
        {
            using (DbEntities db = new DbEntities())
            {
                var tbl = db.InDocTables
                    .AsNoTracking()
                    .FirstOrDefault(m => m.Pk == tblPK);

                if (tbl != null)
                    ChangeStatus(tbl, status, checkedBy, checkElapsedTimeInMS);
            }
        }
        public static void ChangeStatus(InDocTables tbl, InDocTables.CheckStatuses status, string checkedBy, long checkElapsedTimeInMS)
        {
            using (DbEntities db = new DbEntities())
            {
                db.InDocTables.Attach(tbl);
                if (tbl.Pk == 0)
                    db.Entry(tbl).State = EntityState.Added;
                else
                    db.Entry(tbl).State = EntityState.Modified;

                tbl.CheckStatus = status;
                tbl.CheckedBy = checkedBy;
                tbl.CheckedDate = System.DateTime.Now;
                tbl.CheckElapsedInMs = (int)checkElapsedTimeInMS;

                db.SaveChanges();

            }
        }



    }
}
