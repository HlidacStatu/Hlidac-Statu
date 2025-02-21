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
    public static async Task<PuOrganizace> GetFullDetailAsync(string[] datovaSchranky)
    {
        await using var db = new DbEntities();

        return await db.PuOrganizace
            .AsNoTracking()
            .Where(pu => datovaSchranky.Contains(pu.DS) )
            .Include(o => o.Metadata)
            .Include(o => o.Tags)
            .Include(o => o.FirmaDs)
            .Include(o => o.Platy) // Include PuPlat
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


    public static async Task<int> GetPlatyCountAsync(int rok)
    {
        await using var db = new DbEntities();

        return await db.PuPoliticiPrijmy
            .AsNoTracking()
            .Where(p => p.Rok == rok)
            .CountAsync();
    }

    public static async Task<List<PuPolitikPrijem>> GetPlatyAsync(int rok)
    {
        await using var db = new DbEntities();

        return await db.PuPoliticiPrijmy
            .AsNoTracking()
            .Where(p => p.Rok == rok)
            .ToListAsync();
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