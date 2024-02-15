using HlidacStatu.Entities;
using HlidacStatu.Entities.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HlidacStatu.Repositories;

public static class PuRepo
{
    public const int DefaultYear = 2022;

    public static readonly string[] MainTags =
    [
        "bezpečnost",
        "centrální a kontrolní instituce",
        "doprava",
        "finance",
        "justice",
        "kultura",
        "ministerstvo",
        "nemocnice",
        "práce",
        "průmysl a obchod",
        "služby",
        "společnost",
        "výzkum",
        "vzdělávání",
        "zdravotnictví",
        "zemědělství",
        "životní prostředí",
        "ostatní"
    ];

    public static async Task<PuRokOrganizaceStat> GetGlobalStatAsync(int rok = DefaultYear)
    {
        await using var db = new DbEntities();

        PuRokOrganizaceStat stat = new PuRokOrganizaceStat();
        stat.PocetOslovenych= await db.PuOrganizaceMetadata
            .CountAsync(m => m.DatumOdeslaniZadosti.HasValue && m.Rok == rok);

        stat.PocetCoPoslaliPlat = await db.PuPlaty
            .Where(m => m.Rok == rok)
            .Select(m=>m.IdOrganizace)
            .Distinct()
            .CountAsync();

        var platyRok = db.PuPlaty
            .AsNoTracking()
            .Where(m => m.Rok == rok)
            .Select(m => new { mplat = m.HrubyMesicniPlat, plat = m, org = m.Organizace })
            .ToArray()
            .OrderBy(o => o.mplat)
            .ToArray();

        var platyRokHlavouni = db.PuPlaty
            .AsNoTracking()
            .Where(m => m.Rok == rok && m.JeHlavoun == true)
            .Select(m => new { mplat = m.HrubyMesicniPlat, plat = m, org = m.Organizace })
            .ToArray()
            .OrderBy(o=>o.mplat)
            .ToArray();


        stat.PercentilyPlatu = new Dictionary<int, decimal>() {
            {1, HlidacStatu.Util.MathTools.PercentileCont(0.01m,platyRok.Select(m=>m.mplat))},
            {5, HlidacStatu.Util.MathTools.PercentileCont(0.05m,platyRok.Select(m=>m.mplat))},
            {10, HlidacStatu.Util.MathTools.PercentileCont(0.10m,platyRok.Select(m=>m.mplat))},
            {25, HlidacStatu.Util.MathTools.PercentileCont(0.25m,platyRok.Select(m=>m.mplat))},
            {50, HlidacStatu.Util.MathTools.PercentileCont(0.50m,platyRok.Select(m=>m.mplat))},
            {75, HlidacStatu.Util.MathTools.PercentileCont(0.75m,platyRok.Select(m=>m.mplat))},
            {90, HlidacStatu.Util.MathTools.PercentileCont(0.90m,platyRok.Select(m=>m.mplat))},
            {95, HlidacStatu.Util.MathTools.PercentileCont(0.95m,platyRok.Select(m=>m.mplat))},
            {99, HlidacStatu.Util.MathTools.PercentileCont(0.99m,platyRok.Select(m=>m.mplat))},
        };
        stat.PercentilyPlatuHlavounu = new Dictionary<int, decimal>() {
            {1, HlidacStatu.Util.MathTools.PercentileCont(0.01m,platyRokHlavouni.Select(m=>m.mplat))},
            {5, HlidacStatu.Util.MathTools.PercentileCont(0.05m,platyRokHlavouni.Select(m=>m.mplat))},
            {10, HlidacStatu.Util.MathTools.PercentileCont(0.10m,platyRokHlavouni.Select(m=>m.mplat))},
            {25, HlidacStatu.Util.MathTools.PercentileCont(0.25m,platyRokHlavouni.Select(m=>m.mplat))},
            {50, HlidacStatu.Util.MathTools.PercentileCont(0.50m,platyRokHlavouni.Select(m=>m.mplat))},
            {75, HlidacStatu.Util.MathTools.PercentileCont(0.75m,platyRokHlavouni.Select(m=>m.mplat))},
            {90, HlidacStatu.Util.MathTools.PercentileCont(0.90m,platyRokHlavouni.Select(m=>m.mplat))},
            {95, HlidacStatu.Util.MathTools.PercentileCont(0.95m,platyRokHlavouni.Select(m=>m.mplat))},
            {99, HlidacStatu.Util.MathTools.PercentileCont(0.99m,platyRokHlavouni.Select(m=>m.mplat))},
        };
        return stat;
    }

    public static async Task<PuOrganizace> GetFullDetailAsync(string datovaSchranka)
    {
        await using var db = new DbEntities();

        return await db.PuOrganizace
            .AsNoTracking()
            .Where(pu => pu.DS == datovaSchranka)
            .Include(o => o.Tags) // Include PuOrganizaceTags
            .Include(o => o.Platy) // Include PuPlat
            .Include(o => o.Metadata) // Include PuOranizaceMetadata
            .FirstOrDefaultAsync();
    }

    public static List<PuPlat> Rok(this ICollection<PuPlat> platy, int rok = DefaultYear)
    {
        return platy?.Where(m => m.Rok == rok).ToList();
    }

    public static async Task<List<PuOrganizace>> GetOrganizaceForTagAsync(string tag)
    {
        await using var db = new DbEntities();

        return await db.PuOrganizaceTags
            .AsNoTracking()
            .Where(t => t.Tag.Equals(tag))
            .Include(t => t.Organizace)
            .ThenInclude(o => o.Platy)
            .Select(t => t.Organizace)
            .ToListAsync();
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