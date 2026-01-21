using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HlidacStatu.Caching;
using HlidacStatu.Entities;
using HlidacStatu.Entities.Facts;
using HlidacStatu.Entities.Views;
using HlidacStatu.Extensions;
using HlidacStatu.Util;
using Microsoft.EntityFrameworkCore;
using ZiggyCreatures.Caching.Fusion;

namespace HlidacStatu.Repositories.Cache;

public static class SponzoringCache
{

    private static IFusionCache MemoryCache =>
        HlidacStatu.Caching.CacheFactory.CreateNew(CacheFactory.CacheType.L1Default, nameof(SponzoringCache));
    
    
    public static ValueTask<Dictionary<string, string>> StranyIcoAsync() =>
        MemoryCache.GetOrSetAsync($"_stranyIcoCache_", async ct =>
            {
                await using DbEntities db = new DbEntities();
                var query = await db.StranaView.FromSql($@"
                    SELECT IcoPrijemce AS IcoStrany, zs.KratkyNazev
                    FROM Sponzoring sp
                    LEFT JOIN ZkratkaStrany zs ON sp.IcoPrijemce = zs.ICO
                    GROUP BY zs.KratkyNazev, IcoPrijemce
                ").AsNoTracking().ToListAsync(cancellationToken: ct);
                
                var result = new Dictionary<string, string>();
                foreach (var item in query)
                {
                    var jmeno = item.KratkyNazev ?? await Firmy.GetJmenoAsync(item.IcoStrany);
                    result.Add(item.IcoStrany, jmeno);
                }
                return result;
            },
            options => options.ModifyEntryOptionsDuration(TimeSpan.FromHours(6)));

    public static async Task<List<SponzoringOverview>> PartiesPerYearsOverviewAsync(int? year,
        CancellationToken cancellationToken)
    {
        int rok = year ?? 0;
        int yearSwitch = year.HasValue ? 0 : 1;

        var partiesPerYear = await MemoryCache.GetOrSetAsync<List<SponzoringOverview>>($"_partiesPerYearsOverview:{rok}_{yearSwitch}",
            async ct =>
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
                    .AsNoTracking()
                    .ToListAsync(ct);
            }, 
            options => options.ModifyEntryOptionsDuration(TimeSpan.FromHours(6)), token: cancellationToken);

        return partiesPerYear;
    }

}