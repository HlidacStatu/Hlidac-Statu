using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HlidacStatu.Entities;
using HlidacStatu.Entities.Entities;
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
    
    public static async Task<List<PuOrganizace>> GetPlatyAsync(string oblast)
    {
        await using var db = new DbEntities();

        return await db.PuOrganizace
            .AsNoTracking()
            .Where(o => o.Oblast.StartsWith(oblast))
            .Include(o => o.Platy) // Include PuPlat
            .ToListAsync();
    }
    
    public static async Task<List<KeyValuePair<string, string>>> GetFollowingOblastiAsync(string oblast)
    {
        await using var db = new DbEntities();

        var oblasti = await db.PuOrganizace
            .AsNoTracking()
            .Where(o => o.Oblast.StartsWith(oblast))
            .Where(o => o.Oblast.Length >= oblast.Length + 1) // vybrat pokračující oblasti, odstranit končící
            .Select(o => o.Oblast.Substring(oblast.Length + 1) // odstranit základ a odstranit oddělovač '>'
                .Split(PuOrganizace.PathSplittingChar,
                    StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault()) // vzít jen následující element
            .Distinct()
            .ToListAsync();

        List<KeyValuePair<string, string>> results = new();
        
        foreach (var nextOblast in oblasti)
        {
            if(string.IsNullOrWhiteSpace(nextOblast))
                continue;
            
            results.Add(new KeyValuePair<string, string>(nextOblast,$"{oblast}{PuOrganizace.PathSplittingChar}{nextOblast}"));

        }

        return results;
    }
    
    
    
   
}