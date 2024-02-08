using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HlidacStatu.Entities;
using HlidacStatu.Entities.Entities;
using HlidacStatu.Util;
using Microsoft.EntityFrameworkCore;

namespace HlidacStatu.Repositories;

public class PuRepo
{
    public static async Task<PuOrganizace> GetDetailEagerAsync(int id)
    {
        await using var db = new DbEntities();

        return await db.PuOrganizace
            .AsNoTracking()
            .Where(pu => pu.Id == id)
            .Include(o => o.Tags) // Include PuOrganizaceTags
            .Include(o => o.Platy) // Include PuPlat
            .Include(o => o.Metadata) // Include PuOranizaceMetadata
            .FirstOrDefaultAsync();
    }
    
    public static async Task<List<PuOrganizace>> GetOrganizaceForOblastiAsync(string oblast)
    {
        await using var db = new DbEntities();

        return await db.PuOrganizace
            .AsNoTracking()
            .Where(o => o.Oblast.StartsWith(oblast))
            .Include(o => o.Platy) // Include PuPlat
            .ToListAsync();
    }
    
    //todo: test it
    public static async Task<List<PuOrganizace>> GetOrganizacForTagAsync(string tag)
    {
        await using var db = new DbEntities();

        return await db.PuOrganizaceTags
            .AsNoTracking()
            .Where(t => t.Tag == tag)
            .Include(t => t.Organizace)
            .ThenInclude(t => t.Platy)
            .Select(t => t.Organizace)
            .ToListAsync();
    }
    
    public static async Task<List<KeyValuePair<string, string>>> GetFollowingOblastiAsync(string oblast)
    {
        await using var db = new DbEntities();

        var oblasti = await db.PuOrganizace
            .AsNoTracking()
            .Where(o => o.Oblast.StartsWith(oblast))
            .Where(o => o.Oblast.Length >= oblast.Length + 1) // vybrat pokračující oblasti, odstranit končící
            .Select(o => o.Oblast.Substring(oblast.Length + 1)) // vzít jen následující element
            .Distinct()
            .ToListAsync();

        var oblastiCleaned = oblasti.Select(o => o.Split(PuOrganizace.PathSplittingChar,
                    StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault())
            .Distinct(); // split nefunguje v selectu správně, proto očišťujeme až zde
        
        List<KeyValuePair<string, string>> results = new();
        
        foreach (var nextOblast in oblastiCleaned) //musíme distinct tady, protože se split provádí až po 
        {
            if(string.IsNullOrWhiteSpace(nextOblast))
                continue;
            
            results.Add(new KeyValuePair<string, string>(nextOblast,$"{oblast}{PuOrganizace.PathSplittingChar}{nextOblast}"));

        }

        return results;
    }


    public static async Task<PuPlat> GetPlatAsync(int id)
    {
        await using var db = new DbEntities();

        return await db.PuPlaty
            .AsNoTracking()
            .Include(p => p.Organizace)
            .Where(p => p.Id == id)
            .FirstOrDefaultAsync();
    }
    
    public static async Task<List<PuPlat>> GetPlatyAsync(int rok)
    {
        await using var db = new DbEntities();

        return await db.PuPlaty
            .AsNoTracking()
            .Where(p => p.Rok == rok)
            .ToListAsync();
    }
    
    public static async Task<List<string>> GetPrimalOblastiAsync()
    {
        await using var db = new DbEntities();
        
        var oblasti = await db.Database.SqlQuery<string>(@$"SELECT DISTINCT
            LEFT(Oblast, CHARINDEX({PuOrganizace.PathSplittingChar}, Oblast + {PuOrganizace.PathSplittingChar}) - 1) AS FirstPart
            FROM Pu_Organizace
            WHERE Oblast IS NOT NULL")
        .ToListAsync();
        
        return oblasti;
    }

    public static async Task<List<PuPlat>> GetPoziceDlePlatuAsync(int rangeMin, int rangeMax, int year)
    {
        await using var db = new DbEntities();

        return await db.PuPlaty
            .AsNoTracking()
            .Where(p => p.Rok == year)
            .Where(p => (((p.Plat ?? 0) + (p.Odmeny ?? 0)) * (1 / p.Uvazek ?? 1) / (p.PocetMesicu ?? 12)) >= rangeMin)
            .Where(p => (((p.Plat ?? 0) + (p.Odmeny ?? 0)) * (1 / p.Uvazek ?? 1) / (p.PocetMesicu ?? 12)) <= rangeMax)
            .Include(p => p.Organizace)
            .ToListAsync();
    }

    
}