using System;
using HlidacStatu.Entities;
using HlidacStatu.Entities.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Devmasters.Collections;

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
        stat.PocetOslovenych = await db.PuOrganizaceMetadata
            .CountAsync(m => m.DatumOdeslaniZadosti.HasValue && m.Rok == rok);

        stat.PocetCoPoslaliPlat = await db.PuPlaty
            .Where(m => m.Rok == rok)
            .Select(m => m.IdOrganizace)
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
            .OrderBy(o => o.mplat)
            .ToArray();


        stat.PercentilyPlatu = new Dictionary<int, decimal>()
        {
            { 1, HlidacStatu.Util.MathTools.PercentileCont(0.01m, platyRok.Select(m => m.mplat)) },
            { 5, HlidacStatu.Util.MathTools.PercentileCont(0.05m, platyRok.Select(m => m.mplat)) },
            { 10, HlidacStatu.Util.MathTools.PercentileCont(0.10m, platyRok.Select(m => m.mplat)) },
            { 25, HlidacStatu.Util.MathTools.PercentileCont(0.25m, platyRok.Select(m => m.mplat)) },
            { 50, HlidacStatu.Util.MathTools.PercentileCont(0.50m, platyRok.Select(m => m.mplat)) },
            { 75, HlidacStatu.Util.MathTools.PercentileCont(0.75m, platyRok.Select(m => m.mplat)) },
            { 90, HlidacStatu.Util.MathTools.PercentileCont(0.90m, platyRok.Select(m => m.mplat)) },
            { 95, HlidacStatu.Util.MathTools.PercentileCont(0.95m, platyRok.Select(m => m.mplat)) },
            { 99, HlidacStatu.Util.MathTools.PercentileCont(0.99m, platyRok.Select(m => m.mplat)) },
        };
        stat.PercentilyPlatuHlavounu = new Dictionary<int, decimal>()
        {
            { 1, HlidacStatu.Util.MathTools.PercentileCont(0.01m, platyRokHlavouni.Select(m => m.mplat)) },
            { 5, HlidacStatu.Util.MathTools.PercentileCont(0.05m, platyRokHlavouni.Select(m => m.mplat)) },
            { 10, HlidacStatu.Util.MathTools.PercentileCont(0.10m, platyRokHlavouni.Select(m => m.mplat)) },
            { 25, HlidacStatu.Util.MathTools.PercentileCont(0.25m, platyRokHlavouni.Select(m => m.mplat)) },
            { 50, HlidacStatu.Util.MathTools.PercentileCont(0.50m, platyRokHlavouni.Select(m => m.mplat)) },
            { 75, HlidacStatu.Util.MathTools.PercentileCont(0.75m, platyRokHlavouni.Select(m => m.mplat)) },
            { 90, HlidacStatu.Util.MathTools.PercentileCont(0.90m, platyRokHlavouni.Select(m => m.mplat)) },
            { 95, HlidacStatu.Util.MathTools.PercentileCont(0.95m, platyRokHlavouni.Select(m => m.mplat)) },
            { 99, HlidacStatu.Util.MathTools.PercentileCont(0.99m, platyRokHlavouni.Select(m => m.mplat)) },
        };
        return stat;
    }

    public static async Task<PuOrganizace> GetFullDetailAsync(string datovaSchranka)
    {
        await using var db = new DbEntities();

        return await db.PuOrganizace
            .AsNoTracking()
            .Where(pu => pu.DS == datovaSchranka)
            .Include(o => o.Metadata)
            .Include(o => o.Tags)
            .Include(o => o.FirmaDs)
            .Include(o => o.Platy) // Include PuPlat
            .FirstOrDefaultAsync();
    }
    
    public static async Task<PuOrganizace> GetOrganizationOfTheDayAsync()
    {
        await using var db = new DbEntities();

        var orgs = await db.PuOrganizace.ToListAsync();
        var tip = orgs.TipOfTheDay();

        return await db.PuOrganizace
            .AsNoTracking()
            .Where(pu => pu.DS == tip.DS)
            .Include(o => o.Metadata)
            .Include(o => o.Tags)
            .Include(o => o.FirmaDs)
            .Include(o => o.Platy) // Include PuPlat
            .FirstOrDefaultAsync();
    }

    public static List<PuPlat> Rok(this ICollection<PuPlat> platy, int rok = DefaultYear)
    {
        return platy?.Where(m => m.Rok == rok).ToList();
    }

    public static async Task<List<PuOrganizace>> GetOrganizaceForTagAsync(string tag, int limit = 0)
    {
        await using var db = new DbEntities();

        var query = db.PuOrganizaceTags
            .AsNoTracking()
            .Where(t => t.Tag.Equals(tag))
            .Include(t => t.Organizace).ThenInclude(o => o.FirmaDs)
            .Include(t => t.Organizace).ThenInclude(o => o.Metadata)
            .Include(t => t.Organizace).ThenInclude(o => o.Platy)
            .Select(t => t.Organizace);
        
        if(limit > 0)
            query = query.Take(limit);
        
        return await query.ToListAsync();
    }

    public static async Task<PuPlat> GetPlatAsync(int id)
    {
        await using var db = new DbEntities();

        return await db.PuPlaty
            .AsNoTracking()
            .Include(p => p.Organizace)
            .ThenInclude(o => o.FirmaDs)
            .Where(p => p.Id == id)
            .FirstOrDefaultAsync();
    }
    
    public static async Task<PuPlat> GetPlatAsync(int idOrganizace, int rok, string nazevPozice)
    {
        await using var db = new DbEntities();

        return await db.PuPlaty
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.IdOrganizace == idOrganizace
                                      && p.Rok == rok
                                      && p.NazevPozice == nazevPozice);
    }
    

    public static async Task<List<PuPlat>> GetPlatyAsync(int rok)
    {
        await using var db = new DbEntities();

        return await db.PuPlaty
            .AsNoTracking()
            .Where(p => p.Rok == rok)
            .ToListAsync();
    }
    
    public static async Task<List<PuPlat>> GetPlatyWithOrganizaceForYearAsync(int rok)
    {
        await using var db = new DbEntities();

        return await db.PuPlaty
            .AsNoTracking()
            .Include(p => p.Organizace)
            .ThenInclude(o => o.FirmaDs)
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

    public static async Task UpsertOrganizaceAsync(PuOrganizace organizace)
    {
        await using var dbContext = new DbEntities();

        if (string.IsNullOrWhiteSpace(organizace.DS))
        {
            throw new Exception("Chybí vyplněná datová schránka");
        }

        organizace.DS = organizace.DS.Trim();
        
        // null navigation properties in organizace, because we will add them manually
        var metadata = organizace.Metadata;
        organizace.Metadata = null;
        var tagy = organizace.Tags;
        organizace.Tags = null;
        var platy = organizace.Platy;
        organizace.Platy = null;
        organizace.FirmaDs = null;

        var original = await dbContext.PuOrganizace.FirstOrDefaultAsync(o => o.DS == organizace.DS);
        if (original is null || original.Id == 0)
        {
            dbContext.PuOrganizace.Add(organizace);
        }
        else
        {
            original.Info = organizace.Info;
            original.HiddenNote = organizace.HiddenNote;
        }

        await dbContext.SaveChangesAsync();


        if (metadata is not null)
        {
            foreach (var metadatum in metadata)
            {
                await UpsertMetadataAsync(metadatum);
            }
        }

        if (tagy is not null)
        {
            foreach (var tag in tagy)
            {
                await UpsertTagAsync(tag);
            }
        }

        if (platy is not null)
        {
            foreach (var plat in platy)
            {
                await UpsertPlatAsync(plat);
            }
        }
    }

    public static async Task UpsertMetadataAsync(PuOrganizaceMetadata metadatum)
    {
        if (metadatum.Rok == 0)
        {
            throw new Exception("Chybí vyplněný rok");
        }

        if (metadatum.IdOrganizace == 0)
        {
            throw new Exception("Chybí vyplněné id organizace");
        }

        PuOrganizaceMetadata origMetadata;

        await using var dbContext = new DbEntities();
        if (metadatum.Id == 0)
        {
            origMetadata = await dbContext.PuOrganizaceMetadata
                .FirstOrDefaultAsync(p => p.IdOrganizace == metadatum.IdOrganizace
                                          && p.Rok == metadatum.Rok);
        }
        else
        {
            origMetadata = await dbContext.PuOrganizaceMetadata
                .FirstOrDefaultAsync(p => p.Id == metadatum.Id);
        }

        if (origMetadata is null)
        {
            dbContext.PuOrganizaceMetadata.Add(metadatum);
        }
        else
        {
            origMetadata.ZpusobKomunikace = metadatum.ZpusobKomunikace;
            origMetadata.DatumOdeslaniZadosti = metadatum.DatumOdeslaniZadosti;
            origMetadata.DatumPrijetiOdpovedi = metadatum.DatumPrijetiOdpovedi;
            origMetadata.ZduvodneniMimoradnychOdmen = metadatum.ZduvodneniMimoradnychOdmen;
            origMetadata.SkrytaPoznamka = metadatum.SkrytaPoznamka;
        }

        await dbContext.SaveChangesAsync();
    }

    public static async Task UpsertTagAsync(PuOrganizaceTag tag)
    {
        if (string.IsNullOrWhiteSpace(tag.Tag))
        {
            throw new Exception("Chybí název tagu");
        }

        if (tag.IdOrganizace == 0)
        {
            throw new Exception("Chybí id organizace");
        }

        PuOrganizaceTag origTag;
        tag.Tag = tag.Tag.Trim();
        await using var dbContext = new DbEntities();
        if (tag.Id == 0)
        {
            origTag = await dbContext.PuOrganizaceTags
                .FirstOrDefaultAsync(t => t.IdOrganizace == tag.IdOrganizace && t.Tag == tag.Tag);
        }
        else
        {
            origTag = await dbContext.PuOrganizaceTags
                .FirstOrDefaultAsync(t => t.Id == tag.Id);
        }

        if (origTag is null || origTag.Id == 0)
        {
            dbContext.PuOrganizaceTags.Add(tag);
        }
        else
        {
            origTag.Tag = tag.Tag;
        }

        await dbContext.SaveChangesAsync();
    }

    public static async Task UpsertPlatAsync(PuPlat plat)
    {
        if (string.IsNullOrWhiteSpace(plat.NazevPozice))
        {
            throw new Exception("Chybí vyplněný název pozice");
        }

        if (plat.Rok == 0)
        {
            throw new Exception("Chybí vyplněný rok pozice");
        }

        if (plat.IdOrganizace == 0)
        {
            throw new Exception("Chybí vyplněné id organizace");
        }

        plat.NazevPozice = plat.NazevPozice.Trim();

        PuPlat origPlat;

        await using var dbContext = new DbEntities();
        if (plat.Id == 0)
        {
            origPlat = await dbContext.PuPlaty
                .FirstOrDefaultAsync(p => p.IdOrganizace == plat.IdOrganizace
                                          && p.Rok == plat.Rok
                                          && p.NazevPozice == plat.NazevPozice);
        }
        else
        {
            origPlat = await dbContext.PuPlaty
                .FirstOrDefaultAsync(p => p.Id == plat.Id);
        }

        if (origPlat is null)
        {
            dbContext.PuPlaty.Add(plat);
        }
        else
        {
            origPlat.NazevPozice = plat.NazevPozice;
            origPlat.Plat = plat.Plat;
            origPlat.Odmeny = plat.Odmeny;
            origPlat.Uvazek = plat.Uvazek;
            origPlat.DisplayOrder = plat.DisplayOrder;
            origPlat.JeHlavoun = plat.JeHlavoun;
            origPlat.NefinancniBonus = plat.NefinancniBonus;
            origPlat.PocetMesicu = plat.PocetMesicu;
            origPlat.PoznamkaPlat = plat.PoznamkaPlat;
            origPlat.PoznamkaPozice = plat.PoznamkaPozice;
            origPlat.SkrytaPoznamka = plat.SkrytaPoznamka;
        }

        await dbContext.SaveChangesAsync();
    }

}