using HlidacStatu.Entities;
using HlidacStatu.Entities.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HlidacStatu.Repositories;

public static class PuRepo
{
    public const int DefaultYear = 2022;

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

    public static List<PuPlat> Rok(this ICollection<PuPlat> platy, int rok = DefaultYear)
    {
        return platy?.Where(m => m.Rok == rok).ToList();
    }

    public static async Task<List<PuOrganizace>> GetOrganizaceForOblastiAsync(string oblast, string podoblast)
    {
        await using var db = new DbEntities();

        var query = db.PuOrganizace
            .AsNoTracking()
            .Where(o => o.Oblast.Equals(oblast));
        
        if(!string.IsNullOrWhiteSpace(podoblast))
            query = query.Where(o => o.PodOblast.Equals(podoblast));
        
        return await query 
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

    public static async Task<List<string>> GetPodoblastiAsync(string oblast)
    {
        await using var db = new DbEntities();

        var podoblasti = await db.PuOrganizace
            .AsNoTracking()
            .Where(o => o.Oblast.Equals(oblast))
            .Select(o => o.PodOblast)
            .Where(s=>!string.IsNullOrWhiteSpace(s))
            .Distinct()
            .ToListAsync();

        return podoblasti;
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

    public static async Task<List<string>> GetPrimalOblasti()
    {
        await using var db = new DbEntities();

        var oblasti = await db.PuOrganizace
            .AsNoTracking()
            .Select(o => o.Oblast)
            .Distinct()
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