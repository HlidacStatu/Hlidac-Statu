using HlidacStatu.Entities;
using HlidacStatu.Extensions;
using HlidacStatu.Util;

using Microsoft.EntityFrameworkCore;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace HlidacStatu.Repositories
{
    public static class OsobaEventRepo
    {
        public static OsobaEvent GetById(int id)
        {
            using (DbEntities db = new DbEntities())
            {
                return db.OsobaEvent.AsQueryable()
                    .Where(m =>
                        m.Pk == id
                    )
                    .FirstOrDefault();
            }
        }

        public static IEnumerable<OsobaEvent> GetByEvent(Expression<Func<OsobaEvent, bool>> predicate)
        {
            using (DbEntities db = new DbEntities())
            {
                var events = db.OsobaEvent
                    .Where(predicate);

                return events.ToList();
            }
        }

        public static List<OsobaEvent> GetByOsobaId(int osobaId)
        {
            using (DbEntities db = new DbEntities())
            {
                return db.OsobaEvent
                    .AsNoTracking()
                    .Where(m => m.OsobaId == osobaId)
                    .ToList();
            }
        }

        // tohle by ještě sneslo optimalizaci - ale až budou k dispozici data
        public static IEnumerable<string> GetAddInfos(string jmeno, int? eventTypeId, int maxNumOfResults = 1500)
        {
            using (DbEntities db = new DbEntities())
            {
                var result = db.OsobaEvent.AsQueryable()
                    .Where(m => m.Type == eventTypeId)
                    .Where(m => m != null)
                    .Where(m => m.AddInfo.Contains(jmeno))
                    //.OrderBy(m => m.AddInfo)
                    .Select(m => m.AddInfo)
                    .Distinct()
                    .Take(maxNumOfResults)
                    .ToList();

                return result;
            }
        }

        public static IEnumerable<string> GetOrganisations(string jmeno, int? eventTypeId, int maxNumOfResults = 1500)
        {
            using (DbEntities db = new DbEntities())
            {
                var result = db.OsobaEvent.AsQueryable()
                    .Where(m => m.Type == eventTypeId)
                    .Where(m => m != null)
                    .Where(m => m.Organizace.Contains(jmeno))
                    //.OrderBy(m => m.AddInfo)
                    .Select(m => m.Organizace)
                    .Distinct()
                    .Take(maxNumOfResults)
                    .ToList();

                return result;
            }
        }

        public static OsobaEvent CreateOrUpdate(OsobaEvent osobaEvent, string user)
        {
            NormalizeOsobaEvent(osobaEvent);

            using (DbEntities db = new DbEntities())
            {
                OsobaEvent eventToUpdate = null;
                // známe PK
                if (osobaEvent.Pk > 0)
                {
                    eventToUpdate = db.OsobaEvent.AsQueryable()
                        .Where(ev =>
                            ev.Pk == osobaEvent.Pk
                        )
                        .FirstOrDefault();

                    if (eventToUpdate != null)
                        return UpdateEvent(eventToUpdate, osobaEvent, user, db);
                }

                eventToUpdate = GetDuplicate(osobaEvent, db);

                if (eventToUpdate != null)
                {
                    return UpdateEvent(eventToUpdate, osobaEvent, user, db);
                }

                return CreateEvent(osobaEvent, user, db);
            }
        }

        public static OsobaEvent AddCleverCEOIntoOsobaEvent(
            this Osoba osoba, Firma CeoOf, DateTime? dateFrom, DateTime? dateTo,
            string zdroj,
            bool overrideExisting,
            string changingUser)
        {
            string pozice = osoba.Pohlavi == "f" ? "Ředitelka" : "Ředitel";
            OsobaEvent.Types type = OsobaEvent.Types.VerejnaSpravaPracovni;
            if (CeoOf.ICO == "48136450") //CNB
            {
                pozice = osoba.Pohlavi == "f" ? "Guvernérka" : "Guvernér";
                type = OsobaEvent.Types.VerejnaSpravaPracovni;
            }
            else if (CeoOf.ICO == "00064581") //Praha
            {
                pozice = osoba.Pohlavi == "f" ? "Primátorka" : "Primátor";
                type = OsobaEvent.Types.PolitickaPracovni;
            }
            else if (CeoOf.ICO == "49467352") //NSZ
            {
                pozice = osoba.Pohlavi == "f" ? "Nejvyšší státní zástupkyně" : "Nejvyšší státní zástupce";
                type = OsobaEvent.Types.VerejnaSpravaPracovni;
            }
            else if (CeoOf.ICO == "48510190") //NS
            {
                pozice = osoba.Pohlavi == "f" ? "Předsedkyně nejvyššího soudu" : "Předseda nejvyššího soudu";
                type = OsobaEvent.Types.VerejnaSpravaPracovni;
            }
            else if (CeoOf.ICO == "75003716") //NSZ
            {
                pozice = osoba.Pohlavi == "f" ? "Předsedkyně nejvyššího správního soudu" : "Předseda nejvyššího správního soudu";
                type = OsobaEvent.Types.VerejnaSpravaPracovni;
            }


            else if (CeoOf.KategorieOVM().Any(m => m.id == (int)Firma.Zatrideni.SubjektyObory.Verejne_vysoke_skoly))
            {
                pozice = osoba.Pohlavi == "f" ? "Rektorka" : "Rektor";
                type = OsobaEvent.Types.VerejnaSpravaPracovni;
            }
            else if (CeoOf.KategorieOVM().Any(m => m.id == (int)Firma.Zatrideni.SubjektyObory.Kraje_Praha))
            {
                pozice = osoba.Pohlavi == "f" ? "Hejtmanka" : "Hejtman";
                type = OsobaEvent.Types.PolitickaPracovni;
            }
            else if (CeoOf.KategorieOVM().Any(m => m.id == (int)Firma.Zatrideni.SubjektyObory.Ministerstva))
            {
                pozice = osoba.Pohlavi == "f" ? "Ministryně" : "Ministr";
                type = OsobaEvent.Types.PolitickaPracovni;
            }
            else if (CeoOf.KategorieOVM().Any(m => m.id == (int)Firma.Zatrideni.SubjektyObory.Statutarni_mesta))
            {
                pozice = osoba.Pohlavi == "f" ? "Primátorka" : "Primátor";
                type = OsobaEvent.Types.PolitickaPracovni;
            }
            else if (CeoOf.KategorieOVM().Any(m => m.id == (int)Firma.Zatrideni.SubjektyObory.Obce))
            {
                pozice = osoba.Pohlavi == "f" ? "Starostka" : "Starosta";
                type = OsobaEvent.Types.PolitickaPracovni;
            }
            else if (CeoOf.KategorieOVM().Any(m => m.id == (int)Firma.Zatrideni.SubjektyObory.Zakladni_a_stredni_skoly))
            {
                pozice = osoba.Pohlavi == "f" ? "Ředitelka" : "Ředitel";
                type = OsobaEvent.Types.VerejnaSpravaPracovni;
            }
            else if (CeoOf.KategorieOVM().Any(m => m.id == (int)Firma.Zatrideni.SubjektyObory.Vrchni_statni_zastupitelstvi))
            {
                pozice = osoba.Pohlavi == "f" ? "Vrchní státní zástupkyně" : "Vrchní státní zástupce";
                type = OsobaEvent.Types.VerejnaSpravaPracovni;
            }
            else if (CeoOf.KategorieOVM().Any(m => m.id == (int)Firma.Zatrideni.SubjektyObory.Krajska_statni_zastupitelstvi))
            {
                pozice = osoba.Pohlavi == "f" ? "Krajská státní zástupkyně" : "Krajský státní zástupce";
                type = OsobaEvent.Types.VerejnaSpravaPracovni;
            }
            else if (CeoOf.KategorieOVM().Any(m => m.id == (int)Firma.Zatrideni.SubjektyObory.Krajske_soudy))
            {
                pozice = osoba.Pohlavi == "f" ? "Předsedkyně krajského soudu" : "Předseda krajského soudu";
                type = OsobaEvent.Types.VerejnaSpravaPracovni;
            }
            else if (CeoOf.KategorieOVM().Any(m => m.id == (int)Firma.Zatrideni.SubjektyObory.Soudy))
            {
                pozice = osoba.Pohlavi == "f" ? "Předsedkyně soudu" : "Předseda soudu";
                type = OsobaEvent.Types.VerejnaSpravaPracovni;
            }

            //find duplicate
            using (var db = new DbEntities())
            {
                var exists = db.OsobaEvent.AsNoTracking().AsQueryable()
                    .Where(ev =>
                        ev.OsobaId == osoba.InternalId
                        && ev.Ico == CeoOf.ICO
                        && ev.Ceo == 1)
                    .FirstOrDefault();
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
            return AddOrUpdateEvent(osoba, newOE, changingUser);



        }

        //migrace: tohle půjde přesunout do extension osoby
        public static OsobaEvent AddOrUpdateEvent(this Osoba osoba, OsobaEvent ev, string user)
        {
            if (ev == null || osoba == null)
                return null;

            ev.OsobaId = osoba.InternalId;

            return CreateOrUpdate(ev, user);
        }

        private static OsobaEvent GetDuplicate(OsobaEvent osobaEvent, DbEntities db)
        {
            return db.OsobaEvent.AsQueryable()
                .Where(ev =>
                    ev.OsobaId == osobaEvent.OsobaId
                    && ev.Ico == osobaEvent.Ico
                    && ev.AddInfo == osobaEvent.AddInfo
                    && ev.AddInfoNum == osobaEvent.AddInfoNum
                    && ev.DatumOd == osobaEvent.DatumOd
                    && ev.Type == osobaEvent.Type
                    && ev.Organizace == osobaEvent.Organizace)
                .FirstOrDefault();
        }

        private static OsobaEvent CreateEvent(OsobaEvent osobaEvent, string user, DbEntities db)
        {
            if (osobaEvent.OsobaId == 0 && string.IsNullOrWhiteSpace(osobaEvent.Ico))
                throw new Exception("Cant attach event to a person or to a company since their reference is empty");

            osobaEvent.Organizace = ParseTools.NormalizaceStranaShortName(osobaEvent.Organizace);
            osobaEvent.Created = DateTime.Now;
            db.OsobaEvent.Add(osobaEvent);
            db.SaveChanges();
            if (osobaEvent.OsobaId > 0)
            {
                OsobaRepo.FlushCache(osobaEvent.OsobaId);
            }

            AuditRepo.Add(Audit.Operations.Update, user, osobaEvent, null);
            return osobaEvent;
        }

        private static OsobaEvent UpdateEvent(OsobaEvent eventToUpdate, OsobaEvent osobaEvent, string user,
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

            db.SaveChanges();
            if (osobaEvent.OsobaId > 0)
            {
                OsobaRepo.FlushCache(osobaEvent.OsobaId);
            }

            AuditRepo.Add(Audit.Operations.Update, user, eventToUpdate, eventOriginal);
            return eventToUpdate;
        }

        public static void Delete(OsobaEvent osobaEvent, string user)
        {
            if (osobaEvent.Pk > 0)
            {
                using (DbEntities db = new DbEntities())
                {
                    db.OsobaEvent.Attach(osobaEvent);
                    db.Entry(osobaEvent).State = EntityState.Deleted;
                    AuditRepo.Add<OsobaEvent>(Audit.Operations.Delete, user, osobaEvent, null);
                    Osoby.CachedEvents.Delete(osobaEvent.OsobaId);
                    db.SaveChanges();
                }
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