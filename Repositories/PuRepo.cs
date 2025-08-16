using Devmasters.Collections;
using EnumsNET;
using HlidacStatu.Connectors;
using HlidacStatu.Entities;
using HlidacStatu.Entities;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HlidacStatu.Repositories;

public static class PuRepo
{
    /*
     List PU_organizace s prelozenymi CZISCO
    select o.id,f.Jmeno, o.DS, t2.nazev

from PU_Organizace o 
	cross apply (select nazev from string_split(o.CZISCO,',') ss inner join PU_ISPV_CZISCO cz on ss.value = cz.kod) t2

inner join Firma_DS fds
	on o.DS = fds.DatovaSchranka
	inner join firma f on f.ICO = fds.ICO


     */

    public const int DefaultYear = 2024;
    public const int MinYear = 2016;

    public static int[] AllYears = Enumerable.Range(MinYear, DefaultYear - MinYear + 1).ToArray();

    public static readonly string[] MainTags =
    [
        "bezpečnost",
        "centrální a kontrolní instituce",
        "doprava",
        "finance",
        "justice",
        "kraje",
        "kultura",
        "ministerstvo",
        "nemocnice",
        "práce",
        "průmysl a obchod",
        "rozvoj",
        "služby",
        "společnost",
        "výzkum",
        "vzdělávání",
        "zdravotnictví",
        "zemědělství",
        "životní prostředí",
        "ostatní"
    ];

    public static async Task SaveVydelek(PuVydelek vydelek)
    {
        await SaveVydelky(new[] { vydelek });
    }

    public static async Task SaveVydelky(IEnumerable<PuVydelek> vydelky)
    {
        await using var db = new DbEntities();

        foreach (var vydelek in vydelky)
        {
            db.PuVydelky.Attach(vydelek);
            if (vydelek.Pk == 0)
            {
                db.Entry(vydelek).State = EntityState.Added;
            }
            else
                db.Entry(vydelek).State = EntityState.Modified;
        }

        await db.SaveChangesAsync();
    }

    public static async Task<PuVydelek[]> LoadVydelekDataAsync(int rok, int level = 4)
    {
        await using var db = new DbEntities();
        PuVydelek[] res = await db.PuVydelky
            .Where(m => m.Rok == rok
                        && m.Level == level)
            .ToArrayAsync();

        return res;
    }

    public static async Task<PuVydelek[]> LoadVydelekForZamestnaniAsync(string cz_ISCO, PuVydelek.VydelekSektor? sektor)
    {
        await using var db = new DbEntities();
        var query = db.PuVydelky
            .Where(m => m.CZ_ISCO == cz_ISCO);
        if (sektor.HasValue)
            query = query.Where(m => m.SektorId == (int)sektor);

        PuVydelek[] res = await query.ToArrayAsync();

        return res;
    }

    public static Devmasters.Cache.LocalMemory.ManagerAsync<PuPlatStat, int> GetPlatyPerYearStatCache =
        new Devmasters.Cache.LocalMemory.ManagerAsync<PuPlatStat, int>("GetPlatyUrednikuPerYearStatCache",
            async rok =>
            {
                var data = await PuRepo.GetPlatyAsync(rok);
                if (data == null)
                    return new PuPlatStat(Array.Empty<PuPlat>());
                var platyStat = new PuPlatStat(data);

                return platyStat;
            }, TimeSpan.FromMinutes(60), m => m.ToString());


    public static async Task<PuRokOrganizaceStat> GetGlobalStatOrganizaceAsync(int rok = DefaultYear)
    {
     
        await using var db = new DbEntities();

        PuRokOrganizaceStat stat = new PuRokOrganizaceStat();
        stat.PocetOslovenych = await db.PuOrganizaceMetadata
            .Where(m => m.Typ == PuOrganizaceMetadata.TypMetadat.PlatyUredniku)
            .CountAsync(m => m.DatumOdeslaniZadosti.HasValue && m.Rok == rok);

        stat.PocetCoPoslaliPlat = await db.PuPlaty
            .Where(m => m.Rok == rok)
            .Select(m => m.IdOrganizace)
            .Distinct()
            .CountAsync();

        var platyRok = db.PuPlaty
            .AsNoTracking()
            .Where(m => m.Rok == rok)
            .Select(m => new { mplat = m.HrubyMesicniPlatVcetneOdmen, plat = m, org = m.Organizace })
            .ToArray()
            .OrderBy(o => o.mplat)
            .ToArray();

        var platyRokHlavouni = db.PuPlaty
            .AsNoTracking()
            .Where(m => m.Rok == rok && m.JeHlavoun == true)
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
        return await GetFullDetailAsync(new string[] { datovaSchranka });
    }
    public static async Task<PuOrganizace> GetFullDetailAsync(string[] datovaSchranky)
    {
        await using var db = new DbEntities();

        return await db.PuOrganizace
            .AsNoTracking()
            .Where(pu => datovaSchranky.Contains(pu.DS))
            .Include(o => o.Metadata)
            .Include(o => o.Tags)
            .Include(o => o.FirmaDs)
            .Include(o => o.Platy) // Include PuPlat
            .FirstOrDefaultAsync();
    }

    public static async Task<List<PuOrganizace>> ExportAllAsync(string? datovaSchranka, int? year)
    {
        await using var db = new DbEntities();

        IQueryable<PuOrganizace> query = db.PuOrganizace
            .AsNoTracking()
            .Include(o => o.Metadata)
            .Include(o => o.Tags)
            .Include(o => o.FirmaDs)
            .Include(o => o.Platy);

        if (!string.IsNullOrEmpty(datovaSchranka))
        {
            query = query.Where(pu => pu.DS == datovaSchranka);
        }

        if (year.HasValue)
        {
            query = query.Where(pu =>
                pu.Metadata.Any(m => m.Rok == year.Value) || pu.Platy.Any(p => p.Rok == year.Value));
        }
        var data = await query.ToListAsync();
        data = data
            .Where(o => o.Tags.Any())
            .ToList();

        return data;
    }

    public static async Task<PuOrganizace> GetOrganizationOfTheDayAsync()
    {
        await using var db = new DbEntities();

        var orgs = await db.PuOrganizace
            .AsNoTracking()
            .Include(o => o.Tags)
            .Where(o => o.Tags.Any())
            .ToListAsync();
        var tip = orgs.TipOfTheDay();

        return await db.PuOrganizace
            .AsNoTracking()
            .Where(pu => pu.DS == tip.DS)
            .Include(o => o.Metadata.Where(m => m.Typ == PuOrganizaceMetadata.TypMetadat.PlatyUredniku))
            .Include(o => o.Tags)
            //.Include(o => o.FirmaDs)
            .Include(o => o.Platy) // Include PuPlat
            .FirstOrDefaultAsync();
    }

    public static List<PuPlat> AktualniRok(this ICollection<PuPlat> platy, int rok = DefaultYear)
    {
        return platy?.Where(m => m.Rok == rok).ToList();
    }

    public static List<PuOrganizaceMetadata> AktualniRok(this ICollection<PuOrganizaceMetadata> metadata, int rok = DefaultYear)
    {
        return metadata?.Where(m => m.Rok == rok).ToList();
    }


    public static PuOrganizaceMetadata.Description GetMetadataDescriptionUrednici(this PuOrganizace org, int rok = DefaultYear)
    {
        var res = new PuOrganizaceMetadata.Description();

        var metadataList = org.MetadataPlatyUredniku.Where(m => m.Rok == rok && m.Typ == PuOrganizaceMetadata.TypMetadat.PlatyUredniku).ToList();
        //ted pracuju pouze s jednou
        var metadata = metadataList.FirstOrDefault();

        if (org.Platy.AktualniRok().Count == 0 && PuRepo.GetNeaktivniOrganizace().ToArray().Any(m => m.Item1 == org.DS))
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
        else if (org.Platy.AktualniRok(rok).Count == 0)
        {
            res.TextStatus = "Odmítli poskytnout platy";
            res.Detail = metadata.PoznamkaHlidace;
            res.BootstrapStatus = "danger";
            res.Icon = "fa-solid fa-circle-xmark";
        }
        else if (org.Platy.AktualniRok(rok).Count == 1)
        {
            res.TextStatus = "Evidujeme jeden plat jedné pozice";
            res.Detail = metadata.PoznamkaHlidace;
            res.BootstrapStatus = "warning";
            res.Icon = "fa-solid fa-circle-exclamation";
        }
        else if (org.Platy.AktualniRok(rok).Count < 5)
        {
            res.TextStatus = $"{org.Platy.AktualniRok(rok).Count} platy";
            res.Detail = metadata.PoznamkaHlidace;
            res.BootstrapStatus = "success";
            res.Icon = "fa-solid fa-badge-check";
        }
        else
        {
            res.TextStatus = $"{org.Platy.AktualniRok(rok).Count} platů";
            res.Detail = metadata.PoznamkaHlidace;
            res.BootstrapStatus = "success";
            res.Icon = "fa-solid fa-badge-check";
        }
        return res;
    }



    public static string PlatyForYearUredniciDescription(this PuOrganizace org, int rok = DefaultYear)
    {
        var desc = org.GetMetadataDescriptionUrednici(rok);
        return desc.TextStatus;
    }


    public static string PlatyForYearUredniciDescriptionHtml(this PuOrganizace org, int rok = DefaultYear, bool withDetail = false)
    {
        var desc = org.GetMetadataDescriptionUrednici(rok);

        return $"<span class='text-{desc.BootstrapStatus}'><i class='{desc.Icon}'></i> {desc.TextStatus}{(withDetail ? $". {desc.Detail}" : "")}</span>";
    }


    static Devmasters.Cache.LocalMemory.AutoUpdatedCache<IEnumerable<Tuple<string, string>>> _neaktivniOrganizaceCache
        = new Devmasters.Cache.LocalMemory.AutoUpdatedCache<IEnumerable<Tuple<string, string>>>(TimeSpan.FromHours(1), "NeaktivniOrganizace",
            (o) =>
            {
                return DirectDB.GetList<string, string>(@"
select distinct ds.DatovaSchranka, f.ico from firma f 
	inner join Firma_DS ds on f.ICO=ds.ICO
	inner join PU_Organizace puo on ds.DatovaSchranka = puo.DS
	where f.status>1 	
");
            }

            );

    /// <summary>
    /// dvojice datovaschranka , ICO
    /// </summary>
    /// <returns></returns>
    public static IEnumerable<Tuple<string, string>> GetNeaktivniOrganizace()
    {
        IEnumerable<Tuple<string, string>> nonActiveDS = _neaktivniOrganizaceCache.Get();

        return nonActiveDS;
    }

    public static async Task<List<PuOrganizace>> GetActiveOrganizaceForTagAsync(string tag, int limit = 0)
    {
        var normalizedTag = PuOrganizaceTag.NormalizeTag(tag);
        
        await using var db = new DbEntities();

        var query = db.PuOrganizaceTags
            .AsNoTracking()
            .Where(t => tag == null || t.TagNormalized.Equals(normalizedTag))
            .Where(t => t.Organizace.Metadata.Any(m => m.Typ == PuOrganizaceMetadata.TypMetadat.PlatyUredniku))
            .Include(t => t.Organizace).ThenInclude(o => o.FirmaDs)
            .Include(t => t.Organizace).ThenInclude(o => o.Metadata.Where(m => m.Typ == PuOrganizaceMetadata.TypMetadat.PlatyUredniku))
            .Include(t => t.Organizace).ThenInclude(o => o.Platy)
            .Select(t => t.Organizace);

        if (limit > 0)
            query = query.Take(limit);

        return await query.ToListAsync();
    }
    
    public static async Task<PuOrganizaceTag> GetTagAsync(string tag)
    {
        var normalizedTag = PuOrganizaceTag.NormalizeTag(tag);
        
        await using var db = new DbEntities();

        return await db.PuOrganizaceTags.AsNoTracking().FirstOrDefaultAsync(t => t.TagNormalized.Equals(normalizedTag));
    }

    public static async Task<int> GetOrganizaceCountForTagAsync(string tag, int rok)
    {
        await using var db = new DbEntities();
        try
        {
            var listOfCounts = await db.PuOrganizaceTags
                .AsNoTracking()
                .Where(t => t.Tag.Equals(tag))
                .Include(t => t.Organizace).ThenInclude(o => o.Platy)
                .Select(m => m.Organizace.Platy.Count(c => c.Rok == rok))
                .ToArrayAsync();


            return listOfCounts.Sum();
        }
        catch (Exception e)
        {
            throw;
        }
    }

    public static async Task<PuPlat> GetPlatAsync(int id)
    {
        await using var db = new DbEntities();

        return await db.PuPlaty
            .AsNoTracking()
            .Include(p => p.Organizace).ThenInclude(o => o.FirmaDs)
            .Include(p => p.Organizace).ThenInclude(o => o.Tags)
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


    public static async Task<int> GetPlatyCountAsync(int rok)
    {
        await using var db = new DbEntities();

        return await db.PuPlaty
            .AsNoTracking()
            .Where(p => p.Rok == rok)
            .CountAsync();
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
            .Include(p => p.Organizace).ThenInclude(o => o.FirmaDs)
            .ToListAsync();
    }

    public static async Task<PuOrganizace> UpsertOrganizaceAsync(PuOrganizace organizace)
    {
        PuOrganizace res = null;
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
            await dbContext.SaveChangesAsync();
            res = organizace;
        }
        else
        {
            original.Info = organizace.Info;
            original.HiddenNote = organizace.HiddenNote;
            await dbContext.SaveChangesAsync();
            res = original;
        }



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
        return res;
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
            origMetadata.PoznamkaHlidace = metadatum.PoznamkaHlidace;
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
            plat.DateCreated = DateTime.Now;
            plat.DateModified = DateTime.Now;
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
            origPlat.SkrytaPoznamka = plat.SkrytaPoznamka;
            origPlat.DateModified = DateTime.Now;
        }

        await dbContext.SaveChangesAsync();
    }

    public static async Task<List<PuOrganizace>> GetPlatyForYearsAsync(int minYear, int lastYear)
    {
        await using var db = new DbEntities();

        var result = await db.PuOrganizace
            .AsNoTracking()
            .Include(o => o.FirmaDs)
            .Include(o => o.Tags)
            .Select(o => new
            {
                o, // Include the main entity
                Platy = o.Platy.Where(p => p.Rok == minYear || p.Rok == lastYear).ToList()
            })
            .ToArrayAsync();
        result = result
            .Where(m => m.o.Tags.Any())
            .ToArray();

        // Map the result back to the list of PuOrganizace with filtered Platy
        return result.Select(x =>
        {
            x.o.Platy = x.Platy; // Assign the filtered Platy list back to the PuOrganizace entity
            return x.o;
        }).ToList();
    }
    

    public static async Task<PuEvent> UpsertEventAsync(PuEvent _event, bool addNewCj)
    {
        await using var dbContext = new DbEntities();

        if (string.IsNullOrWhiteSpace(_event.IcoOrganizace))
        {
            throw new ArgumentException("ICO is missing");
        }
        if (string.IsNullOrWhiteSpace(_event.OsobaNameId))
        {
            throw new ArgumentException("OsobaNameId is missing");
        }

        PuEvent? original = null;
        if (_event.Pk == 0)
        {
            original = await dbContext.PuEvents.FirstOrDefaultAsync(o => o.IdOrganizace == _event.IdOrganizace 
                                                                         && o.OsobaNameId == _event.OsobaNameId 
                                                                         && o.Kanal == _event.Kanal 
                                                                         && o.ProRok == _event.ProRok
                                                                         && o.DotazovanaInformace == _event.DotazovanaInformace
                                                                         && o.Smer == _event.Smer
                                                                         && o.Typ == _event.Typ
                                                                         && o.Kanal == _event.Kanal);
        }
        else
        {
            original = await dbContext.PuEvents.FirstOrDefaultAsync(o => o.Pk == _event.Pk);
        }
        
        if (original is null)
        {
            dbContext.PuEvents.Add(_event);
        }
        else
        {
            _event.Pk = original.Pk;
            dbContext.Entry(original).CurrentValues.SetValues(_event);
        }
        if (addNewCj && string.IsNullOrEmpty(_event.NaseCJ))
            _event.NaseCJ = await GetNewCisloJednaciAsync(_event.DotazovanaInformace);
        await dbContext.SaveChangesAsync();

        return _event;

    }

    public static async Task<string> GetNewCisloJednaciAsync(PuEvent.DruhDotazovaneInformace druh)
    {
        string postfix = "OB";
        if (druh == PuEvent.DruhDotazovaneInformace.Urednik)
            postfix = "UR";
        else if (druh == PuEvent.DruhDotazovaneInformace.Politik)
            postfix = "PO";

        await using var dbContext = new DbEntities();
        var existing = await dbContext.PuEvents
            .AsNoTracking()
            .Where(m => m.NaseCJ != null)
            .Select(m => m.NaseCJ)
            .Distinct()
            .ToArrayAsync();

        if (existing.Any() == false)
            return $"1/{DateTime.Now.Year}/{postfix}";

        var parts = existing
            .Select(m => m.Split('/'))
            .Where(m => m.Length == 3)
            .Select(m => new { cislo = int.Parse(m[0]), rok = int.Parse(m[1]), druh = m[2] })
            .Where(m => m.rok == DateTime.Now.Year && m.druh==postfix)
            .OrderByDescending(m => m.cislo)
            .FirstOrDefault();
        if (parts == null)
            return $"1/{DateTime.Now.Year}/{postfix}";

        return $"{parts.cislo + 1}/{DateTime.Now.Year}/{postfix}";
    }

    public static async Task<PuOrganizace> GetOrganizaceForIcoAsync(string ico)
    {
        if (string.IsNullOrWhiteSpace(ico))
            return null;

        await using var db = new DbEntities();
        
        var foundFirmy = await db.FirmaDs.AsNoTracking()
            .Where(o => o.Ico == ico && o.DsParent == null)
            .ToListAsync();

        if (foundFirmy is null || !foundFirmy.Any())
            return null;

        foreach (var foundFirma in foundFirmy)
        {
            var foundPuOrg = await db.PuOrganizace.AsNoTracking()
                .Where(o => o.DS == foundFirma.DatovaSchranka)
                .FirstOrDefaultAsync();

            if (foundPuOrg is not null)
            {
                return foundPuOrg;
            }
        }
        
        var firstCompany = foundFirmy.FirstOrDefault();
        
        // create new org
        var newOrganizace = new PuOrganizace()
        {
            DS = firstCompany.DatovaSchranka
        };
        await PpRepo.UpsertOrganizaceAsync(newOrganizace);
        
        return newOrganizace;
    }
}