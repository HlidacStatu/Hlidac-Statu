using HlidacStatu.Entities;
using Microsoft.EntityFrameworkCore;
using Polly.Caching;
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
            .Where(pu => datoveSchranky.Contains(pu.DS))
            .Include(o => o.Metadata)
            .Include(o => o.Tags)
            .Include(o => o.FirmaDs)
            .Include(o => o.PrijmyPolitiku) // Include PuPrijmyPolitiku
            .FirstOrDefaultAsync();
    }



    public static List<PpPrijem> AktualniRok(this ICollection<PpPrijem> prijmy, int rok = DefaultYear)
    {
        return prijmy?.Where(m => m.Rok == rok).ToList();
    }

    public static async Task<PpPrijem> GetPrijemAsync(int id)
    {
        await using var db = new DbEntities();

        return await db.PpPrijmy
            .AsNoTracking()
            .Include(p => p.Organizace).ThenInclude(o => o.FirmaDs)
            .Include(p => p.Organizace).ThenInclude(o => o.Tags)
            .Where(p => p.Id == id)
            .FirstOrDefaultAsync();
    }

    public static async Task<PpPrijem> GetPlatAsync(int idOrganizace, int rok, string nameid)
    {
        await using var db = new DbEntities();

        return await db.PpPrijmy
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.IdOrganizace == idOrganizace
                                      && p.Rok == rok
                                      && p.Nameid == nameid);
    }

    public static async Task<PpStat> GetGlobalStatAsync(int rok = DefaultYear)
    {
        await using var db = new DbEntities();

        PpStat stat = new PpStat(rok,
            db.PpPrijmy
                .AsNoTracking()
                .Where(m => m.Rok == rok)
                .Select(m =>
                    new PpStat.SimplePlatData() { organizace = m.IdOrganizace.ToString(), osoba = m.Nameid, plat = m.HrubyMesicniPlatVcetneOdmen }
                )
            );
        return stat;
    }

    public static async Task<List<PpPrijem>> GetPrijmyPolitika(string nameid)
    {
        await using var db = new DbEntities();

        return await db.PpPrijmy
            .AsNoTracking()
            .Include(p => p.Organizace).ThenInclude(o => o.FirmaDs)
            .Include(p => p.Organizace).ThenInclude(o => o.Tags)
            .Where(p => p.Nameid == nameid)
            .ToListAsync();
    }


    public static async Task<int> GetPlatyCountAsync(int rok)
    {
        await using var db = new DbEntities();

        return await db.PpPrijmy
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

    public static async Task<List<PpPrijem>> GetPlatyAsync(int rok)
    {
        await using var db = new DbEntities();

        return await db.PpPrijmy
            .AsNoTracking()
            .Where(p => p.Rok == rok)
            .ToListAsync();
    }

    public static string PlatyForYearPoliticiDescriptionHtml(this PuOrganizace org, int rok = DefaultYear, bool withDetail = false)
    {
        var desc = org.GetMetadataDescriptionPolitici(rok);

        return $"<span class='text-{desc.BootstrapStatus}'><i class='{desc.Icon}'></i> {desc.TextStatus}{(withDetail ? $". {desc.Detail}" : "")}</span>";
    }

    public static PuOrganizaceMetadata.Description GetMetadataDescriptionPolitici(this PuOrganizace org, int rok = DefaultYear)
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

    public static async Task<List<PpPrijem>> GetPlatyWithOrganizaceForYearAsync(int rok)
    {
        await using var db = new DbEntities();

        return await db.PpPrijmy
            .AsNoTracking()
            .Include(p => p.Organizace)
            .ThenInclude(o => o.FirmaDs)
            .Where(p => p.Rok == rok)
            .ToListAsync();
    }

    public static async Task<List<PpPrijem>> GetPoziceDlePlatuAsync(int rangeMin, int rangeMax, int year)
    {
        await using var db = new DbEntities();

        return await db.PpPrijmy
            .AsNoTracking()
            .Where(p => p.Rok == year)
            .Where(p => (((p.Plat ?? 0) + (p.Odmeny ?? 0)) / (p.PocetMesicu ?? 12)) >= rangeMin)
            .Where(p => (((p.Plat ?? 0) + (p.Odmeny ?? 0)) / (p.PocetMesicu ?? 12)) <= rangeMax)
            .Include(p => p.Organizace).ThenInclude(o => o.FirmaDs)
            .ToListAsync();
    }
    public static async Task UpsertEventAsync(PpEvent _event)
    {
        PpEvent ev = _event;
        await using var dbContext = new DbEntities();

        if (string.IsNullOrWhiteSpace(ev.IcoOrganizace))
        {
            throw new ArgumentException("ICO is missing");
        }
        if (string.IsNullOrWhiteSpace(ev.OsobaNameId))
        {
            throw new ArgumentException("OsobaNameId is missing");
        }

        var original = await dbContext.PpEvents.FirstOrDefaultAsync(o => o.Pk == ev.Pk);
        if (original is null || ev.Pk == 0)
        {
            dbContext.PpEvents.Add(ev);
        }
        else
        {
            dbContext.PpEvents.Attach(ev);
            dbContext.Entry(ev).State = EntityState.Modified;
        }

        await dbContext.SaveChangesAsync();

    }
    public static async Task<string> GetNewCisloJednaciAsync()
    {
        await using var dbContext = new DbEntities();
        var existing = await dbContext.PpEvents
            .AsNoTracking()
            .Where(m => m.NaseCJ != null)
            .Select(m => m.NaseCJ)
            .Distinct()
            .ToArrayAsync();

        if (existing.Any() == false)
            return $"1/{DateTime.Now.Year}";

        var parts = existing
            .Select(m => m.Split('/'))
            .Where(m => m.Length == 2)
            .Select(m => new { cislo = int.Parse(m[0]), rok = int.Parse(m[1]) })
            .Where(m => m.rok == DateTime.Now.Year)
            .OrderByDescending(m => m.cislo)
            .FirstOrDefault();
        if (parts == null)
            return $"1/{DateTime.Now.Year}";

        return $"{parts.cislo + 1}/{DateTime.Now.Year}";
    }

    public static async Task<PpEvent> GetEventAsync(int id)
    {
        await using var dbContext = new DbEntities();
        return await dbContext.PpEvents
            .AsNoTracking()
            .Where(e => e.Pk == id)
            .FirstOrDefaultAsync();
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

    public static async Task UpsertPrijemPolitikaAsync(PpPrijem prijemPolitika)
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

        PpPrijem origPlat;

        await using var dbContext = new DbEntities();
        if (prijemPolitika.Id == 0)
        {
            origPlat = await dbContext.PpPrijmy
                .FirstOrDefaultAsync(p => p.IdOrganizace == prijemPolitika.IdOrganizace
                                          && p.Rok == prijemPolitika.Rok
                                          && p.Nameid == prijemPolitika.Nameid);
        }
        else
        {
            origPlat = await dbContext.PpPrijmy
                .FirstOrDefaultAsync(p => p.Id == prijemPolitika.Id);
        }

        if (origPlat is null)
        {
            dbContext.PpPrijmy.Add(prijemPolitika);
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