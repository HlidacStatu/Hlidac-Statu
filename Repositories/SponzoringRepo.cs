using System;
using System.Collections.Generic;
using System.Linq;
using Devmasters.Collections;
using HlidacStatu.Entities;
using HlidacStatu.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Nest;

namespace HlidacStatu.Repositories
{
    public static class SponzoringRepo
    {
        public static IEnumerable<Sponzoring> GetByDarce(int osobaId)
        {
            using (DbEntities db = new DbEntities())
            {
                return db.Sponzoring.AsQueryable()
                    .Where(s => s.OsobaIdDarce == osobaId);
            }
        }

        public static IEnumerable<Sponzoring> GetByDarce(string icoDarce)
        {
            using (DbEntities db = new DbEntities())
            {
                return db.Sponzoring.AsQueryable()
                    .Where(s => s.IcoDarce == icoDarce);
            }
        }

        public static IEnumerable<Sponzoring> GetByPrijemce(int osobaId)
        {
            using (DbEntities db = new DbEntities())
            {
                return db.Sponzoring.AsQueryable()
                    .Where(s => s.OsobaIdPrijemce == osobaId);
            }
        }

        public static IEnumerable<Sponzoring> GetByPrijemce(string icoPrijemce)
        {
            using (DbEntities db = new DbEntities())
            {
                return db.Sponzoring.AsQueryable()
                    .Where(s => s.IcoPrijemce == icoPrijemce);
            }
        }

        public static Sponzoring CreateOrUpdate(Sponzoring sponzoring, string user)
        {
            using (DbEntities db = new DbEntities())
            {
                return CreateSponzoring(sponzoring, user, db);
            }
        }

        private static Sponzoring CreateSponzoring(Sponzoring sponzoring, string user, DbEntities db)
        {
            if (sponzoring.OsobaIdDarce == 0
                && string.IsNullOrWhiteSpace(sponzoring.IcoDarce))
                throw new Exception(
                    "Cant attach sponzoring to a person or to a company since their reference is empty");

            sponzoring.Created = DateTime.Now;
            sponzoring.Edited = DateTime.Now;
            sponzoring.UpdatedBy = user;

            db.Sponzoring.Add(sponzoring);
            db.SaveChanges();

            AuditRepo.Add(Audit.Operations.Create, user, sponzoring, null);
            return sponzoring;
        }

        private static Sponzoring UpdateSponzoring(Sponzoring sponzoringToUpdate, Sponzoring sponzoring, string user,
            DbEntities db)
        {
            var sponzoringOriginal = sponzoringToUpdate.ShallowCopy();

            if (!string.IsNullOrWhiteSpace(sponzoring.IcoDarce))
                sponzoringToUpdate.IcoDarce = sponzoring.IcoDarce;
            if (sponzoring.OsobaIdDarce > 0)
                sponzoringToUpdate.OsobaIdDarce = sponzoring.OsobaIdDarce;

            sponzoringToUpdate.Edited = DateTime.Now;
            sponzoringToUpdate.UpdatedBy = user;

            sponzoringToUpdate.DarovanoDne = sponzoring.DarovanoDne;
            sponzoringToUpdate.Hodnota = sponzoring.Hodnota;
            sponzoringToUpdate.IcoPrijemce = sponzoring.IcoPrijemce;
            sponzoringToUpdate.OsobaIdPrijemce = sponzoring.OsobaIdPrijemce;
            sponzoringToUpdate.Popis = sponzoring.Popis;
            sponzoringToUpdate.Typ = sponzoring.Typ;
            sponzoringToUpdate.Zdroj = sponzoring.Zdroj;

            db.SaveChanges();

            AuditRepo.Add<Sponzoring>(Audit.Operations.Update, user, sponzoringToUpdate, sponzoringOriginal);
            return sponzoringToUpdate;
        }

        public static void MergeDonatingOsoba(int originalOsobaId, int duplicateOsobaId, string user)
        {
            if (originalOsobaId == 0 && duplicateOsobaId == 0)
                throw new ArgumentException("Id osoby nesmí být 0");

            using (DbEntities db = new DbEntities())
            {
                var sponzoring = db.Sponzoring.AsEnumerable().Where(s => s.OsobaIdDarce == duplicateOsobaId).ToList();

                foreach (var donation in sponzoring)
                {
                    var donationBackup = donation.ShallowCopy();

                    donation.OsobaIdDarce = originalOsobaId;
                    donation.Edited = DateTime.Now;
                    AuditRepo.Add(Audit.Operations.Update, user, donation, donationBackup);
                }

                db.SaveChanges();
            }
        }

        public static void Delete(Sponzoring sponzoring, string user)
        {
            if (sponzoring.Id > 0)
            {
                using (DbEntities db = new DbEntities())
                {
                    db.Sponzoring.Attach(sponzoring);
                    db.Entry(sponzoring).State = EntityState.Deleted;
                    AuditRepo.Add(Audit.Operations.Delete, user, sponzoring, null);

                    db.SaveChanges();
                }
            }
        }


        public static IEnumerable<Sponsors.Sponzorstvi<IBookmarkable>> AllTimeTopSponzorsPerStrana(string strana,
            int top = int.MaxValue, int? minYear = null)
        {
            var o = AllTimeTopSponzorsPerStranaOsoby(strana, top * 50, minYear);
            var f = AllTimeTopSponzorsPerStranaFirmy(strana, top * 50, minYear);

            return o.Select(m => (Sponsors.Sponzorstvi<IBookmarkable>) m)
                .Union(f.Select(m => (Sponsors.Sponzorstvi<IBookmarkable>) m))
                .OrderByDescending(m => m.CastkaCelkem)
                .Take(top);
        }

        public static IEnumerable<Sponsors.Sponzorstvi<Osoba>> AllTimeTopSponzorsPerStranaOsoby(string strana,
            int top = int.MaxValue, int? minYear = null)
        {
            minYear = minYear ?? DateTime.Now.Year - 10;
            return AllSponzorsPerYearPerStranaOsoby.Get()
                    .Where(m => m.Strana == strana && m.Rok >= minYear)
                    .GroupBy(k => k.Sponzor, sp => sp, (k, sp) => new Sponsors.Sponzorstvi<Osoba>()
                    {
                        Sponzor = k,
                        CastkaCelkem = sp.Sum(m => m.CastkaCelkem),
                        Rok = 0,
                        Strana = strana
                    })
                    .OrderByDescending(o => o.CastkaCelkem)
                    .Take(top)
                ;
        }

        public static IEnumerable<Sponsors.Sponzorstvi<Firma>> AllTimeTopSponzorsPerStranaFirmy(string strana,
            int top = int.MaxValue, int? minYear = null)
        {
            minYear = minYear ?? DateTime.Now.Year - 10;

            return AllSponzorsPerYearPerStranaFirmy.Get()
                    .Where(m => m.Strana == strana && m.Rok >= minYear)
                    .GroupBy(k => k.Sponzor, sp => sp, (k, sp) => new Sponsors.Sponzorstvi<Firma>()
                    {
                        Sponzor = k,
                        CastkaCelkem = sp.Sum(m => m.CastkaCelkem),
                        Rok = 0,
                        Strana = strana
                    })
                    .OrderByDescending(o => o.CastkaCelkem)
                    .Take(top)
                ;
        }

        public static Devmasters.Cache.LocalMemory.LocalMemoryCache<IEnumerable<Strany.StranaPerYear>>
            GetStranyPerYear
                = new Devmasters.Cache.LocalMemory.LocalMemoryCache<IEnumerable<Strany.StranaPerYear>>(
                    TimeSpan.FromDays(4), "sponzori_stranyPerYear", (obj) =>
                    {
                        using (DbEntities db = new DbEntities())
                        {
                            var resultO = db.Sponzoring.AsQueryable()
                                .Where(m => m.DarovanoDne != null && m.OsobaIdDarce != null)
                                .ToArray()
                                .Select(m => new {rok = m.DarovanoDne.Value.Year, oe = m})
                                .GroupBy(g => new {rok = g.rok, strana = g.oe.JmenoPrijemce()}, oe => oe.oe, (r, oe) =>
                                    new Strany.StranaPerYear()
                                    {
                                        Rok = r.rok,
                                        Strana = r.strana,
                                        Osoby = new Strany.AggSum()
                                            {Num = oe.Count(), Sum = oe.Sum(s => s.Hodnota) ?? 0}
                                    });

                            var resultF = db.Sponzoring.AsQueryable()
                                .Where(m => m.DarovanoDne != null && m.IcoDarce != null)
                                .ToArray()
                                .Select(m => new {rok = m.DarovanoDne.Value.Year, oe = m})
                                .GroupBy(g => new {rok = g.rok, strana = g.oe.JmenoPrijemce()}, oe => oe.oe, (r, oe) =>
                                    new Strany.StranaPerYear()
                                    {
                                        Rok = r.rok,
                                        Strana = r.strana,
                                        Firmy = new Strany.AggSum()
                                            {Num = oe.Count(), Sum = oe.Sum(s => s.Hodnota) ?? 0}
                                    });

                            var roky = resultO.FullOuterJoin(resultF, o => o, f => f,
                                (o, f, k) => new Strany.StranaPerYear()
                                {
                                    Strana = k.Strana,
                                    Rok = k.Rok,
                                    Osoby = o?.Osoby ?? new Strany.AggSum(),
                                    Firmy = f?.Firmy ?? new Strany.AggSum()
                                }, cmp: new Strany.StranaPerYear.PerYearStranaEquality()
                            );

                            return roky;
                        }
                    });

        public static IEnumerable<Strany.StranaPerYear> StranaPerYears(string strana)
        {
            return GetStranyPerYear.Get().Where(m => m.Strana == strana);
        }

        public static Strany.StranaPerYear StranaPerYears(string strana, int year)
        {
            var ret = GetStranyPerYear.Get().Where(m => m.Strana == strana && m.Rok == year).FirstOrDefault();
            if (ret == null)
                ret = new Strany.StranaPerYear() {Strana = strana, Rok = year};
            return ret;
        }

        public static Devmasters.Cache.LocalMemory.LocalMemoryCache<IEnumerable<Sponsors.Sponzorstvi<Osoba>>>
            AllSponzorsPerYearPerStranaOsoby
                = new Devmasters.Cache.LocalMemory.LocalMemoryCache<IEnumerable<Sponsors.Sponzorstvi<Osoba>>>(
                    TimeSpan.FromDays(4), "ucty_index_allSponzoriOsoby", (obj) =>
                    {
                        List<Sponsors.Sponzorstvi<Osoba>> result = new List<Sponsors.Sponzorstvi<Osoba>>();
                        using (DbEntities db = new DbEntities())
                        {
                            var res = db.Sponzoring
                                .Where(OsobaRepo.SponzoringLimitsPredicate)
                                .Join(db.Osoba, oe => oe.OsobaIdDarce, o => o.InternalId,
                                    (oe, o) => new {osoba = o, oe = oe})
                                .OrderByDescending(o => o.oe.Hodnota)
                                .ToArray()
                                .GroupBy(
                                    g => new
                                    {
                                        osoba = g.osoba, rok = g.oe.DarovanoDne.Value.Year,
                                        strana = g.oe.JmenoPrijemce()
                                    }, oe => oe.oe, (o, oe) => new Sponsors.Sponzorstvi<Osoba>()
                                    {
                                        Sponzor = o.osoba,
                                        CastkaCelkem = oe.Sum(e => e.Hodnota) ?? 0,
                                        Rok = o.rok,
                                        Strana = o.strana
                                    })
                                .OrderByDescending(o => o.CastkaCelkem);
                            result.AddRange(res);
                        }

                        return result;
                    });

        public static Devmasters.Cache.LocalMemory.LocalMemoryCache<IEnumerable<Sponsors.Sponzorstvi<Firma>>>
            AllSponzorsPerYearPerStranaFirmy
                = new(
                    TimeSpan.FromDays(4), "sponzori_index_allSponzoriFirmy", (obj) =>
                    {
                        List<Sponsors.Sponzorstvi<Firma>> result = new();
                        using (DbEntities db = new DbEntities())
                        {
                            var res = db.Sponzoring.AsQueryable()
                                .Where(s => s.IcoDarce != null)
                                .OrderByDescending(o => o.Hodnota)
                                .ToArray()
                                .GroupBy(
                                    g => new
                                    {
                                        Ico = g.IcoDarce, rok = g.DarovanoDne.Value.Year, strana = g.JmenoPrijemce()
                                    }, oe => oe, (o, oe) => new Sponsors.Sponzorstvi<Firma>()
                                    {
                                        Sponzor = FirmaRepo.FromIco(o.Ico),
                                        CastkaCelkem = oe.Sum(e => e.Hodnota) ?? 0,
                                        Rok = o.rok,
                                        Strana = o.strana
                                    })
                                .OrderByDescending(o => o.CastkaCelkem);
                            result.AddRange(res);
                        }

                        return result;
                    });


        public static string[] VelkeStrany = new string[]
        {
            "ANO", "ODS", "ČSSD", "Česká pirátská strana", "KSČM",
            "SPD", "STAN", "KDU-ČSL", "TOP 09",
            "Svobodní", "Strana zelených"
        };

        public static string[] TopStrany = VelkeStrany.Take(9).ToArray();

        public static int DefaultLastSponzoringYear = DateTime.Now.AddMonths(-7).AddYears(-1).Year;

        static SponzoringRepo()
        {
            var maxYear = VelkeStrany.Select(
                    strana => StranaPerYears(strana).Max(m => m.Rok)
                )
                .Max();
            DefaultLastSponzoringYear = maxYear;
        }


        public class Strany
        {
            public class AggSum
            {
                public int Num { get; set; } = 0;
                public decimal Sum { get; set; } = 0;
            }

            public class PerStrana : AggSum
            {
                public string Strana { get; set; }
            }

            public class StranaPerYear
            {
                public string Strana { get; set; }

                public int Rok { get; set; }
                public AggSum Osoby { get; set; } = new AggSum();

                public AggSum Firmy { get; set; } = new AggSum();

                public decimal? TotalKc
                {
                    get { return Osoby?.Sum + Firmy?.Sum; }
                }


                public class PerYearStranaEquality : IEqualityComparer<StranaPerYear>
                {
                    public bool Equals(StranaPerYear x, StranaPerYear y)
                    {
                        if (x == null || y == null)
                            return false;

                        return x.Rok == y.Rok && x.Strana == y.Strana;
                    }

                    public int GetHashCode(StranaPerYear obj)
                    {
                        //http://stackoverflow.com/a/4630550
                        return
                            new
                            {
                                obj.Rok,
                                obj.Strana
                            }.GetHashCode();
                    }
                }
            }
        }
    }
}