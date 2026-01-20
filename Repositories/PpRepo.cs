using Devmasters.Enums;
using HlidacStatu.Entities;
using HlidacStatu.Entities.Entities;
using HlidacStatu.Extensions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using HlidacStatu.Repositories.Cache;
using static HlidacStatu.Entities.PuEvent;

namespace HlidacStatu.Repositories;

public static partial class PpRepo
{
    public const int DefaultYear = 2024;
    public const int MinYear = 2024;

    public static async Task<PuOrganizace> GetOrganizaceFullDetailAsync(string datovaSchranka)
    {
        return await GetOrganizaceFullDetailAsync(new string[] { datovaSchranka });
    }

    public static async Task<List<string>> GetOrganizaceBezPlatuAsync(string nameId = null, string ico = null,
        int rok = PpRepo.DefaultYear)
    {
        await using var db = new DbEntities();
        var allOrgs = BasePotvrzenePlaty(db, rok).Select(m => m.IdOrganizace).Distinct();

        var query = db.PuEvents
            .AsNoTracking()
            .Where(m => m.ProRok == rok
                        && m.DotazovanaInformace == PuEvent.DruhDotazovaneInformace.Politik
                        && !allOrgs.Contains(m.IdOrganizace));
        ;
        if (!string.IsNullOrEmpty(ico))
        {
            query = query.Where(m => m.IcoOrganizace == ico);
        }

        if (!string.IsNullOrEmpty(nameId))
        {
            query = query.Where(m => m.OsobaNameId == nameId);
        }

        var res = await query
            .Select(m => m.IcoOrganizace)
            .Distinct()
            .ToListAsync();

        return res;
    }


    public static string GetEventFormatedDescription(this PuEvent.EventDescription evd, bool html = false)
    {
        throw new NotImplementedException();
        //StringBuilder sb = new StringBuilder();
        //sb.AppendFormat("<span class='text-{0}'>", evd.Negativity.GetBootstrapStatus());
        //if (html)
        //    sb.AppendFormat("<i class='{0}'></i> ", evd.Negativity.GetIcon());
        //sb.AppendFormat("{0} {1:d. M. yyyy}", evd.Title, evd.Date);
        //if (!string.IsNullOrEmpty(evd.Note))
        //    sb.AppendFormat(" <span class='note'>{0}</span>", evd.Note);
        //sb.Append("</span>");
        //return sb.ToString();
    }

    public static PuEvent.EventDescription GetEventDescription(this PuEvent ev)
    {
        EventDescription evd = new EventDescription();
        evd.Date = ev.Datum;
        evd.Smer = ev.Smer;
        evd.Note = ev.Poznamka;
        switch (ev.Typ)
        {
            case PuEvent.TypUdalosti.Neurceno:
                evd.Title = ev.Poznamka;

                if (ev.Poznamka?.EndsWith(".") == false)
                    evd.Title += ".";
                break;
            case PuEvent.TypUdalosti.ZaslaniZadosti:

                evd.Title = $"Zaslali jsme žádost o informace o platech a odměnách.";
                break;
            case PuEvent.TypUdalosti.Upresneni:
                evd.Negativity = PuEvent.EventDescription.NegativityLevel.LowIssue;
                if (ev.Smer == PuEvent.SmerKomunikace.ZpravaProNas)
                    evd.Title = $"jsme dostali žádost o upřesnění žádosti.";
                else
                    evd.Title = $"jsme upřesnili žádost o informace.";
                break;
            case PuEvent.TypUdalosti.UplneOdmitnutiPoskytnutiInformaci:
                evd.Title = $"Obdrželi jsme úplné odmítnutí poskytnutí informací.";
                evd.Negativity = EventDescription.NegativityLevel.HighIssue;
                break;
            case PuEvent.TypUdalosti.CastecneOdmitnutiPoskytnutiInformaci:
                evd.Negativity = EventDescription.NegativityLevel.HighIssue;
                evd.Title = $"Obdrželi jsme částečné odmítnutí poskytnutí informací.";
                break;
            case PuEvent.TypUdalosti.PoskytnutiInformace:
                evd.Negativity = EventDescription.NegativityLevel.Ok;
                evd.Title = $"Obdrželi jsme informace o platech a odměnách.";
                break;
            case PuEvent.TypUdalosti.Zadost_o_UhraduNakladu:
                evd.Negativity = EventDescription.NegativityLevel.LowIssue;
                evd.Title = $"Obdrželi jsme žádost o úhradu nákladů spojených s poskytnutím informací.";
                break;
            case PuEvent.TypUdalosti.Stiznost:
                evd.Negativity = EventDescription.NegativityLevel.HighIssue;
                if (ev.Smer == PuEvent.SmerKomunikace.ZpravaProNas)
                    evd.Title = "Dostali jsme reakci na stížnost pro porušení zákona.";
                else
                    evd.Title = $"Podali jsme stížnost na porušení zákona.";
                break;
            case PuEvent.TypUdalosti.Odvolani:
                if (ev.Smer == PuEvent.SmerKomunikace.ZpravaProNas)
                    evd.Title = "Dostali jsme reakci na odvolání proti postupu organizace.";
                else
                    evd.Title = $"Podali odvolání proti postupu či rozhodnutí úřadu.";
                break;
            case PuEvent.TypUdalosti.Jine:
                evd.Note = "";
                if (ev.Smer == PuEvent.SmerKomunikace.ZpravaProNas)
                    evd.Title = $"Obdrželi jsme zprávu: {ev.Poznamka}";
                else
                    evd.Title = $"Poslali jsme zprávu: {ev.Poznamka}";
                break;
            case PuEvent.TypUdalosti.NahraniUdaju:
                evd.Title = $"Nahráli jsme údaje do databáze.";
                break;
            default:
                break;
        }

        return evd;
    }

    public static string GetEventsTextDescription(this IEnumerable<PuEvent> events,
        string template = "{0}", string itemTemplate = "{0}",
        string itemDelimeter = "<br/>")
    {
        throw new NotImplementedException();
        StringBuilder sb = new StringBuilder();

        foreach (var ev in events.OrderBy(o => o.Datum).Select(m => m.GetEventDescription()))
        {
            //sb.AppendFormat(itemTemplate, ev.GetEventTextDescription());
            //evd.Title = itemDelimeter);
        }

        return string.Format(template, sb.ToString());
    }

    public static async Task<List<PuEvent>> GetEventsForPolitikAndOrganizace(string nameId, int idOrganizace,
        int year = PpRepo.DefaultYear)
    {
        var events = await GetAllEventsAsync(year,
            m => m.DotazovanaInformace == PuEvent.DruhDotazovaneInformace.Politik
                 && m.IdOrganizace == idOrganizace
                 && m.OsobaNameId == nameId);

        return events;
    }

    public static async Task<List<PuEvent>> GetEventsForPolitikAndOrganizace(string nameId, string ico,
        int year = PpRepo.DefaultYear)
    {
        var events = await GetAllEventsAsync(year,
            m => m.DotazovanaInformace == PuEvent.DruhDotazovaneInformace.Politik
                 && m.IcoOrganizace == ico
                 && m.OsobaNameId == nameId);
        return events;
    }

    public static async Task<PuOrganizace> GetOrganizaceFullDetailPerIcoAsync(string ico)
    {
        if (HlidacStatu.Util.DataValidators.CheckCZICO(ico))
        {
            Firma f = await Firmy.GetAsync(ico);
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

    public static async Task<PuOrganizace> GetOrganizaceFullDetailAsync(int idOrganizace)
    {
        await using var db = new DbEntities();

        return await db.PuOrganizace
            .AsNoTracking()
            .Where(pu => pu.Id == idOrganizace)
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

    public static async Task<PuOrganizace> GetOrganizaceOnly(int idOrganizace)
    {
        await using var db = new DbEntities();

        return await db.PuOrganizace
            .AsNoTracking()
            .Where(pu => pu.Id == idOrganizace)
            .Include(o => o.FirmaDs)
            .FirstOrDefaultAsync();
    }

    public static Task<int[]> GetRokyPotvrzenePlatyAsync(DbEntities db)
    {
        return db.PpPrijmy
            .AsNoTracking()
            .Where(m => m.Status >= 0)
            .Select(m => m.Rok)
            .Distinct()
            .OrderBy(m => m)
            .ToArrayAsync();
    }

    public static IQueryable<PpPrijem> BaseAllPlaty(DbEntities db, int rok = DefaultYear)
    {
        return db.PpPrijmy
            .Where(m => m.Rok == rok);
    }


    public static IQueryable<PpPrijem> BasePotvrzenePlaty(DbEntities db, int rok = DefaultYear)
    {
        return BaseAllPlaty(db, rok)
            .Where(m => m.Status >= 0);
    }

    public async static Task<string[]> AllNameIdAsync(bool? zeny, int rok = DefaultYear)
    {
        await using var db = new DbEntities();
        string[] res = null;
        if (zeny.HasValue == false)
            res = await BasePotvrzenePlaty(db, rok)
                .AsNoTracking()
                .Select(p => p.Nameid)
                .Distinct()
                .ToArrayAsync();
        else
            res = await BasePotvrzenePlaty(db, rok)
                .AsNoTracking()
                .Join(db.Osoba,
                    p => p.Nameid,
                    o => o.NameId,
                    (p, o) => o)
                .Where(o => o.Pohlavi == (zeny.Value ? "f" : "m"))
                .Select(o => o.NameId)
                .Distinct()
                .ToArrayAsync();

        return res;
    }

    public static List<PpPrijem> AktualniRok(this ICollection<PpPrijem> prijmy, int rok = DefaultYear)
    {
        return prijmy.Where(m => m.Rok == rok).ToList();
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

        Osoba o = await OsobaCache.GetPersonByNameIdAsync(nameId);
        if (o != null)
        {
            var vazby = await o.PrimaAngazovanostAsync(DS.Graphs.Relation.CharakterVazbyEnum.VlastnictviKontrola,
                HlidacStatu.DS.Graphs.Relation.AktualnostType.Nedavny);
            var d1 = vazby
                .Where(v => Devmasters.DT.DateInterval.IsOverlappingIntervals(
                    new Devmasters.DT.DateInterval(v.RelFrom, v.RelTo), obdobi))
                .Select(m => m.To.Id)
                .Select(m => Firmy.GetAsync(m)).Where(f => f?.Valid == true).ToArray();
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

        return await BasePotvrzenePlaty(db, rok)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.IdOrganizace == idOrganizace
                                      && p.Nameid == nameid);
    }


    //při injectnutém db contextu se nesmí dělat paralelní operace
    public static async Task<List<PpPrijem>> GetPrijmyPolitikaAsync(string nameid, int rok = DefaultYear,
        bool pouzePotvrzene = true, DbEntities db = null)
    {
        bool ownsContext = db == null;
        if (ownsContext)
            db = new DbEntities();
        
        try
        {
            var baseData = pouzePotvrzene ? BasePotvrzenePlaty(db, rok) : BaseAllPlaty(db, rok);
            return await baseData
                .AsNoTracking()
                .Include(p => p.Organizace).ThenInclude(o => o.FirmaDs)
                .Include(p => p.Organizace).ThenInclude(o => o.Tags)
                .Where(p => p.Nameid == nameid)
                .ToListAsync();
        }
        finally
        {
            if (ownsContext && db != null)
            {
                await db.DisposeAsync();
            }
        }
    }

    public static async Task<Osoba> GetCachedOsobaAsync(this PpPrijem prijem)
    {
        if (string.IsNullOrEmpty(prijem.Nameid))
            return null;
        else
            return await OsobaCache.GetPersonByNameIdAsync(prijem.Nameid);
    }


    public static async Task<int> GetPlatyCountAsync(int rok)
    {
        await using var db = new DbEntities();

        return await BasePotvrzenePlaty(db, rok)
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
                Status = PpPrijem.StatusPlatu.Zjistujeme_zadost_106
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
                .Include(t => t.PrijmyPolitiku.Where(p => p.Rok == rok && p.Status >= 0))
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


    public static async Task<Dictionary<string, PpPrijem[]>> GetPrijmyGroupedByNameIdAsync(
        int rok, bool withDetails = false,
        string ico = null, string[] onlyNameIds = null, PoliticianGroup? group = null, bool pouzePotvrzene = true,
        Expression<Func<PpPrijem, bool>> predicate = null)
    {
        await using var db = new DbEntities();
        var q = pouzePotvrzene
            ? BasePotvrzenePlaty(db, rok)
            : BaseAllPlaty(db, rok)
                .AsNoTracking();
        if (withDetails || string.IsNullOrEmpty(ico) == false || group.HasValue)
            q = q.Include(i => i.Organizace).ThenInclude(o => o.FirmaDs);
        if (!string.IsNullOrEmpty(ico))
            q = q.Where(m => m.Organizace.FirmaDs.Ico == ico);
        q = q.Where(p => p.Rok == rok);

        if (onlyNameIds?.Count() > 0)
            q = q.Where(p => onlyNameIds.Contains(p.Nameid));

        if (predicate != null)
            q = q.Where(predicate);

        if (group.HasValue)
        {
            var nameids = await GetNameIdsForGroupAsync(group.Value, rok, pouzePotvrzene);
            q = q.Where(m => nameids.Contains(m.Nameid));
        }

        var qGrouped = (await q.ToArrayAsync())
            .GroupBy(k => k.Nameid, v => v,
                (k, v) => new { nameId = k, platy = v.Where(p => p.Status >= 0).ToArray() });

        return qGrouped.ToDictionary(k => k.nameId, v => v.platy);
    }

    public static async Task<List<PpPrijem>> GetJednotlivePlatyAsync(int rok, bool withDetails = false,
        string ico = null)
    {
        await using var db = new DbEntities();

        var q = BasePotvrzenePlaty(db, rok)
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
        var desc = org.GetSimpleStatus(rok);

        return
            $"<span class='text-{desc.BootstrapStatus}'><i class='{desc.Icon}'></i> {desc.TextStatus}</span>";
    }

    public static PuOrganizaceMetadata.Description GetSimpleStatus(this PuOrganizace org,
        int rok = DefaultYear)
    {
        var res = new PuOrganizaceMetadata.Description();

        if (org.PrijmyPolitiku.All(p => p.Status == PpPrijem.StatusPlatu.Zjistujeme_zadost_106))
        {
            res.TextStatus = "Neposkytli žádný plat";
            res.BootstrapStatus = "danger";
            res.Icon = "fa-solid fa-circle-xmark";
        }
        else if (org.PrijmyPolitiku.All(p => p.Status > PpPrijem.StatusPlatu.Zjistujeme_zadost_106))
        {
            res.TextStatus = $"Platy poskytli v plném rozsahu";
            res.BootstrapStatus = "success";
            res.Icon = "fa-solid fa-badge-check";
        }
        else
        {
            res.TextStatus = "Poslali pouze část platů";
            res.BootstrapStatus = "warning";
            res.Icon = "fa-solid fa-question-circle";
        }

        return res;
    }


    public static async Task<List<PpPrijem>> GetPlatyWithOrganizaceForYearAsync(int rok)
    {
        await using var db = new DbEntities();

        return await BasePotvrzenePlaty(db, rok)
            .AsNoTracking()
            .Include(p => p.Organizace)
            .ThenInclude(o => o.FirmaDs)
            .ToListAsync();
    }

    public static async Task<List<PpPrijem>> GetPoziceDlePlatuAsync(int rangeMin, int rangeMax, int year)
    {
        await using var db = new DbEntities();

        return await BasePotvrzenePlaty(db, year)
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

        var correctDs = await GetCorrectDs(organizace.DS);

        if (correctDs is null)
            throw new Exception("Organizace nebyla nalezena podle datové schránky");

        organizace.DS = correctDs;

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

    public static async Task<string> GetCorrectDs(string originalDs)
    {
        await using var dbContext = new DbEntities();

        var ds = originalDs.Trim().ToLower();

        var firmaDs = await dbContext.FirmaDs.AsNoTracking()
            .Where(o => o.DatovaSchranka == ds)
            .FirstOrDefaultAsync();

        while (firmaDs is not null && firmaDs.DsParent is not null)
        {
            var dsParent = firmaDs.DsParent;
            firmaDs = await dbContext.FirmaDs.AsNoTracking()
                .Where(o => o.DatovaSchranka == dsParent)
                .FirstOrDefaultAsync();
        }

        return firmaDs?.DatovaSchranka;
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
            prijemPolitika.DateCreated = DateTime.Now;
            prijemPolitika.DateModified = DateTime.Now;
            dbContext.PpPrijmy.Add(prijemPolitika);
        }
        else
        {
            prijemPolitika.Id = origPlat.Id;
            prijemPolitika.DateModified = DateTime.Now;
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

    [ShowNiceDisplayName]
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

    public async static Task<Dictionary<string, PpPrijem[]>> GetPrijmyByAgeAsync(int minAge, int maxAge,
        bool withDetails = false, int year = DefaultYear)
    {
        var currY = DateTime.Now.Year;
        var minBirthDate = new DateTime(currY - maxAge, 1, 1);
        var maxBirthDate = new DateTime(currY - minAge, 12, 31);
        return await GetPrijmyByAgeAsync(minBirthDate, maxBirthDate, withDetails, year);
    }

    public async static Task<Dictionary<string, PpPrijem[]>> GetPrijmyByAgeAsync(DateTime minBirthDate,
        DateTime maxBirthDate, bool withDetails = false, int year = DefaultYear)
    {
        await using var db = new DbEntities();

        var filterosoby = db.Osoba.AsQueryable();
        filterosoby = filterosoby
                .Where(o => o.Narozeni != null)
                .Where(o => o.Narozeni >= minBirthDate && o.Narozeni <= maxBirthDate)
            ;


        var nameIds = await BasePotvrzenePlaty(db, DefaultYear)
            .AsNoTracking()
            .Join(filterosoby,
                p => p.Nameid,
                o => o.NameId,
                (p, o) => p.Nameid)
            .ToArrayAsync();

        Dictionary<string, PpPrijem[]> res =
            (await GetPrijmyGroupedByNameIdAsync(year, withDetails, onlyNameIds: nameIds))
            .ToDictionary();

        return res;
    }


    public async static Task<Dictionary<string, PpPrijem[]>> GetPrijmyBySexAsync(bool? woman, bool withDetails = false,
        int year = DefaultYear)
    {
        await using var db = new DbEntities();

        var filterosoby = db.Osoba.AsQueryable();
        if (woman.HasValue)
        {
            var pohlavi = woman.Value ? "f" : "m";
            filterosoby = filterosoby.Where(m => m.Pohlavi == pohlavi);
        }

        var nameIds = await BasePotvrzenePlaty(db, DefaultYear)
            .AsNoTracking()
            .Join(filterosoby,
                p => p.Nameid,
                o => o.NameId,
                (p, o) => p.Nameid)
            .ToArrayAsync();

        Dictionary<string, PpPrijem[]> res =
            (await GetPrijmyGroupedByNameIdAsync(year, withDetails, onlyNameIds: nameIds))
            .ToDictionary();

        return res;
    }

    private static string[] GetIcaForGroup(PoliticianGroup group) =>
        group switch
        {
            PoliticianGroup.Vse => [],
            PoliticianGroup.Poslanci => [UradyConstants.Ica.KancelarPoslaneckeSnemovny],
            PoliticianGroup.Senatori => [UradyConstants.Ica.Senat],
            PoliticianGroup.KrajstiZastupitele => UradyConstants.Ica.Kraje,
            _ => []
        };

    public static async Task<List<PpPrijem>> GetPrijmyForGroupAsync(PoliticianGroup group, int rok = DefaultYear,
        bool pouzePotvrzene = true)
    {
        await using var db = new DbEntities();
        IQueryable<PpPrijem> query;
        switch (group)
        {
            case PoliticianGroup.Vse:
                query = pouzePotvrzene
                    ? BasePotvrzenePlaty(db, rok)
                    : BaseAllPlaty(db, rok)
                        .AsNoTracking();
                break;

            case PoliticianGroup.Poslanci:
            case PoliticianGroup.Senatori:
            case PoliticianGroup.KrajstiZastupitele:
            case PoliticianGroup.Vlada:
                var nameIds = await GetNameIdsForGroupAsync(group, rok, pouzePotvrzene);
                query = pouzePotvrzene
                    ? BasePotvrzenePlaty(db, rok)
                    : BaseAllPlaty(db, rok)
                        .AsNoTracking()
                        .Where(p => nameIds.Contains(p.Nameid));
                break;

            default:
                query = Enumerable.Empty<PpPrijem>().AsQueryable();
                break;
        }

        query = query
            .Include(p => p.Organizace).ThenInclude(o => o.FirmaDs);

        return await query.ToListAsync();
        ;
    }

    public static async Task<List<string>> GetNameIdsForGroupAsync(PoliticianGroup group, int rok = DefaultYear,
        bool pouzePotvrzene = true)
    {
        await using var db = new DbEntities();
        IQueryable<PpPrijem> query = null;
        List<string> nameIds = null;
        switch (group)
        {
            case PoliticianGroup.Vse:
                query = pouzePotvrzene
                    ? BasePotvrzenePlaty(db, rok)
                    : BaseAllPlaty(db, rok)
                        .AsNoTracking();
                break;

            case PoliticianGroup.Poslanci:
            case PoliticianGroup.Senatori:
            case PoliticianGroup.KrajstiZastupitele:
                var ica = GetIcaForGroup(group);
                query = pouzePotvrzene
                    ? BasePotvrzenePlaty(db, rok)
                    : BaseAllPlaty(db, rok)
                        .AsNoTracking()
                        .Include(i => i.Organizace).ThenInclude(o => o.FirmaDs);
                query = query
                    .Where(p => ica.Contains(p.Organizace.FirmaDs.Ico));
                break;

            case PoliticianGroup.Vlada:
                var ministri =
                    (await OsobaRepo.GetByZatrideniAsync(OsobaRepo.Zatrideni.Ministr, rok)).Select(o => o.NameId);
                var predseda =
                    (await OsobaRepo.GetByZatrideniAsync(OsobaRepo.Zatrideni.PredsedaVlady, rok)).Select(o => o.NameId);
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

        var queryRes = query
            .Select(p => p.Nameid)
            .Distinct();

        return await queryRes
            .ToListAsync();
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

    public static async Task<string> GetOrganizaceNameAsync(string ico)
    {
        await using var db = new DbEntities();
        var q = db.PuOrganizace
            .Include(o => o.FirmaDs)
            .Where(o => o.FirmaDs.Ico == ico);
        var org = await q.FirstOrDefaultAsync();

        if (org is null)
            return "";

        return org.Nazev;
    }

    private static ConcurrentDictionary<int, PpGlobalStat> _globalStat = new();

    public static async Task<PpGlobalStat> GetGlobalStatAsync(int rok)
    {
        if (_globalStat.TryGetValue(rok, out var globalStat))
            return globalStat;

        var res = new PpGlobalStat() { Rok = rok };
        await using var db = new DbEntities();
        var dataQueryable = BasePotvrzenePlaty(db, rok)
            .AsNoTracking();

        var data = await dataQueryable.ToListAsync();
        //calculate statistics
        res.PocetPrijmu = data.Count();
        res.PocetPrijmuPozadano = await db.PuEvents
            .AsNoTracking()
            .Where(m => m.ProRok == rok && m.DotazovanaInformace == PuEvent.DruhDotazovaneInformace.Politik)
            .Where(m => m.Typ == PuEvent.TypUdalosti.ZaslaniZadosti)
            .Distinct()
            .CountAsync();

        res.PocetOsobMaPlat = data
            .Where(m => m.Status >= 0)
            .Select(m => m.Nameid)
            .Distinct()
            .Count();

        res.PocetOsobPozadano = data
            .Select(m => m.Nameid)
            .Distinct()
            .Count();


        res.PocetOrganizaciDaliPlat = data.Select(m => m.IdOrganizace).Distinct().Count();
        res.PocetOrganizaciPozadano = await db.PuEvents
            .AsNoTracking()
            .Where(m => m.ProRok == rok && m.DotazovanaInformace == PuEvent.DruhDotazovaneInformace.Politik)
            .Where(m => m.Typ == PuEvent.TypUdalosti.ZaslaniZadosti || m.Typ == PuEvent.TypUdalosti.Neurceno
                                                                    || m.Typ == PuEvent.TypUdalosti.Upresneni ||
                                                                    m.Typ == PuEvent.TypUdalosti.Stiznost
                                                                    || m.Typ == PuEvent.TypUdalosti.Odvolani ||
                                                                    m.Typ == PuEvent.TypUdalosti.Jine ||
                                                                    m.Typ == PuEvent.TypUdalosti.NahraniUdaju)
            .Select(m => m.IdOrganizace)
            .Distinct()
            .CountAsync();

        res.PercentilyPlatu = new Dictionary<int, decimal>
        {
            {
                10,
                HlidacStatu.Util.MathTools.PercentileCont(0.10m, data.Select(m => m.PrumernyMesicniPrijemVcetneOdmen))
            },
            {
                25,
                HlidacStatu.Util.MathTools.PercentileCont(0.25m, data.Select(m => m.PrumernyMesicniPrijemVcetneOdmen))
            },
            {
                50,
                HlidacStatu.Util.MathTools.PercentileCont(0.50m, data.Select(m => m.PrumernyMesicniPrijemVcetneOdmen))
            },
            {
                75,
                HlidacStatu.Util.MathTools.PercentileCont(0.75m, data.Select(m => m.PrumernyMesicniPrijemVcetneOdmen))
            },
            {
                90,
                HlidacStatu.Util.MathTools.PercentileCont(0.90m, data.Select(m => m.PrumernyMesicniPrijemVcetneOdmen))
            }
        };

        _globalStat.TryAdd(rok, res);

        return res;
    }
}