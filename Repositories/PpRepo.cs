using HlidacStatu.Entities;
using HlidacStatu.Extensions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using HlidacStatu.Entities.Entities;
using Devmasters.Enums;

namespace HlidacStatu.Repositories;

public static class PpRepo
{
    public const int DefaultYear = 2024;
    public const int MinYear = 2024;

    public static async Task<PuOrganizace> GetOrganizaceFullDetailAsync(string datovaSchranka)
    {
        return await GetOrganizaceFullDetailAsync(new string[] { datovaSchranka });
    }

    public static async Task<PuOrganizace> GetOrganizaceFullDetailPerIcoAsync(string ico)
    {
        if (HlidacStatu.Util.DataValidators.CheckCZICO(ico))
        {
            Firma f = Firmy.Get(ico);
            return await GetOrganizaceFullDetailAsync(f.DatovaSchranka);
        }
        else return null;
    }

    public static async Task<PuOrganizace> GetOrganizaceFullDetailAsync(string[] datoveSchranky)
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

    public static async Task<PuOrganizace> GetOrganizaceOnly(string datovaSchranka)
    {
        await using var db = new DbEntities();

        return await db.PuOrganizace
            .AsNoTracking()
            .Where(pu => pu.DS == datovaSchranka)
            .Include(o => o.FirmaDs)
            .FirstOrDefaultAsync();
    }

    public static IQueryable<PpPrijem> BaseAllPlaty(DbEntities db, int rok = DefaultYear)
    {

        return db.PpPrijmy
            .Where(m => m.Rok == rok);
    }

    public static IQueryable<PpPrijem> BasePotvrzenePlaty(DbEntities db, int rok = DefaultYear)
    {

        return BaseAllPlaty(db, rok)
            .Where(m=> m.Status == PpPrijem.StatusPlatu.Potvrzen);
    }

    public static List<PpPrijem> AktualniRok(this ICollection<PpPrijem> prijmy, int rok = DefaultYear)
    {
        using var db = new DbEntities();

        return BasePotvrzenePlaty(db,rok).Where(m => m.Rok == rok).ToList();
    }

    public static async Task<PpPrijem> GetPrijemAsync(int id)
    {
        await using var db = new DbEntities();

        return await BasePotvrzenePlaty(db)
            .AsNoTracking()
            .Include(p => p.Organizace).ThenInclude(o => o.FirmaDs)
            .Include(p => p.Organizace).ThenInclude(o => o.Tags)
            .Where(p => p.Id == id)
            .FirstOrDefaultAsync();
    }

    static IEqualityComparer<Firma> icoComparer = new FirmaByIcoComparer();

    public static async Task<List<Firma>> FindStatniFirmyAsync(string nameId, Devmasters.DT.DateInterval obdobi)
    {
        /// ico- nameid[]
        List<Firma> res = new();

        Osoba o = Osoby.GetByNameId.Get(nameId);
        if (o != null)
        {
            var vazby = o.PrimaAngazovanost(HlidacStatu.DS.Graphs.Relation.AktualnostType.Nedavny);
            var d1 = vazby
                .Where(v => Devmasters.DT.DateInterval.IsOverlappingIntervals(
                    new Devmasters.DT.DateInterval(v.RelFrom, v.RelTo), obdobi))
                .Select(m => m.To.Id)
                .Select(m => Firmy.Get(m)).Where(f => f.Valid).ToArray();
            var d2 = d1
                .Where(f => f.JsemStatniFirma() || f.JsemOVM()).ToArray();
            if (d2.Any())
            {
                //Console.WriteLine($" : {string.Join(",", d2.Select(m => m.Jmeno).Distinct())}");
                var d3 = d2
                    .Distinct(icoComparer).ToArray();
                res.AddRange(d3);
            }
        }


        return res;
    }

    public static async Task<PpPrijem> GetPlatAsync(int idOrganizace, int rok, string nameid)
    {
        await using var db = new DbEntities();

        return await BasePotvrzenePlaty(db,rok)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.IdOrganizace == idOrganizace                                      
                                      && p.Nameid == nameid);
    }


    public static async Task<List<PpPrijem>> GetPrijmyPolitika(string nameid, int rok = DefaultYear)
    {
        await using var db = new DbEntities();

        return await BasePotvrzenePlaty(db, rok)
            .AsNoTracking()
            .Include(p => p.Organizace).ThenInclude(o => o.FirmaDs)
            .Include(p => p.Organizace).ThenInclude(o => o.Tags)
            .Where(p => p.Nameid == nameid)
            .ToListAsync();
    }

    public static Osoba GetOsoba(this PpPrijem prijem)
    {
        if (string.IsNullOrEmpty(prijem.Nameid))
            return null;
        else
            return Osoby.GetByNameId.Get(prijem.Nameid);
    }


    public static async Task<int> GetPlatyCountAsync(int rok)
    {
        await using var db = new DbEntities();

        return await BasePotvrzenePlaty(db,rok)
            .AsNoTracking()
            .CountAsync();
    }

    public static async Task<List<PuEvent>> GetOrganizationEventsAsync(this PuOrganizace org, int rok,
        Expression<Func<PuEvent, bool>> predicate = null)
    {
        await using var db = new DbEntities();

        var query = db.PuEvents
                .AsNoTracking()
                .Where(m => m.IdOrganizace == org.Id && m.ProRok == rok)
            ;
        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        return await query.ToListAsync();
    }

    public static async Task<PuEvent> ZahajeniDotazovaniPlatuPolitikaAsync(Firma firma, string osobaNameId, int rok,
        string naseCj)
    {
        await using var db = new DbEntities();

        //existuje PuOrganizace?
        var puorg = await PpRepo.GetOrganizaceFullDetailAsync(firma.DatovaSchranka);
        if (puorg == null)
            puorg = await PuRepo.UpsertOrganizaceAsync(new PuOrganizace()
            {
                DS = firma.DatovaSchranka.FirstOrDefault()
            });

        PuEvent ev = new PuEvent()
        {
            ProRok = rok,
            DotazovanaInformace = PuEvent.DruhDotazovaneInformace.Politik,
            IdOrganizace = puorg.Id,
            IcoOrganizace = firma.ICO,
            DsOrganizace = firma.DatovaSchranka.FirstOrDefault() ?? "",
            Datum = DateTime.Now,
            Typ = PuEvent.TypUdalosti.ZaslaniZadosti,
            Smer = PuEvent.SmerKomunikace.ZpravaOdNas,
            Kanal = PuEvent.KomunikacniKanal.DatovaSchranka,
            NaseCJ = naseCj,
            Poznamka = "Zaslání žádosti o informace o platech a odměnách",
            OsobaNameId = osobaNameId,
        };
        ev = await PuRepo.UpsertEventAsync(ev, false);

        //existuje zaznam s platem?
        var existsPlat = await db.PpPrijmy
            .AsNoTracking()
            .Where(m => m.IdOrganizace == puorg.Id && m.Rok == rok && m.Nameid == osobaNameId)
            .FirstOrDefaultAsync();
        if (existsPlat == null)
        {
            await PpRepo.UpsertPrijemPolitikaAsync(new PpPrijem()
            {
                IdOrganizace = puorg.Id,
                Rok = rok,
                Nameid = osobaNameId,
                NazevFunkce = "",
                Plat = 0,
                PocetMesicu = 0,
                Status = PpPrijem.StatusPlatu.Zjistujeme
            });
        }

        return ev;
    }

    public static async Task<List<PuEvent>> GetAllEventsAsync(int rok,
        Expression<Func<PuEvent, bool>> predicate = null)
    {
        await using var db = new DbEntities();

        var query = db.PuEvents
                .AsNoTracking()
                .Where(m => m.ProRok == rok)
            ;
        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        return await query.ToListAsync();
    }

    public static async Task<List<PuOrganizace>> GetActiveOrganizaceAsync(int rok, string tag = null, string ico = null,
        string ds = null, int limit = 0)
    {
        await using var db = new DbEntities();
        var orgsInEvents = db.PuEvents
            .Where(m => m.ProRok == rok && m.DotazovanaInformace == PuEvent.DruhDotazovaneInformace.Politik)
            .Select(m => m.IdOrganizace)
            .Distinct();

        var query = db.PuOrganizace
                .AsNoTracking()
                .Where(m => orgsInEvents.Contains(m.Id))
                .Include(t => t.FirmaDs)
                .Include(t => t.PrijmyPolitiku.Where(p => p.Rok == rok && p.Status == PpPrijem.StatusPlatu.Potvrzen))
                .Include(t => t.Tags)
                .Where(m => tag == null || m.Tags.Any(t => t.Tag == tag))
                .Where(m => ico == null || m.FirmaDs.Ico == ico)
                .Where(m => ds == null || m.DS == ds)
            ;

        if (limit > 0)
            query = query.Take(limit);

        return await query.ToListAsync();
    }

    public static readonly string[] MainTags =
    [
        "politici",
    ];

    public static async Task<List<PpPrijem>> GetPlatyAsync(int rok, bool withDetails = false, string ico = null)
    {
        await using var db = new DbEntities();

        var q = BasePotvrzenePlaty(db,rok)
            .AsNoTracking();

        if (withDetails || string.IsNullOrEmpty(ico) == false)
            q = q.Include(i => i.Organizace).ThenInclude(o => o.FirmaDs);
        if (!string.IsNullOrEmpty(ico))
            q = q.Where(m => m.Organizace.FirmaDs.Ico == ico);

        q = q.Where(p => p.Rok == rok);

        var res = await q.ToListAsync();

        return res;
    }

    public static string PlatyForYearPoliticiDescriptionHtml(this PuOrganizace org, int rok = DefaultYear,
        bool withDetail = false)
    {
        var desc = org.GetMetadataDescriptionPolitici(rok);

        return
            $"<span class='text-{desc.BootstrapStatus}'><i class='{desc.Icon}'></i> {desc.TextStatus}{(withDetail ? $". {desc.Detail}" : "")}</span>";
    }

    public static PuOrganizaceMetadata.Description GetMetadataDescriptionPolitici(this PuOrganizace org,
        int rok = DefaultYear)
    {
        var res = new PuOrganizaceMetadata.Description();

        var events = org
            .GetOrganizationEventsAsync(rok, m => m.DotazovanaInformace == PuEvent.DruhDotazovaneInformace.Politik)
            .ConfigureAwait(false).GetAwaiter().GetResult();
        // MetadataPlatyUredniku.Where(m => m.Rok == rok && m.Typ == PuOrganizaceMetadata.TypMetadat.PlatyPolitiku).ToList();

        if (
            events.Any(m => m.Typ == PuEvent.TypUdalosti.ZaslaniZadosti) == false
            && events.Any(m => m.Typ == PuEvent.TypUdalosti.PoskytnutiInformace) == false
        )
        {
            res.TextStatus = "Této organizace jsme se na platy neptali";
            res.Detail = "";
            res.BootstrapStatus = "primary";
            res.Icon = "fa-solid fa-question-circle";
        }
        else if (
            events.Any(m => m.Typ == PuEvent.TypUdalosti.ZaslaniZadosti)
            &&
            events.Any(m => m.Typ == PuEvent.TypUdalosti.PoskytnutiInformace) == false
        )
        {
            res.TextStatus = $"{events.Max(m => m.Datum):d. M. yyyy} Odeslána žádost o platy";
            res.Detail = "Data jsme zatím nedostali nebo nezpracovali.";
            res.BootstrapStatus = "primary";
            res.Icon = "fa-solid fa-question-circle";
        }
        else if (
            events.Any(m => m.Typ == PuEvent.TypUdalosti.ZaslaniZadosti)
            &&
            events.Any(m => m.Typ == PuEvent.TypUdalosti.UplneOdmitnutiPoskytnutiInformaci)
        )
        {
            res.TextStatus = "Odmítli poskytnout platy";
            res.Detail = string.Join(". ", events
                .Where(m => m.Typ == PuEvent.TypUdalosti.UplneOdmitnutiPoskytnutiInformaci)
                .Select(m => m.Poznamka));
            res.BootstrapStatus = "danger";
            res.Icon = "fa-solid fa-circle-xmark";
        }
        else if (
            events.Any(m => m.Typ == PuEvent.TypUdalosti.ZaslaniZadosti)
            &&
            events.Any(m => m.Typ == PuEvent.TypUdalosti.CastecneOdmitnutiPoskytnutiInformaci)
        )
        {
            res.TextStatus = "Poskytli pouze část požadovaných informací";
            res.Detail = string.Join(". ", events
                .Where(m => m.Typ == PuEvent.TypUdalosti.CastecneOdmitnutiPoskytnutiInformaci)
                .Select(m => m.Poznamka));
            res.BootstrapStatus = "danger";
            res.Icon = "fa-solid fa-circle-xmark";
        }
        else
        {
            res.TextStatus = $"Požadované platy nám poskytli v plném rozsahu";
            res.Detail = string.Join(". ", events
                .Where(m => m.Typ == PuEvent.TypUdalosti.PoskytnutiInformace)
                .Select(m => m.Poznamka));
            res.BootstrapStatus = "success";
            res.Icon = "fa-solid fa-badge-check";
        }

        return res;
    }

    public static async Task<List<PpPrijem>> GetPlatyWithOrganizaceForYearAsync(int rok)
    {
        await using var db = new DbEntities();

        return await BasePotvrzenePlaty(db,rok)
            .AsNoTracking()
            .Include(p => p.Organizace)
            .ThenInclude(o => o.FirmaDs)
            .ToListAsync();
    }

    public static async Task<List<PpPrijem>> GetPoziceDlePlatuAsync(int rangeMin, int rangeMax, int year)
    {
        await using var db = new DbEntities();

        return await BasePotvrzenePlaty(db,year)
            .AsNoTracking()
            .Where(p => (((p.Plat ?? 0) + (p.Odmeny ?? 0)) / (p.PocetMesicu ?? 12)) >= rangeMin)
            .Where(p => (((p.Plat ?? 0) + (p.Odmeny ?? 0)) / (p.PocetMesicu ?? 12)) <= rangeMax)
            .Include(p => p.Organizace).ThenInclude(o => o.FirmaDs)
            .ToListAsync();
    }


    public static async Task<PuEvent> GetEventAsync(int id)
    {
        await using var dbContext = new DbEntities();
        return await dbContext.PuEvents
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
            prijemPolitika.Id = origPlat.Id;
            dbContext.Entry(origPlat).CurrentValues.SetValues(prijemPolitika);
        }

        await dbContext.SaveChangesAsync();
    }
    
    public static async Task DeletePrijemPolitikaAsync(PpPrijem prijemPolitika)
    {
        await using var dbContext = new DbEntities();
        dbContext.PpPrijmy.Remove(prijemPolitika);
        await dbContext.SaveChangesAsync();
    }

    [Devmasters.Enums.ShowNiceDisplayName]
    public enum PoliticianGroup
    {
        [NiceDisplayName("Všichni politici")]
        Vse,
        [NiceDisplayName("Poslanci")]

        Poslanci,
        [NiceDisplayName("Senátoři")]
        Senatori,
        [NiceDisplayName("Krajští zastupitelé")]
        KrajstiZastupitele,
        [NiceDisplayName("Členové vlády")]
        Vlada
    }

    private static string[] GetIcaForGroup(PoliticianGroup group) =>
        group switch
        {
            PoliticianGroup.Vse => [],
            PoliticianGroup.Poslanci => [Constants.Ica.KancelarPoslaneckeSnemovny],
            PoliticianGroup.Senatori => [Constants.Ica.Senat],
            PoliticianGroup.KrajstiZastupitele => Constants.Ica.Kraje,
            _ => []
        };

    public static async Task<List<PpPrijem>> GetPrijmyForGroupAsync(PoliticianGroup group, int year = DefaultYear)
    {
        await using var db = new DbEntities();
        IQueryable<PpPrijem> query;
        switch (group)
        {
            case PoliticianGroup.Vse:
                query = BasePotvrzenePlaty(db, year)
                    .AsNoTracking();
                break;
            
            case PoliticianGroup.Poslanci:
            case PoliticianGroup.Senatori:
            case PoliticianGroup.KrajstiZastupitele:
                var ica = GetIcaForGroup(group);
                query = BasePotvrzenePlaty(db,year)
                    .AsNoTracking()
                    .Where(p => p.Organizace.FirmaDs != null &&
                                ica.Contains(p.Organizace.FirmaDs.Ico));
                break;
            
            case PoliticianGroup.Vlada:
                var nameIds = await GetNameIdsForGroupAsync(group, year);
                query = BasePotvrzenePlaty(db,year)
                    .AsNoTracking()
                    .Where(p => nameIds.Contains(p.Nameid));
                break;
                
            default:
                query = Enumerable.Empty<PpPrijem>().AsQueryable();
                break;
        }
        
        return await query
            .Include(p => p.Organizace).ThenInclude(o => o.FirmaDs)
            .ToListAsync();;
    }

    public static async Task<List<string>> GetNameIdsForGroupAsync(PoliticianGroup group, int year = DefaultYear)
    {
        await using var db = new DbEntities();
        IQueryable<PpPrijem> query = null;
        List<string> nameIds = null;
        switch (group)
        {
            case PoliticianGroup.Vse:
                query = BasePotvrzenePlaty(db,year)
                    .AsNoTracking();
                break;
            
            case PoliticianGroup.Poslanci:
            case PoliticianGroup.Senatori:
            case PoliticianGroup.KrajstiZastupitele:
                var ica = GetIcaForGroup(group);
                query = BasePotvrzenePlaty(db,year)
                    .AsNoTracking()
                    .Where(p => p.Organizace.FirmaDs != null &&
                                ica.Contains(p.Organizace.FirmaDs.Ico));
                break;
            
            case PoliticianGroup.Vlada:
                var ministri = OsobaRepo.GetByZatrideni(OsobaRepo.Zatrideni.Ministr, year).Select(o => o.NameId);
                var predseda = OsobaRepo.GetByZatrideni(OsobaRepo.Zatrideni.PredsedaVlady, year).Select(o => o.NameId);
                nameIds = ministri.Concat(predseda).Distinct().ToList();
                break;
            
            default:
                query = Enumerable.Empty<PpPrijem>().AsQueryable();
                break;
        }

        if (query is null)
        {
            return nameIds;
        }
        
        return await query.Select(p => p.Nameid).Distinct().ToListAsync();
    }

    public static async Task<string> GetOrganizaceNameAsync(int organizaceId)
    {
        await using var db = new DbEntities();
        var org = await db.PuOrganizace
            .Include(o => o.FirmaDs)
            .FirstOrDefaultAsync(o => o.Id == organizaceId);

        if (org is null)
            return "";

        return org.Nazev;
    }

    public async static Task<PpGlobalStat> GetGlobalStatAsync(int rok, Expression<Func<PpPrijem, bool>> predicate = null)
    {
        var res = new PpGlobalStat() { Rok = rok };
        using var db = new DbEntities();
        var data = BaseAllPlaty(db, rok)
            .AsNoTracking();
        if (predicate != null)
            data = data.Where(predicate);

        //calculate statistics
        res.PocetPrijmu = await data.CountAsync();
        res.PocetPrijmuPozadano = await db.PuEvents
            .AsNoTracking()
            .Where(m => m.ProRok == rok && m.DotazovanaInformace == PuEvent.DruhDotazovaneInformace.Politik)
            .Where(m => m.Typ == PuEvent.TypUdalosti.ZaslaniZadosti )
            .Distinct()
            .CountAsync();


        res.PocetOsobMaPlat = await data
            .Where(m => m.Status == PpPrijem.StatusPlatu.Potvrzen)
            .Select(m => m.Nameid)
            .Distinct()
            .CountAsync();

        res.PocetOsobPozadano = await data
            .Select(m => m.Nameid)
            .Distinct()
            .CountAsync();


        res.PocetOrganizaciDaliPlat = await data.Select(m => m.IdOrganizace).Distinct().CountAsync();
        res.PocetOrganizaciPozadano = await db.PuEvents
            .AsNoTracking()
            .Where(m => m.ProRok == rok && m.DotazovanaInformace == PuEvent.DruhDotazovaneInformace.Politik)
            .Where(m => m.Typ == PuEvent.TypUdalosti.ZaslaniZadosti || m.Typ == PuEvent.TypUdalosti.Neurceno
                || m.Typ == PuEvent.TypUdalosti.Upresneni || m.Typ == PuEvent.TypUdalosti.Stiznost 
                || m.Typ == PuEvent.TypUdalosti.Odvolani || m.Typ == PuEvent.TypUdalosti.Jine || m.Typ == PuEvent.TypUdalosti.NahraniUdaju)
            .Select(m => m.IdOrganizace)
            .Distinct()
            .CountAsync();

        res.PercentilyPlatu = new Dictionary<int, decimal>
        {
            { 10, HlidacStatu.Util.MathTools.PercentileCont(0.10m, data.Select(m => m.PrumernyMesicniPrijemVcetneOdmen)) },
            { 25, HlidacStatu.Util.MathTools.PercentileCont(0.25m, data.Select(m => m.PrumernyMesicniPrijemVcetneOdmen)) },
            { 50, HlidacStatu.Util.MathTools.PercentileCont(0.50m, data.Select(m => m.PrumernyMesicniPrijemVcetneOdmen)) },
            { 75, HlidacStatu.Util.MathTools.PercentileCont(0.75m, data.Select(m => m.PrumernyMesicniPrijemVcetneOdmen)) },
            { 90, HlidacStatu.Util.MathTools.PercentileCont(0.90m, data.Select(m => m.PrumernyMesicniPrijemVcetneOdmen)) }
        };


        return res;
    }
}