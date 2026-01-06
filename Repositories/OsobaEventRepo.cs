using HlidacStatu.Entities;
using HlidacStatu.Extensions;
using HlidacStatu.Util;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace HlidacStatu.Repositories
{
    public static class OsobaEventRepo
    {
        public static async Task<OsobaEvent> GetByIdAsync(int id)
        {
            await using DbEntities db = new DbEntities();
            
            return await db.OsobaEvent
                .Where(m =>
                    m.Pk == id
                )
                .FirstOrDefaultAsync();
        }

        public static async Task<IEnumerable<OsobaEvent>> GetByEventAsync(Expression<Func<OsobaEvent, bool>> predicate)
        {
            await using DbEntities db = new DbEntities();
            var events = await db.OsobaEvent
                .Where(predicate)
                .ToListAsync();

            return events.ToList();
        }

        public static async Task<List<OsobaEvent>> GetByOsobaIdAsync(int osobaId, Expression<Func<OsobaEvent, bool>> predicate = null)
        {
            await using DbEntities db = new DbEntities();

            return await GetByOsobaIdTracked(db, osobaId, predicate).AsNoTracking().ToListAsync();
        }


        public static IQueryable<OsobaEvent> GetByOsobaIdTracked(DbEntities db, int osobaId, Expression<Func<OsobaEvent, bool>> predicate = null)
        {
            var query = db.OsobaEvent
                .Where(m => m.OsobaId == osobaId);

            if (predicate is not null)
                query = query.Where(predicate);

            return query;
        }

        public static async Task<List<OsobaEvent>> GetByIcoAsync(string ico, Expression<Func<OsobaEvent, bool>> predicate = null)
        {
            if (string.IsNullOrWhiteSpace(ico))
                return Enumerable.Empty<OsobaEvent>().ToList();

            await using DbEntities db = new DbEntities();

            var query = db.OsobaEvent
                .AsNoTracking()
                .Where(m => m.Ico == ico);

            if (predicate is not null)
                query = query.Where(predicate);

            return await query.ToListAsync();
        }
        

        public static async Task<(Osoba Osoba, DateTime? From, DateTime? To, string Role)[]> GetCeosAsync(
            string ico, DateTime? from = null, DateTime? to = null,
            Expression<Func<OsobaEvent, bool>> predicate = null)
        {
            await using DbEntities db = new DbEntities();
            
            var ceoQuery = db.OsobaEvent.AsNoTracking()
                .Where(oe => oe.Ceo == 1 && oe.Ico == ico);

            if (from is not null)
                ceoQuery = ceoQuery.Where(oe => oe.DatumDo == null || oe.DatumDo >= from);

            if (to is not null)
                ceoQuery = ceoQuery.Where(oe => oe.DatumOd == null || oe.DatumOd <= to);

            if (predicate is not null)
                ceoQuery = ceoQuery.Where(predicate);

            var ceoEvents = await ceoQuery
                .OrderByDescending(oe => oe.DatumOd)
                .ToArrayAsync();
                
            var ceoEventsWithOsoba = ceoEvents
                .Select(m => (OsobaRepo.GetByInternalId(m.OsobaId), m.DatumOd, m.DatumDo, m.AddInfo))
                .ToArray();

            if (ceoEventsWithOsoba is null)
                return Array.Empty<(Osoba Osoba, DateTime? From, DateTime? To, string Role)>();

            return ceoEventsWithOsoba;
        }
        

        public static async Task<IEnumerable<string>> GetDistinctOrganisationsAsync(OsobaEvent.Types? eventType)
        {
            if (eventType is null)
                return Enumerable.Empty<string>();

            await using DbEntities db = new DbEntities();

            return await db.OsobaEvent.AsNoTracking()
                .Where(m => m.Type == (int)eventType)
                .Where(m => m.Organizace != null)
                .Select(m => m.Organizace)
                .Distinct()
                .Take(100)
                .ToListAsync();

        }

        public static async Task<IEnumerable<string>> GetDistinctAddInfoAsync(OsobaEvent.Types? eventType, string organizace)
        {
            if (eventType is null || string.IsNullOrWhiteSpace(organizace))
                return Enumerable.Empty<string>();

            await using DbEntities db = new DbEntities();

            return await db.OsobaEvent.AsNoTracking()
                .Where(m => m.Type == (int)eventType)
                .Where(m => m.Organizace == organizace)
                .Select(m => m.AddInfo)
                .Distinct()
                .Take(100)
                .ToListAsync();

        }
         

        public static async Task<OsobaEvent> CreateOrUpdateAsync(OsobaEvent osobaEvent, string user,
            [CallerMemberName] string callingByMethod = null,
            [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0)
        {
            NormalizeOsobaEvent(osobaEvent);

            await using DbEntities db = new DbEntities();
            
            OsobaEvent eventToUpdate = null;
            // známe PK
            if (osobaEvent.Pk > 0)
            {
                eventToUpdate = await db.OsobaEvent
                    .Where(ev =>
                        ev.Pk == osobaEvent.Pk
                    )
                    .FirstOrDefaultAsync();

                user = user + "<null>";
                user = user + $"|{callingByMethod}|{sourceLineNumber}";

                if (eventToUpdate != null)
                    return await UpdateEventAsync(eventToUpdate, osobaEvent, user, db);
            }

            eventToUpdate = await GetDuplicateAsync(osobaEvent, db);

            if (eventToUpdate != null)
            {
                return await UpdateEventAsync(eventToUpdate, osobaEvent, user, db);
            }

            return await CreateEventAsync(osobaEvent, user, db);
        }

        public static async Task<OsobaEvent> AddCleverCEOIntoOsobaEventAsync(
            this Osoba osoba, Firma CeoOf, DateTime? dateFrom, DateTime? dateTo,
            string zdroj,
            bool overrideExisting,
            string changingUser)
        {
            string pozice = osoba.Pohlavi == "f" ? "Ředitelka" : "Ředitel";
            OsobaEvent.Types type = OsobaEvent.Types.VerejnaSpravaExekutivni;
            var firmaCat = await CeoOf.KategorieOVMAsync();

            if (CeoOf.ICO == "48136450") //CNB
            {
                pozice = osoba.Pohlavi == "f" ? "Guvernérka" : "Guvernér";
                type = OsobaEvent.Types.VerejnaSpravaExekutivni;
            }
            else if (CeoOf.ICO == "00064581") //Praha
            {
                pozice = osoba.Pohlavi == "f" ? "Primátorka" : "Primátor";
                type = OsobaEvent.Types.PolitickaExekutivni;
            }
            else if (CeoOf.ICO == "49467352") //NSZ
            {
                pozice = osoba.Pohlavi == "f" ? "Nejvyšší státní zástupkyně" : "Nejvyšší státní zástupce";
                type = OsobaEvent.Types.VerejnaSpravaExekutivni;
            }
            else if (CeoOf.ICO == "48510190") //NS
            {
                pozice = osoba.Pohlavi == "f" ? "Předsedkyně nejvyššího soudu" : "Předseda nejvyššího soudu";
                type = OsobaEvent.Types.VerejnaSpravaExekutivni;
            }
            else if (CeoOf.ICO == "75003716") //NSZ
            {
                pozice = osoba.Pohlavi == "f" ? "Předsedkyně nejvyššího správního soudu" : "Předseda nejvyššího správního soudu";
                type = OsobaEvent.Types.VerejnaSpravaExekutivni;
            }
            else if (firmaCat.Any(m => m.id == (int)Firma.Zatrideni.SubjektyObory.Verejne_vysoke_skoly))
            {
                pozice = osoba.Pohlavi == "f" ? "Rektorka" : "Rektor";
                type = OsobaEvent.Types.VerejnaSpravaExekutivni;
            }
            else if (firmaCat.Any(m => m.id == (int)Firma.Zatrideni.SubjektyObory.Kraje_Praha))
            {
                pozice = osoba.Pohlavi == "f" ? "Hejtmanka" : "Hejtman";
                type = OsobaEvent.Types.PolitickaExekutivni;
            }
            else if (firmaCat.Any(m => m.id == (int)Firma.Zatrideni.SubjektyObory.Ministerstva))
            {
                pozice = osoba.Pohlavi == "f" ? "Ministryně" : "Ministr";
                type = OsobaEvent.Types.PolitickaExekutivni;
            }
            else if (firmaCat.Any(m => m.id == (int)Firma.Zatrideni.SubjektyObory.Statutarni_mesta))
            {
                pozice = osoba.Pohlavi == "f" ? "Primátorka" : "Primátor";
                type = OsobaEvent.Types.PolitickaExekutivni;
            }
            else if (firmaCat.Any(m => m.id == (int)Firma.Zatrideni.SubjektyObory.Obce))
            {
                pozice = osoba.Pohlavi == "f" ? "Starostka" : "Starosta";
                type = OsobaEvent.Types.PolitickaExekutivni;
            }
            else if (firmaCat.Any(m => m.id == (int)Firma.Zatrideni.SubjektyObory.Zakladni_a_stredni_skoly))
            {
                pozice = osoba.Pohlavi == "f" ? "Ředitelka" : "Ředitel";
                type = OsobaEvent.Types.VerejnaSpravaExekutivni;
            }
            else if (firmaCat.Any(m => m.id == (int)Firma.Zatrideni.SubjektyObory.Vrchni_statni_zastupitelstvi))
            {
                pozice = osoba.Pohlavi == "f" ? "Vrchní státní zástupkyně" : "Vrchní státní zástupce";
                type = OsobaEvent.Types.VerejnaSpravaExekutivni;
            }
            else if (firmaCat.Any(m => m.id == (int)Firma.Zatrideni.SubjektyObory.Krajska_statni_zastupitelstvi))
            {
                pozice = osoba.Pohlavi == "f" ? "Krajská státní zástupkyně" : "Krajský státní zástupce";
                type = OsobaEvent.Types.VerejnaSpravaExekutivni;
            }
            else if (firmaCat.Any(m => m.id == (int)Firma.Zatrideni.SubjektyObory.Krajske_soudy))
            {
                pozice = osoba.Pohlavi == "f" ? "Předsedkyně krajského soudu" : "Předseda krajského soudu";
                type = OsobaEvent.Types.VerejnaSpravaExekutivni;
            }
            else if (firmaCat.Any(m => m.id == (int)Firma.Zatrideni.SubjektyObory.Soudy))
            {
                pozice = osoba.Pohlavi == "f" ? "Předsedkyně soudu" : "Předseda soudu";
                type = OsobaEvent.Types.VerejnaSpravaExekutivni;
            }

            //find duplicate
            await using (var db = new DbEntities())
            {
                var exists = await db.OsobaEvent.AsNoTracking()
                    .Where(ev =>
                        ev.OsobaId == osoba.InternalId
                        && ev.Ico == CeoOf.ICO
                        && ev.Ceo == 1
                        && ev.DatumOd == dateFrom
                        && ev.DatumDo == dateTo)
                    .FirstOrDefaultAsync();
                
                if (exists != null)
                {
                    if (overrideExisting)
                        db.OsobaEvent.Remove(exists);
                    else
                        return exists;
                }
            }

            OsobaEvent newOE = new OsobaEvent(osoba.InternalId, null, "", type);
            newOE.AddInfo = pozice;
            newOE.Ceo = 1;
            newOE.DatumOd = dateFrom;
            newOE.DatumDo = dateTo;
            newOE.Ico = CeoOf.ICO;
            newOE.Organizace = CeoOf.Jmeno;
            newOE.Zdroj = zdroj;
            return await AddOrUpdateEventAsync(osoba, newOE, changingUser);

        }

        public static async Task<OsobaEvent> AddOrUpdateEventAsync(this Osoba osoba, OsobaEvent ev, string user)
        {
            if (ev == null || osoba == null)
                return null;

            ev.OsobaId = osoba.InternalId;

            return await CreateOrUpdateAsync(ev, user);
        }

        private static async Task<OsobaEvent> GetDuplicateAsync(OsobaEvent osobaEvent, DbEntities db)
        {
            return await db.OsobaEvent
                .Where(ev =>
                    ev.OsobaId == osobaEvent.OsobaId
                    && ev.Ico == osobaEvent.Ico
                    && ev.AddInfo == osobaEvent.AddInfo
                    && ev.AddInfoNum == osobaEvent.AddInfoNum
                    && ev.DatumOd == osobaEvent.DatumOd
                    && ev.Type == osobaEvent.Type
                    && ev.Organizace == osobaEvent.Organizace)
                .FirstOrDefaultAsync();
        }

        private static async Task<OsobaEvent> CreateEventAsync(OsobaEvent osobaEvent, string user, DbEntities db)
        {
            if (osobaEvent.OsobaId == 0 && string.IsNullOrWhiteSpace(osobaEvent.Ico))
                throw new Exception("Cant attach event to a person or to a company since their reference is empty");

            osobaEvent.Organizace = ParseTools.NormalizaceStranaShortName(osobaEvent.Organizace);
            osobaEvent.Created = DateTime.Now;
            osobaEvent.ModifiedBy = user;
            osobaEvent.Modified = DateTime.Now;
            db.OsobaEvent.Add(osobaEvent);
            await db.SaveChangesAsync();
            if (osobaEvent.OsobaId > 0)
            {
                OsobaRepo.FlushCache(osobaEvent.OsobaId);
            }

            AuditRepo.Add(Audit.Operations.Update, user, osobaEvent, null);
            return osobaEvent;
        }

        private static async Task<OsobaEvent> UpdateEventAsync(OsobaEvent eventToUpdate, OsobaEvent osobaEvent, string user,
            DbEntities db)
        {
            if (eventToUpdate is null)
                throw new ArgumentNullException(nameof(eventToUpdate), "Argument can't be null");
            if (osobaEvent is null)
                throw new ArgumentNullException(nameof(osobaEvent), "Argument can't be null");
            if (db is null)
                throw new ArgumentNullException(nameof(db), "Argument can't be null");

            var eventOriginal = eventToUpdate.ShallowCopy();
            NormalizeOsobaEvent(osobaEvent);

            if (!string.IsNullOrWhiteSpace(osobaEvent.Ico))
                eventToUpdate.Ico = osobaEvent.Ico;
            if (osobaEvent.OsobaId > 0)
                eventToUpdate.OsobaId = osobaEvent.OsobaId;

            eventToUpdate.DatumOd = osobaEvent.DatumOd;
            eventToUpdate.DatumDo = osobaEvent.DatumDo;
            eventToUpdate.Organizace = ParseTools.NormalizaceStranaShortName(osobaEvent.Organizace);
            eventToUpdate.AddInfoNum = osobaEvent.AddInfoNum;
            eventToUpdate.AddInfo = osobaEvent.AddInfo;
            eventToUpdate.Title = osobaEvent.Title;
            eventToUpdate.Type = osobaEvent.Type;
            eventToUpdate.Zdroj = osobaEvent.Zdroj;
            eventToUpdate.Status = osobaEvent.Status;
            eventToUpdate.Ceo = osobaEvent.Ceo;
            eventToUpdate.Created = DateTime.Now;
            eventToUpdate.Note = osobaEvent.Note;
            eventToUpdate.ModifiedBy = user;
            eventToUpdate.Modified = DateTime.Now;

            await db.SaveChangesAsync();
            if (osobaEvent.OsobaId > 0)
            {
                OsobaRepo.FlushCache(osobaEvent.OsobaId);
            }

            AuditRepo.Add(Audit.Operations.Update, user, eventToUpdate, eventOriginal);
            return eventToUpdate;
        }

        public static async Task DeleteAsync(OsobaEvent osobaEvent, string user)
        {
            if (osobaEvent.Pk > 0)
            {
                await using DbEntities db = new DbEntities();
                db.OsobaEvent.Attach(osobaEvent);
                db.Entry(osobaEvent).State = EntityState.Deleted;
                AuditRepo.Add<OsobaEvent>(Audit.Operations.Delete, user, osobaEvent, null);
                Osoby.CachedEvents.Delete(osobaEvent.OsobaId);
                await db.SaveChangesAsync();
            }
        }

        private static void NormalizeOsobaEvent(OsobaEvent osobaEvent)
        {
            osobaEvent.AddInfo = osobaEvent.AddInfo?.Trim();
            osobaEvent.Organizace = osobaEvent.Organizace?.Trim();
            osobaEvent.Ico = osobaEvent.Ico?.Trim();
            osobaEvent.Note = osobaEvent.Note?.Trim();
            osobaEvent.Title = osobaEvent.Title?.Trim();
        }
    }
}