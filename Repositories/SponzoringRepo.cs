using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using HlidacStatu.Connectors;
using HlidacStatu.Entities;
using HlidacStatu.Entities.Views;
using HlidacStatu.Extensions;
using HlidacStatu.Util;
using Microsoft.EntityFrameworkCore;
using ZiggyCreatures.Caching.Fusion;

namespace HlidacStatu.Repositories
{
    public static class SponzoringRepo
    {
        private static int? _defaultLastSponzoringYear = null;
        
        private static readonly FusionCache _cache = new FusionCache(new FusionCacheOptions()
        {
            CacheName = "SponzoringCache",
        });
        
        public static string[] VelkeStrany = new string[]
        {
            "ANO", "ODS", "ČSSD", "Česká pirátská strana", "KSČM",
            "SPD", "STAN", "KDU-ČSL", "TOP 09", "Strana zelených"
        };

        public static string[] ParlamentníStrany = new string[]
        {
            "ANO", "ODS", "Česká pirátská strana",
            "SPD", "STAN", "KDU-ČSL", "TOP 09"
        };
        public static string[] TopStrany = VelkeStrany.ToArray();

        public static int DefaultLastSponzoringYear()
        {
            _defaultLastSponzoringYear ??= (HlidacStatu.Connectors.DirectDB.Instance
                                               .GetList<int?>("select max(DATEPART(yy, Sponzoring.DarovanoDne)) from sponzoring where sponzoring.darovanoDne < GetDate()")
                                               .FirstOrDefault()
                                           ?? (DateTime.Now.Year - 1));
            
            return _defaultLastSponzoringYear.Value;
        }



        private static DateTime minBigSponzoringDate = new DateTime(DateTime.Now.Year - 10, 1, 1);
        private static DateTime minSmallSponzoringDate = new DateTime(DateTime.Now.Year - 5, 1, 1);
        private static decimal smallSponzoringThreshold = 5000;

        public static Expression<Func<Sponzoring, bool>> SponzoringLimitsPredicate = s =>
            (s.Hodnota > smallSponzoringThreshold && s.DarovanoDne >= minBigSponzoringDate)
            || (s.Hodnota <= smallSponzoringThreshold && s.DarovanoDne >= minSmallSponzoringDate);

        public static Expression<Func<SponzoringSummed, bool>> SponzoringSummedLimitsPredicate = s =>
            (s.DarCelkem > smallSponzoringThreshold && s.Rok >= minBigSponzoringDate.Year)
            || (s.DarCelkem <= smallSponzoringThreshold && s.Rok >= minSmallSponzoringDate.Year);


        public static async Task<List<Sponzoring>> GetByDarceAsync(int osobaId, Expression<Func<Sponzoring, bool>> predicate, bool withCompany = true)
        {
            await using DbEntities db = new DbEntities();
            
            var osobySponzoring = await db.Sponzoring
                .AsNoTracking()
                .Where(s => s.OsobaIdDarce == osobaId)
                .Where(SponzoringLimitsPredicate)
                .Where(predicate)
                .ToListAsync();

            if (withCompany)
            {
                //sponzoring z navazanych firem kdyz byl statutar
                var firmySponzoring = Osoby.CachedFirmySponzoring.Get(osobaId)
                    .AsQueryable()
                    .Where(SponzoringLimitsPredicate)
                    .Where(predicate)
                    .ToList();

                osobySponzoring.AddRange(firmySponzoring);
            }

            return osobySponzoring;
        }

        public static async Task<List<Sponzoring>> GetByDarceAsync(string icoDarce, Expression<Func<Sponzoring, bool>> predicate)
        {
            await using DbEntities db = new DbEntities();
            return await db.Sponzoring
                .AsNoTracking()
                .Where(predicate)
                .Where(s => s.IcoDarce == icoDarce)
                .ToListAsync();
        }

        public static async Task<List<Sponzoring>> GetByPrijemceAsync(int osobaId)
        {
            await using DbEntities db = new DbEntities();
            return await db.Sponzoring.AsNoTracking()
                .Where(s => s.OsobaIdPrijemce == osobaId)
                .Where(SponzoringLimitsPredicate)
                .ToListAsync();
        }

        public static async Task<List<Sponzoring>> GetByPrijemceAsync(string icoPrijemce)
        {
            await using DbEntities db = new DbEntities();
            return await db.Sponzoring.AsNoTracking()
                .Where(s => s.IcoPrijemce == icoPrijemce)
                .Where(SponzoringLimitsPredicate)
                .ToListAsync();
        }
        
        public static async Task<List<SponzoringDetail>> GetByPrijemceWithPersonDetailsAsync(string icoPrijemce, 
            CancellationToken cancellationToken = default)
        {
            await using DbEntities db = new DbEntities();
            return await db.SponzoringDetails.FromSqlInterpolated(
                    $@"SELECT os.NameId NameIdDarce, os.Jmeno JmenoDarce, os.Prijmeni PrijmeniDarce, os.Narozeni DaumNarozeniDarce,
                              sp.IcoDarce, sp.IcoPrijemce, sp.Typ TypDaru, sp.Hodnota HodnotaDaru, sp.Popis, sp.DarovanoDne
                        FROM dbo.Sponzoring sp
                        Left Join dbo.Osoba os on sp.OsobaIdDarce = os.InternalId
                        WHERE sp.IcoPrijemce = {icoPrijemce}")
                .ToListAsync(cancellationToken);
        }

        public static async Task<Sponzoring> CreateAsync(Sponzoring sponzoring, string user)
        {
            await using DbEntities db = new DbEntities();
            
            if (sponzoring.OsobaIdDarce == 0
                && string.IsNullOrWhiteSpace(sponzoring.IcoDarce))
                throw new Exception(
                    "Cant attach sponzoring to a person or to a company since their reference is empty");

            sponzoring.Created = DateTime.Now;
            sponzoring.Edited = DateTime.Now;
            sponzoring.UpdatedBy = user;

            db.Sponzoring.Add(sponzoring);
            await db.SaveChangesAsync();

            AuditRepo.Add(Audit.Operations.Create, user, sponzoring, null);
            return sponzoring;
        }

        public static async Task MergeDonatingOsobaAsync(int originalOsobaId, int duplicateOsobaId, string user)
        {
            if (originalOsobaId == 0 && duplicateOsobaId == 0)
                throw new ArgumentException("Id osoby nesmí být 0");

            await using DbEntities db = new DbEntities();
            var sponzoring = await db.Sponzoring.Where(s => s.OsobaIdDarce == duplicateOsobaId)
                .ToListAsync();

            foreach (var donation in sponzoring)
            {
                var donationBackup = donation.ShallowCopy();

                donation.OsobaIdDarce = originalOsobaId;
                donation.Edited = DateTime.Now;
                AuditRepo.Add(Audit.Operations.Update, user, donation, donationBackup);
            }

            await db.SaveChangesAsync();
        }

        public static async Task DeleteAsync(Sponzoring sponzoring, string user)
        {
            if (sponzoring.Id > 0)
            {
                await using DbEntities db = new DbEntities();
                db.Sponzoring.Attach(sponzoring);
                db.Entry(sponzoring).State = EntityState.Deleted;
                AuditRepo.Add(Audit.Operations.Delete, user, sponzoring, null);

                await db.SaveChangesAsync();
            }
        }


        //přidat cache
        public static async Task<List<SponzoringOverview>> PartiesPerYearsOverviewAsync(int? year, CancellationToken cancellationToken)
        {
            int rok = year ?? 0;
            int yearSwitch = year.HasValue ? 0 : 1;
            
            var partiesPerYear = await _cache.GetOrSetAsync<List<SponzoringOverview>>($"bookmark:{rok}_{yearSwitch}", async _=>
            {
                await using DbEntities db = new DbEntities();
                return await db.SponzoringOverviewView.FromSqlInterpolated(
                        $@"SELECT zs.KratkyNazev, IcoPrijemce as IcoStrany
                      ,Year(DarovanoDne) as Rok, SUM(Hodnota) as DaryCelkem
                      ,SUM(case when icodarce is null or Len(IcoDarce) < 3 then Hodnota end) as DaryOsob
                      ,SUM(case when icodarce is not null and Len(IcoDarce) >= 3 then Hodnota end) as DaryFirem
                      ,COUNT(distinct osobaiddarce) as PocetDarujicichOsob
                      ,COUNT(distinct icodarce) as PocetDarujicichFirem
                      FROM Sponzoring sp
                      Left Join ZkratkaStrany zs on sp.IcoPrijemce = zs.ICO
                      WHERE (year(sp.DarovanoDne) = {rok} or 1={yearSwitch})
                      group by zs.KratkyNazev, IcoPrijemce, Year(DarovanoDne)")
                    .ToListAsync(cancellationToken);
            }, token: cancellationToken, options: CachingOptions.Cache10m_failsave4h);

            return partiesPerYear;
        }

        static Devmasters.Cache.LocalMemory.Cache<Dictionary<string, string>> stranyIcoCache =
            new Devmasters.Cache.LocalMemory.Cache<Dictionary<string, string>>(
                TimeSpan.FromHours(1), "stranyIcoCache",
                (o) =>
                {
                    var res = DirectDB.Instance.GetList<string, string>(@"SELECT  IcoPrijemce as IcoStrany, zs.KratkyNazev
                      FROM Sponzoring sp
                      Left Join ZkratkaStrany zs on sp.IcoPrijemce = zs.ICO
                      group by zs.KratkyNazev, IcoPrijemce");
                    
                    return res.ToDictionary(k => k.Item1 , v => v.Item2 ?? Firmy.GetJmeno(v.Item1));
                });

        private static Dictionary<string, string> StranyIco()
        {
            return stranyIcoCache.Get();

        }

        public static string IcoToKratkyNazev(string stranaIco)
        {
            if (StranyIco().ContainsKey(stranaIco))
                return StranyIco().FirstOrDefault(m=>m.Key == stranaIco).Value;
            return null;
        }
        public static string KratkyNazevToIco(string stranaKratkyNazev)
        {
            if (StranyIco().ContainsValue(stranaKratkyNazev))
                return StranyIco().FirstOrDefault(m => m.Value == stranaKratkyNazev).Key;
            return null;
        }

        public static async Task<Dictionary<int, decimal>> SponzoringPerYearAsync(string party, int minYear, int maxYear, bool persons, bool companies)
        {
            string icoStrany = ZkratkaStranyRepo.IcoStrany(party);
            await using DbEntities db = new DbEntities();
            
            var dataPerY = await db.Sponzoring
                .Where(m => m.IcoPrijemce == icoStrany && m.DarovanoDne.Value.Year >= minYear && m.DarovanoDne.Value.Year <= maxYear)
                .Where(m => (persons && m.OsobaIdDarce != null) || (companies && m.IcoDarce != null))
                .GroupBy(k => k.DarovanoDne.Value.Year, m => m, (k, v) => new { rok = k, sum = v.Sum(m => m.Hodnota ?? 0) })
                .ToDictionaryAsync(k => k.rok, v => v.sum);
            
            //add missing years
            for (int year = minYear; year <= maxYear; year++)
            {
                dataPerY.TryAdd(year, 0);
            }

            return dataPerY
                .OrderBy(m => m.Key)
                .ToDictionary(k => k.Key, m => m.Value);
        }

        public static async Task<List<SponzoringSummed>> PeopleSponsorsAsync(string party, CancellationToken cancellationToken)
        {
            string icoStrany = ZkratkaStranyRepo.IcoStrany(party);
            int tenYearsBack = DateTime.Now.Year - 10;

            await using DbEntities db = new DbEntities();
            
            return await db.SponzoringSummedView.FromSqlInterpolated(
                    $@"SELECT zs.KratkyNazev as NazevStrany
                       ,sp.IcoPrijemce as IcoStrany
	                   ,SUM(Hodnota) as DarCelkem
	                   ,Year(DarovanoDne) as Rok
	                   ,os.NameId as Id
	                   ,sp.icoDarce as IcoDarce
	                   ,RTRIM(LTRIM(isnull(os.TitulPred,'') + ' ' + os.Jmeno + ' ' + os.Prijmeni + ' ' + isnull(os.TitulPo,''))) as Jmeno
                       ,1 as typ
                        ,0 as PolitickaStrana 
                    FROM Sponzoring sp
                    LEFT Join ZkratkaStrany zs on sp.IcoPrijemce = zs.ICO
                    join Osoba os on sp.OsobaIdDarce = os.InternalId
                    where OsobaIdDarce > 0
                      and sp.IcoPrijemce = {icoStrany}
                      and year(sp.DarovanoDne) >= {tenYearsBack}
                    group by zs.KratkyNazev, IcoPrijemce, Year(DarovanoDne)
                      , os.NameId, os.TitulPred, os.Jmeno, os.Prijmeni, os.TitulPo, sp.icoDarce")
                .Where(SponzoringSummedLimitsPredicate)
                .ToListAsync(cancellationToken);
        }

        public static async Task<List<SponzoringSummed>> CompanySponsorsAsync(string party, CancellationToken cancellationToken)
        {
            string icoStrany = ZkratkaStranyRepo.IcoStrany(party);
            int tenYearsBack = DateTime.Now.Year - 10;

            await using DbEntities db = new DbEntities();
            return await db.SponzoringSummedView.FromSqlInterpolated(
                    $@"SELECT zs.KratkyNazev as NazevStrany
                               ,sp.IcoPrijemce as IcoStrany
	                           ,SUM(Hodnota) as DarCelkem
	                           ,Year(DarovanoDne) as Rok
	                           ,fi.ICO as Id
	                           ,fi.Jmeno as Jmeno
                               ,2 as typ
                               ,iif(fi.Kod_PF = {FirmaExtension.PolitickaStrana_kodPF},1,0) as PolitickaStrana 
                            FROM Sponzoring sp
                            LEFT Join ZkratkaStrany zs on sp.IcoPrijemce = zs.ICO
                            join Firma fi on sp.IcoDarce = fi.ICO
                            where IcoDarce is not null and sp.IcoPrijemce = {icoStrany}
                              and year(sp.DarovanoDne) >= {tenYearsBack}
                            group by zs.KratkyNazev, IcoPrijemce, Year(DarovanoDne), fi.ICO, fi.Jmeno, fi.kod_pf")
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="year">If null it returns sum for all years</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<List<SponzoringSummed>> BiggestPeopleSponsorsAsync(int? year, CancellationToken cancellationToken, int? take = null)
        {
            int rok = year ?? 0;
            int yearSwitch = year.HasValue ? 0 : 1;
            int tenYearsBack = DateTime.Now.Year - 10;

            await using DbEntities db = new DbEntities();
            var request = db.SponzoringSummedView.FromSqlInterpolated(
                    $@"SELECT null as NazevStrany
                               ,null as IcoStrany
	                           ,SUM(Hodnota) as DarCelkem
	                           ,{rok} as Rok
	                           ,os.NameId as Id
	                           ,RTRIM(LTRIM(isnull(os.TitulPred,'') + ' ' + os.Jmeno + ' ' + os.Prijmeni + ' ' + isnull(os.TitulPo,''))) as Jmeno
                                ,1 as typ
                                ,0 as politickaStrana
                            FROM Sponzoring sp
                            join Osoba os on sp.OsobaIdDarce = os.InternalId
                            where (year(sp.DarovanoDne) = {rok} or 1={yearSwitch}) and OsobaIdDarce > 0
                              and year(sp.DarovanoDne) >= {tenYearsBack}
                            group by os.NameId, os.TitulPred, os.Jmeno, os.Prijmeni, os.TitulPo")
                .OrderByDescending(x => x.DarCelkem);

            if (take != null)
                return await request.Take(take.Value).ToListAsync(cancellationToken);

            return await request.ToListAsync(cancellationToken);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="year">If null it returns sum for all years</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<List<SponzoringSummed>> BiggestCompanySponsorsAsync(int? year, CancellationToken cancellationToken, int? take = null)
        {
            int rok = year ?? 0;
            int yearSwitch = year.HasValue ? 0 : 1;
            int tenYearsBack = DateTime.Now.Year - 10;

            await using DbEntities db = new DbEntities();
            var request = db.SponzoringSummedView.FromSqlInterpolated(
                    $@"SELECT null as NazevStrany
                               ,null as IcoStrany
	                           ,SUM(Hodnota) as DarCelkem
	                           ,{rok} as Rok
	                           ,fi.ICO as Id
	                           ,fi.Jmeno as Jmeno
                               ,2 as typ
                               ,iif(fi.Kod_PF = {FirmaExtension.PolitickaStrana_kodPF},1,0) as PolitickaStrana 
                            FROM Sponzoring sp
                            join Firma fi on sp.IcoDarce = fi.ICO
                            where (year(sp.DarovanoDne) = {rok} or 1={yearSwitch}) and IcoDarce is not null
                              and year(sp.DarovanoDne) >= {tenYearsBack}
                            group by fi.ICO, fi.Jmeno, fi.kod_pf")
                .OrderByDescending(x => x.DarCelkem); ;

            if (take != null)
                return await request.Take(take.Value).ToListAsync(cancellationToken);

            return await request.ToListAsync(cancellationToken);
        }
    }
}