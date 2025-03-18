using HlidacStatu.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HlidacStatu.Repositories;

public static class PpRepo
{
    public const int DefaultYear = 2024;
    public const int MinYear = 2024;
    
    public static async Task<PuOrganizace> GetFullDetailAsync(string datovaSchranka)
    {
        return await GetFullDetailAsync(new string[] { datovaSchranka });
    }
    
    public static async Task<PuOrganizace> GetFullDetailAsync(string[] datoveSchranky)
    {
        await using var db = new DbEntities();

        return await db.PuOrganizace
            .AsNoTracking()
            .Where(pu => datoveSchranky.Contains(pu.DS) )
            .Include(o => o.Metadata)
            .Include(o => o.Tags)
            .Include(o => o.FirmaDs)
            .Include(o => o.PrijmyPolitiku) // Include PuPrijmyPolitiku
            .FirstOrDefaultAsync();
    }

    

    public static List<PuPolitikPrijem> AktualniRok(this ICollection<PuPolitikPrijem> prijmy, int rok = DefaultYear)
    {
        return prijmy?.Where(m => m.Rok == rok).ToList();
    }

    public static async Task<PuPolitikPrijem> GetPrijemAsync(int id)
    {
        await using var db = new DbEntities();

        return await db.PuPoliticiPrijmy
            .AsNoTracking()
            .Include(p => p.Organizace).ThenInclude(o => o.FirmaDs)
            .Include(p => p.Organizace).ThenInclude(o => o.Tags)
            .Where(p => p.Id == id)
            .FirstOrDefaultAsync();
    }

    public static async Task<PuPolitikPrijem> GetPlatAsync(int idOrganizace, int rok, string nameid)
    {
        await using var db = new DbEntities();

        return await db.PuPoliticiPrijmy
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.IdOrganizace == idOrganizace
                                      && p.Rok == rok
                                      && p.Nameid == nameid);
    }
    
    public static async Task<PuRokPoliticiStat> GetGlobalStatAsync(int rok = DefaultYear)
    {
        await using var db = new DbEntities();

        var stat = new PuRokPoliticiStat();
        stat.PocetOslovenych = await db.PuOrganizaceMetadata
            .Where(m => m.Typ == PuOrganizaceMetadata.TypMetadat.PlatyPolitiku)
            .CountAsync(m => m.DatumOdeslaniZadosti.HasValue && m.Rok == rok);

        stat.PocetCoPoslaliPlat = await db.PuPoliticiPrijmy
            .Where(m => m.Rok == rok)
            .Select(m => m.IdOrganizace)
            .Distinct()
            .CountAsync();

        var platyRok = db.PuPoliticiPrijmy
            .AsNoTracking()
            .Where(m => m.Rok == rok)
            .Select(m => new { mplat = m.HrubyMesicniPlatVcetneOdmen, plat = m, org = m.Organizace })
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
        return stat;
    }
    
    public static async Task<List<PuPolitikPrijem>> GetPrijmyPolitika(string nameid)
    {
        await using var db = new DbEntities();

        return await db.PuPoliticiPrijmy
            .AsNoTracking()
            .Include(p => p.Organizace).ThenInclude(o => o.FirmaDs)
            .Include(p => p.Organizace).ThenInclude(o => o.Tags)
            .Where(p => p.Nameid == nameid)
            .ToListAsync();
    }


    public static async Task<int> GetPlatyCountAsync(int rok)
    {
        await using var db = new DbEntities();

        return await db.PuPoliticiPrijmy
            .AsNoTracking()
            .Where(p => p.Rok == rok)
            .CountAsync();
    }
    
    public static async Task<List<PuOrganizace>> GetActiveOrganizaceForTagAsync(string tag, int limit = 0)
    {
        await using var db = new DbEntities();

        var query = db.PuOrganizaceTags
            .AsNoTracking()
            .Where(t => tag == null || t.Tag.Equals(tag))
            .Include(t => t.Organizace).ThenInclude(o => o.FirmaDs)
            .Include(t => t.Organizace).ThenInclude(o => o.Metadata.Where(m => m.Typ == PuOrganizaceMetadata.TypMetadat.PlatyPolitiku))
            .Include(t => t.Organizace).ThenInclude(o => o.PrijmyPolitiku)
            .Select(t => t.Organizace);

        if (limit > 0)
            query = query.Take(limit);

        return await query.ToListAsync();
    }

    public static readonly string[] MainTags =
    [
        "politici",
    ];
    
    public static async Task<List<PuPolitikPrijem>> GetPlatyAsync(int rok)
    {
        await using var db = new DbEntities();

        return await db.PuPoliticiPrijmy
            .AsNoTracking()
            .Where(p => p.Rok == rok)
            .ToListAsync();
    }
    
    public static string PlatyForYearPoliticiDescriptionHtml(this PuOrganizace org, int rok = DefaultYear, bool withDetail = false)
    {
        var desc = org.GetMetadataDescriptionPolitici(rok);

        return $"<span class='text-{desc.BootstrapStatus}'><i class='{desc.Icon}'></i> {desc.TextStatus}{(withDetail ? $". {desc.Detail}" : "")}</span>";
    }
    
    public static PuOrganizaceMetadata.Description GetMetadataDescriptionPolitici(this PuOrganizace org, int rok = DefaultYear )
    {
        var res = new PuOrganizaceMetadata.Description();
        
        var metadataList = org.MetadataPlatyUredniku.Where(m => m.Rok == rok && m.Typ == PuOrganizaceMetadata.TypMetadat.PlatyPolitiku).ToList();
        //ted pracuju pouze s jednou
        var metadata = metadataList.FirstOrDefault();

        if (org.PrijmyPolitiku.AktualniRok().Count == 0 && PuRepo.GetNeaktivniOrganizace().ToArray().Any(m => m.Item1 == org.DS))
        {
            res.TextStatus = "Této organizace jsme se na platy neptali";
            res.Detail = "";
            res.BootstrapStatus = "primary";
            res.Icon = "fa-solid fa-question-circle";
        }
        else if (metadata == null)
        {
            res.TextStatus = "Této organizace jsme se na platy neptali";
            res.Detail = "";
            res.BootstrapStatus = "primary";
            res.Icon = "fa-solid fa-question-circle";

        }
        else if (metadata.DatumPrijetiOdpovedi == null)
        {
            res.TextStatus = $"{metadata.DatumOdeslaniZadosti:d. M. yyyy} Odeslána žádost o platy";
            res.Detail = "Data jsme zatím nedostali nebo nezpracovali.";
            res.BootstrapStatus = "primary";
            res.Icon = "fa-solid fa-question-circle";

        }
        else if (org.PrijmyPolitiku.AktualniRok(rok).Count == 0)
        {
            res.TextStatus = "Odmítli poskytnout platy";
            res.Detail = metadata.PoznamkaHlidace;
            res.BootstrapStatus = "danger";
            res.Icon = "fa-solid fa-circle-xmark";
        }
        else if (org.PrijmyPolitiku.AktualniRok(rok).Count == 1)
        {
            res.TextStatus = "Evidujeme jeden plat jedné pozice";
            res.Detail = metadata.PoznamkaHlidace;
            res.BootstrapStatus = "warning";
            res.Icon = "fa-solid fa-circle-exclamation";
        }
        else if (org.PrijmyPolitiku.AktualniRok(rok).Count < 5)
        {
            res.TextStatus = $"{org.PrijmyPolitiku.AktualniRok(rok).Count} platy";
            res.Detail = metadata.PoznamkaHlidace;
            res.BootstrapStatus = "success";
            res.Icon = "fa-solid fa-badge-check";
        }
        else
        {
            res.TextStatus = $"{org.PrijmyPolitiku.AktualniRok(rok).Count} platů";
            res.Detail = metadata.PoznamkaHlidace;
            res.BootstrapStatus = "success";
            res.Icon = "fa-solid fa-badge-check";
        }
        return res;
    }

    public static async Task<List<PuPolitikPrijem>> GetPlatyWithOrganizaceForYearAsync(int rok)
    {
        await using var db = new DbEntities();

        return await db.PuPoliticiPrijmy
            .AsNoTracking()
            .Include(p => p.Organizace)
            .ThenInclude(o => o.FirmaDs)
            .Where(p => p.Rok == rok)
            .ToListAsync();
    }

    public static async Task<List<PuPolitikPrijem>> GetPoziceDlePlatuAsync(int rangeMin, int rangeMax, int year)
    {
        await using var db = new DbEntities();

        return await db.PuPoliticiPrijmy
            .AsNoTracking()
            .Where(p => p.Rok == year)
            .Where(p => (((p.Plat ?? 0) + (p.Odmeny ?? 0)) / (p.PocetMesicu ?? 12)) >= rangeMin)
            .Where(p => (((p.Plat ?? 0) + (p.Odmeny ?? 0)) / (p.PocetMesicu ?? 12)) <= rangeMax)
            .Include(p => p.Organizace).ThenInclude(o => o.FirmaDs)
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
        var prijmyPolitiku = organizace.PrijmyPolitiku;
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
                await PuRepo.UpsertMetadataAsync(metadatum);
            }
        }

        if (tagy is not null)
        {
            foreach (var tag in tagy)
            {
                await PuRepo.UpsertTagAsync(tag);
            }
        }

        if (prijmyPolitiku is not null)
        {
            foreach (var plat in prijmyPolitiku)
            {
                await UpsertPrijemPolitikaAsync(plat);
            }
        }
    }

    public static async Task UpsertPrijemPolitikaAsync(PuPolitikPrijem prijemPolitika)
    {
        if (string.IsNullOrWhiteSpace(prijemPolitika.NazevFunkce))
        {
            throw new Exception("Chybí vyplněný název pozice");
        }

        if (prijemPolitika.Rok == 0)
        {
            throw new Exception("Chybí vyplněný rok pozice");
        }

        if (prijemPolitika.IdOrganizace == 0)
        {
            throw new Exception("Chybí vyplněné id organizace");
        }
        
        if (string.IsNullOrWhiteSpace(prijemPolitika.Nameid))
        {
            throw new Exception("Chybí vyplněné nameid");
        }
        
        PuPolitikPrijem origPlat;

        await using var dbContext = new DbEntities();
        if (prijemPolitika.Id == 0)
        {
            origPlat = await dbContext.PuPoliticiPrijmy
                .FirstOrDefaultAsync(p => p.IdOrganizace == prijemPolitika.IdOrganizace
                                          && p.Rok == prijemPolitika.Rok
                                          && p.Nameid == prijemPolitika.Nameid);
        }
        else
        {
            origPlat = await dbContext.PuPoliticiPrijmy
                .FirstOrDefaultAsync(p => p.Id == prijemPolitika.Id);
        }

        if (origPlat is null)
        {
            dbContext.PuPoliticiPrijmy.Add(prijemPolitika);
        }
        else
        {
            origPlat.NazevFunkce = prijemPolitika.NazevFunkce;
            origPlat.Plat = prijemPolitika.Plat;
            origPlat.Odmeny = prijemPolitika.Odmeny;
            origPlat.Prispevky = prijemPolitika.Prispevky;
            origPlat.DisplayOrder = prijemPolitika.DisplayOrder;
            origPlat.PocetMesicu = prijemPolitika.PocetMesicu;
            origPlat.NefinancniBonus = prijemPolitika.NefinancniBonus;
            origPlat.PoznamkaPlat = prijemPolitika.PoznamkaPlat;
            origPlat.SkrytaPoznamka = prijemPolitika.SkrytaPoznamka;
            origPlat.Uvolneny = prijemPolitika.Uvolneny;
            origPlat.NahradaAdministrativa = prijemPolitika.NahradaAdministrativa;
            origPlat.NahradaAsistent = prijemPolitika.NahradaAsistent;
            origPlat.NahradaCestovni = prijemPolitika.NahradaCestovni;
            origPlat.NahradaKancelar = prijemPolitika.NahradaKancelar;
            origPlat.NahradaReprezentace = prijemPolitika.NahradaReprezentace;
            origPlat.NahradaTelefon = prijemPolitika.NahradaTelefon;
            origPlat.NahradaUbytovani = prijemPolitika.NahradaUbytovani;
            
        }

        await dbContext.SaveChangesAsync();
    }
}