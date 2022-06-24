using Devmasters;

using HlidacStatu.Connectors;
using HlidacStatu.Entities;
using HlidacStatu.Extensions;

using Microsoft.EntityFrameworkCore;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Devmasters.Enums;

namespace HlidacStatu.Repositories
{
    public static partial class OsobaRepo
    {
        public static Osoba GetOrCreateNew(string titulPred, string jmeno, string prijmeni, string titulPo,
            string narozeni, Osoba.StatusOsobyEnum status, string user
        )
        {
            return GetOrCreateNew(titulPred, jmeno, prijmeni, titulPo, Devmasters.DT.Util.ToDate(narozeni), status,
                user);
        }

        public static Osoba GetOrCreateNew(string titulPred, string jmeno, string prijmeni, string titulPo,
            DateTime? narozeni, Osoba.StatusOsobyEnum status, string user, DateTime? umrti = null)
        {
            var p = new Osoba();
            p.TitulPred = Osoba.NormalizeTitul(titulPred, true);
            p.TitulPo = Osoba.NormalizeTitul(titulPo, false);
            p.Jmeno = Osoba.NormalizeJmeno(jmeno);
            p.Prijmeni = Osoba.NormalizePrijmeni(prijmeni);

            if (narozeni.HasValue == false)
            {
                p.Umrti = umrti;
                p.Status = (int)status;
                Save(p);
                AuditRepo.Add(Audit.Operations.Create, user, p, null);
                return p;
            }

            var exiO = Searching.GetByName(p.Jmeno, p.Prijmeni, narozeni.Value);


            if (exiO == null)
            {
                p.Umrti = umrti;
                p.Status = (int)status;
                p.Narozeni = narozeni;
                Save(p);
                AuditRepo.Add(Audit.Operations.Create, user, p, null);
                return p;
            }
            else
            {
                return exiO;
            }
        }

        public static string GetUniqueNamedId(Osoba osoba)
        {
            if (string.IsNullOrWhiteSpace(osoba.JmenoAscii) || string.IsNullOrWhiteSpace(osoba.PrijmeniAscii))
                return "";
            if (!char.IsLetter(osoba.JmenoAscii[0]) || !char.IsLetter(osoba.PrijmeniAscii[0]))
                return "";

            string basic = TextUtil.ShortenText(osoba.JmenoAscii, 23) + "-" +
                           TextUtil.ShortenText(osoba.PrijmeniAscii, 23).Trim();
            basic = basic.ToLowerInvariant().NormalizeToPureTextLower();
            basic = TextUtil.ReplaceDuplicates(basic, ' ').Trim();
            basic = basic.Replace(" ", "-");

            if (osoba.Status == (int)Osoba.StatusOsobyEnum.Duplicita)
                basic += "-duplicita";

            Osoba exists = null;
            int num = 0;
            string checkUniqueName = basic;
            using (DbEntities db = new DbEntities())
            {
                do
                {
                    exists = db.Osoba.AsQueryable()
                        .Where(m => m.NameId.StartsWith(checkUniqueName))
                        .OrderByDescending(m => m.NameId)
                        .FirstOrDefault();
                    if (exists != null)
                    {
                        Regex r = new Regex(
                            @"(?<num>\d{1,})$", RegexOptions.IgnorePatternWhitespace);
                        var m = r.Match(exists.NameId);
                        if (m.Success)
                        {
                            num = Convert.ToInt32(m.Groups["num"].Value);
                        }

                        num++;
                        checkUniqueName = basic + "-" + num.ToString();
                    }
                    else
                        return checkUniqueName;
                } while (exists != null);
            }

            return basic;
        }

        public static Osoba Save(Osoba osoba, params OsobaExternalId[] externalIds)
        {
            using (DbEntities db = new DbEntities())
            {
                osoba.JmenoAscii = TextUtil.RemoveDiacritics(osoba.Jmeno);
                osoba.PrijmeniAscii = TextUtil.RemoveDiacritics(osoba.Prijmeni);
                osoba.PuvodniPrijmeniAscii = TextUtil.RemoveDiacritics(osoba.PuvodniPrijmeni);

                if (string.IsNullOrEmpty(osoba.NameId))
                {
                    osoba.NameId = GetUniqueNamedId(osoba);
                }

                osoba.LastUpdate = DateTime.Now;

                if (osoba.Pohlavi != "f" || osoba.Pohlavi != "m")
                {
                    var sex = osoba.PohlaviCalculated();
                }

                db.Osoba.Attach(osoba);


                if (osoba.InternalId == 0)
                {
                    db.Entry(osoba).State = EntityState.Added;
                }
                else
                    db.Entry(osoba).State = EntityState.Modified;

                try
                {
                    db.SaveChanges();
                }
                catch (Exception e)
                {
                    HlidacStatu.Util.Consts.Logger.Error($"Saving osoba {osoba.NameId}", e);
                }

                if (externalIds != null)
                {
                    foreach (var ex in externalIds)
                    {
                        ex.OsobaId = osoba.InternalId;
                        OsobaExternalRepo.Add(ex);
                    }
                }
            }

            return osoba;
        }

        public static Osoba GetByNameId(string nameId, bool canReturnDuplicate = false)
        {
            using (DbEntities db = new DbEntities())
            {
                var osoba = db.Osoba.AsQueryable()
                    .Where(m =>
                        m.NameId == nameId
                    )
                    .FirstOrDefault();

                if (canReturnDuplicate)
                    return osoba;

                return osoba?.GetOriginal();
            }
        }

        public static Osoba GetByInternalId(int id, bool canReturnDuplicate = false)
        {
            using (DbEntities db = new DbEntities())
            {
                var osoba = db.Osoba.AsQueryable()
                    .Where(m =>
                        m.InternalId == id
                    )
                    .FirstOrDefault();

                if (canReturnDuplicate)
                    return osoba;

                return osoba?.GetOriginal();
            }
        }

        private static Osoba GetOriginal(this Osoba osoba)
        {
            if (osoba.Status != (int)Osoba.StatusOsobyEnum.Duplicita)
                return osoba;

            using (DbEntities db = new DbEntities())
            {
                var originalOsoba = db.Osoba.AsQueryable()
                    .Where(o => o.InternalId == osoba.OriginalId)
                    .FirstOrDefault();

                return originalOsoba?.GetOriginal();
            }
        }

        public static Osoba GetByExternalID(string exId, OsobaExternalId.Source source)
        {
            using (DbEntities db = new DbEntities())
            {
                var oei = db.OsobaExternalId.AsQueryable()
                    .Where(m => m.ExternalId == exId && m.ExternalSource == (int)source).FirstOrDefault();
                if (oei == null)
                    return null;
                else
                    return GetByInternalId(oei.OsobaId);
            }
        }

        public static List<Osoba> GetByEvent(Expression<Func<OsobaEvent, bool>> predicate)
        {
            using (DbEntities db = new DbEntities())
            {
                var events = db.OsobaEvent
                    .Where(predicate);

                var people = db.Osoba.AsQueryable().Where(o => events.Any(e => e.OsobaId == o.InternalId));

                return people.Distinct().ToList();
            }
        }

        public static void SetManualTimeStamp(int osobaId, string author)
        {
            using (DbEntities db = new DbEntities())
            {
                var osobaToUpdate = db.Osoba.AsQueryable()
                    .Where(m =>
                        m.InternalId == osobaId
                    ).FirstOrDefault();

                if (osobaToUpdate != null)
                {
                    osobaToUpdate.ManuallyUpdated = DateTime.Now;
                    osobaToUpdate.ManuallyUpdatedBy = author;
                    db.SaveChanges();
                }
            }
        }

        public static Osoba Update(Osoba osoba, string user)
        {
            using (DbEntities db = new DbEntities())
            {
                var osobaDb = db.Osoba.AsQueryable()
                    .Where(m =>
                        m.InternalId == osoba.InternalId
                    ).FirstOrDefault();

                var osobaToUpdate = osobaDb?.GetOriginal();

                var osobaOriginal = osobaToUpdate?.ShallowCopy();

                if (osobaToUpdate != null)
                {
                    osobaToUpdate.Jmeno = osoba.Jmeno;
                    osobaToUpdate.Prijmeni = osoba.Prijmeni;
                    osobaToUpdate.TitulPo = osoba.TitulPo;
                    osobaToUpdate.TitulPred = osoba.TitulPred;
                    osobaToUpdate.Narozeni = osoba.Narozeni;
                    osobaToUpdate.Status = osoba.Status;
                    osobaToUpdate.Umrti = osoba.Umrti;

                    string normalizedWikiId = osoba.WikiId?.Trim();
                    if (!string.IsNullOrWhiteSpace(normalizedWikiId))
                        osobaToUpdate.WikiId = normalizedWikiId;

                    Save(osobaToUpdate);
                    
                    AuditRepo.Add(Audit.Operations.Update, user, osobaToUpdate, osobaOriginal);

                    return osobaToUpdate;
                }
            }

            return osoba;
        }


        public static Osoba MergeWith(this Osoba original, Osoba duplicated, string user)
        {
            if (original.InternalId == duplicated.InternalId)
                return original;

            //todo: předělat do transakce tak, aby se neukládaly samostatné části ?!
            SponzoringRepo.MergeDonatingOsoba(original.InternalId, duplicated.InternalId, user);

            List<OsobaEvent> duplicateEvents = OsobaEventRepo.GetByOsobaId(duplicated.InternalId);
            List<OsobaEvent> originalEvents = OsobaEventRepo.GetByOsobaId(original.InternalId);
            foreach (var dEv in duplicateEvents)
            {
                //check duplicates
                var exists = originalEvents
                    .Any(m =>
                        m.OsobaId == original.InternalId
                        && m.Type == dEv.Type
                        && m.AddInfo == dEv.AddInfo
                        && m.Organizace == dEv.Organizace
                        && m.AddInfoNum == dEv.AddInfoNum
                        && m.DatumOd == dEv.DatumOd
                        && m.DatumDo == dEv.DatumDo
                        && m.Status == dEv.Status
                    );
                if (exists == false)
                {
                    original.AddOrUpdateEvent(dEv, user);
                }
            }

            OsobaExternalId[] dEids = duplicated.ExternalIds()
                .Where(m => m.ExternalSource != (int)OsobaExternalId.Source.HlidacSmluvGuid)
                .ToArray();
            List<OsobaExternalId> addExternalIds = new List<OsobaExternalId>();
            foreach (var dEid in dEids)
            {
                bool exists = false;
                foreach (var eid in original.ExternalIds())
                {
                    exists = exists || (eid.ExternalId == dEid.ExternalId &&
                                        eid.ExternalSource == dEid.ExternalSource && eid.OsobaId == dEid.OsobaId);
                }

                if (!exists)
                    addExternalIds.Add(dEid);
            }

            if (string.IsNullOrEmpty(original.TitulPred) && !string.IsNullOrEmpty(duplicated.TitulPred))
                original.TitulPred = duplicated.TitulPred;
            if (string.IsNullOrEmpty(original.Jmeno) && !string.IsNullOrEmpty(duplicated.Jmeno))
                original.Jmeno = duplicated.Jmeno;
            if (string.IsNullOrEmpty(original.Prijmeni) && !string.IsNullOrEmpty(duplicated.Prijmeni))
                original.Prijmeni = duplicated.Prijmeni;
            if (string.IsNullOrEmpty(original.TitulPo) && !string.IsNullOrEmpty(duplicated.TitulPo))
                original.TitulPo = duplicated.TitulPo;
            if (string.IsNullOrEmpty(original.Pohlavi) && !string.IsNullOrEmpty(duplicated.Pohlavi))
                original.Pohlavi = duplicated.Pohlavi;
            if (string.IsNullOrEmpty(original.Ulice) && !string.IsNullOrEmpty(duplicated.Ulice))
                original.Ulice = duplicated.Ulice;
            if (string.IsNullOrEmpty(original.Mesto) && !string.IsNullOrEmpty(duplicated.Mesto))
                original.Mesto = duplicated.Mesto;
            if (string.IsNullOrEmpty(original.Psc) && !string.IsNullOrEmpty(duplicated.Psc))
                original.Psc = duplicated.Psc;
            if (string.IsNullOrEmpty(original.CountryCode) && !string.IsNullOrEmpty(duplicated.CountryCode))
                original.CountryCode = duplicated.CountryCode;
            if (string.IsNullOrEmpty(original.PuvodniPrijmeni) && !string.IsNullOrEmpty(duplicated.PuvodniPrijmeni))
                original.PuvodniPrijmeni = duplicated.PuvodniPrijmeni;

            if (!original.Narozeni.HasValue && duplicated.Narozeni.HasValue)
                original.Narozeni = duplicated.Narozeni;
            if (!original.Umrti.HasValue && duplicated.Umrti.HasValue)
                original.Umrti = duplicated.Umrti;
            if (!original.OnRadar && duplicated.OnRadar)
                original.OnRadar = duplicated.OnRadar;

            if (original.Status != (int)Osoba.StatusOsobyEnum.Politik
                && original.Status < duplicated.Status)
                original.Status = duplicated.Status;

            if (string.IsNullOrWhiteSpace(original.WikiId)
                && !string.IsNullOrWhiteSpace(duplicated.WikiId))
                original.WikiId = duplicated.WikiId;

            //obrazek
            if (original.HasPhoto() == false && duplicated.HasPhoto())
            {
                foreach (var fn in new string[]
                    {"small.jpg", "source.txt", "original.uploaded.jpg", "small.uploaded.jpg"})
                {
                    var from = Init.OsobaFotky.GetFullPath(duplicated, fn);
                    var to = Init.OsobaFotky.GetFullPath(original, fn);
                    if (File.Exists(@from))
                        File.Copy(@from, to);
                }
            }

            Save(original, addExternalIds.ToArray());

            if (duplicated.InternalId != 0)
            {
                duplicated.OriginalId = original.InternalId;
                duplicated.Status = (int)Osoba.StatusOsobyEnum.Duplicita;
                Save(duplicated);
            }

            return original;
        }


        public static void FlushCache(int internalId)
        {
            Osoby.CachedEvents.Delete(internalId);
            Osoby.CachedFirmySponzoring.Delete(internalId);
        }


        public static string GetPhotoSource(this Osoba osoba)
        {
            var fn = Init.OsobaFotky.GetFullPath(osoba, "source.txt");
            if (File.Exists(fn))
            {
                try
                {
                    var source = File.ReadAllText(fn)?.Trim() ?? "";
                    if (!string.IsNullOrEmpty(source) && Uri.TryCreate(source, UriKind.Absolute, out var url))
                    {
                        return source;
                    }
                }
                catch (Exception)
                {
                    return null;
                }
            }

            return null;
        }

        public static string GetPhotoPath(this Osoba osoba)
        {
            if (osoba.HasPhoto())
            {
                var path = Init.OsobaFotky.GetFullPath(osoba, "small.jpg");
                return path;
            }
            else
                return Init.WebAppRoot + @"Content\Img\personNoPhoto.png";
        }

        public static string GetPhotoUrl(this Osoba osoba, bool local = false)
        {
            if (local)
            {
                return "/Photo/" + osoba.NameId;
            }
            else
            {
                return "https://www.hlidacstatu.cz/Photo/" + osoba.NameId;
            }
        }

        public static bool HasPhoto(this Osoba osoba)
        {
            var path = Init.OsobaFotky.GetFullPath(osoba, "small.jpg");
            return File.Exists(path);
        }

        public static OsobaExternalId[] ExternalIds(this Osoba osoba)
        {
            using (DbEntities db = new DbEntities())
            {
                return db.OsobaExternalId.AsQueryable().Where(o => o.OsobaId == osoba.InternalId).ToArray();
            }
        }

        public static async Task<List<Osoba>> PeopleWithAnySponzoringRecordAsync(CancellationToken cancellationToken = default)
        {
            await using var db  = new DbEntities();
            var results = await db.Osoba.FromSqlInterpolated($@"
                    Select * 
                      from Osoba os
                     where os.status in ({(int)Osoba.StatusOsobyEnum.NeniPolitik},{(int)Osoba.StatusOsobyEnum.Sponzor})
                       and exists (select 1 from Sponzoring
                                    where os.internalid = osobaiddarce)")
                .ToListAsync(cancellationToken: cancellationToken);
            return results;
        }
        

        public static Osoba.JSON Export(this Osoba osoba, bool allData = false)
        {
            var t = osoba;
            var r = new Osoba.JSON();

            r.Gender = (t.Pohlavi == "f") ? Osoba.JSON.gender.Žena : Osoba.JSON.gender.Muž;
            r.NameId = t.NameId;
            r.Jmeno = t.Jmeno;
            if (allData == false)
                r.Narozeni = t.Narozeni?.ToString("yyyy") ?? "";
            else
                r.Narozeni = t.Narozeni?.ToString("yyyy-MM-dd") ?? "";

            r.Umrti = t.Umrti?.ToString("yyyy-MM-dd") ?? "";
            r.Prijmeni = t.Prijmeni;
            r.Status = (Osoba.StatusOsobyEnum)t.Status;
            var events = osoba.Events();
            if (allData == false)
                events = Enumerable.Empty<OsobaEvent>();

            r.Sponzoring = osoba.Sponzoring(s => s.IcoPrijemce != null)
                    .Select(s => new Osoba.JSON.sponzoring()
                    {
                        Id = s.Id,
                        DarovanoDne = s.DarovanoDne?.ToString("yyyy-MM-dd") ?? "",
                        Hodnota = s.Hodnota,
                        IcoPrijemce = s.IcoPrijemce,
                        Popis = s.Popis,
                        Typ = (Sponzoring.TypDaru)s.Typ,
                        Zdroj = s.Zdroj
                    }).ToArray();

            r.Event = events.Select(m =>
                new Osoba.JSON.ev()
                {
                    Organizace = m.Organizace,
                    DatumOd = m.DatumOd?.ToString("yyyy-MM-dd") ?? "",
                    DatumDo = m.DatumDo?.ToString("yyyy-MM-dd") ?? "",
                    Title = m.Title,
                    Note = m.Note,
                    Typ = (OsobaEvent.Types)m.Type,
                    AddInfoNum = m.AddInfoNum,
                    AddInfo = m.AddInfo,
                    Zdroj = m.Zdroj
                }
                ).ToArray();
            string[] angazovanostDesc = Devmasters.Enums.EnumTools.EnumToEnumerable(typeof(HlidacStatu.Datastructures.Graphs.Relation.RelationSimpleEnum))
                .Where(m => Convert.ToInt32(m.Value) < 0)
                .Select(m => m.Key).ToArray();


            r.Vazbyfirmy = Graph.VsechnyDcerineVazby(osoba)
                .Where(m => angazovanostDesc.Contains(m.Descr))
                .Select(m =>
                    new Osoba.JSON.vazba()
                    {
                        DatumOd = m.RelFrom?.ToString("yyyy-MM-dd") ?? "",
                        DatumDo = m.RelTo?.ToString("yyyy-MM-dd") ?? "",
                        Popis = m.Descr,
                        TypVazby = (Osoba.JSON.typVazby)Enum.Parse(typeof(Osoba.JSON.typVazby), m.Descr, true),
                        VazbaKIco = m.To.Id,
                        VazbaKOsoba = m.To.PrintName()
                        //Zdroj = m.
                    })
                .ToArray();

            return r;
        }

        public enum Zatrideni
        {
            [NiceDisplayName("Politik")]
            Politik,
            [NiceDisplayName("Člen vlády")]
            Ministr,
            [NiceDisplayName("Hejtman")]
            Hejtman,
            [NiceDisplayName("Poslanec")]
            Poslanec,
            [NiceDisplayName("Vedoucí úřadu")]
            SefUradu,
            [NiceDisplayName("Senátor")]
            Senator,
            [NiceDisplayName("Europoslanec")]
            Europoslanec,
            [NiceDisplayName("Poradce předsedy vlády")]
            PoradcePredsedyVlady,
            
        }
        
        public static List<Osoba> GetByZatrideni(Zatrideni zatrideni, DateTime? toDate)
        {
            if (!toDate.HasValue)
                toDate = DateTime.Now;
            
            switch (zatrideni)
            {
                case Zatrideni.Politik:
                    var politickeEventy = new HashSet<int>()
                    {
                        (int)OsobaEvent.Types.Politicka,
                        (int)OsobaEvent.Types.PolitickaPracovni,
                        // Patří sem i volená?
                    };
                    
                    return GetByEvent(e => 
                            politickeEventy.Any(pe => pe == e.Type)
                            && (e.DatumDo == null || e.DatumDo >= toDate)
                            && (e.DatumOd == null || e.DatumOd <= toDate))
                        .ToList();

                case Zatrideni.Ministr:
                    return GetByEvent(e =>
                            e.Type == (int)OsobaEvent.Types.PolitickaPracovni
                            && e.AddInfo.ToLower().StartsWith("ministr")
                            && e.Organizace.ToLower().StartsWith("ministerstvo")
                            && (e.DatumDo == null || e.DatumDo >= toDate)
                            && (e.DatumOd == null || e.DatumOd <= toDate))
                        .ToList();

                case Zatrideni.Hejtman:
                    return GetByEvent(e =>
                            e.Type == (int)OsobaEvent.Types.PolitickaPracovni
                            && e.AddInfo.ToLower().StartsWith("hejtman")
                            && (e.DatumDo == null || e.DatumDo >= toDate)
                            && (e.DatumOd == null || e.DatumOd <= toDate))
                        .ToList();
                
                case Zatrideni.Poslanec:
                    return GetByEvent(e =>
                            e.Type == (int)OsobaEvent.Types.VolenaFunkce
                            && (e.AddInfo.ToLower().StartsWith("poslanec") ||
                                e.AddInfo.ToLower().StartsWith("poslankyně"))
                            && e.Organizace.ToLower().StartsWith("poslanecká sněmovna pčr")
                            && (e.DatumDo == null || e.DatumDo >= toDate)
                            && (e.DatumOd == null || e.DatumOd <= toDate))
                        .ToList();
                
                case Zatrideni.Europoslanec:
                    return GetByEvent(e =>
                            e.Type == (int)OsobaEvent.Types.VolenaFunkce
                            && (e.AddInfo.ToLower().StartsWith("poslanec ep") ||
                                e.AddInfo.ToLower().StartsWith("poslankyně ep"))
                            && e.Organizace.ToLower().StartsWith("evropský parlament")
                            && (e.DatumDo == null || e.DatumDo >= toDate)
                            && (e.DatumOd == null || e.DatumOd <= toDate))
                        .ToList();
                
                case Zatrideni.SefUradu:
                    return GetByEvent(e =>
                            e.Ceo == 1
                            && (e.DatumDo == null || e.DatumDo >= toDate)
                            && (e.DatumOd == null || e.DatumOd <= toDate))
                        .ToList();
                
                case Zatrideni.Senator:
                    return GetByEvent(e =>
                            e.Type == (int)OsobaEvent.Types.VolenaFunkce
                            && e.AddInfo.ToLower().StartsWith("senát")
                            && e.Organizace.ToLower().StartsWith("senát")
                            && (e.DatumDo == null || e.DatumDo >= toDate)
                            && (e.DatumOd == null || e.DatumOd <= toDate))
                        .ToList();
                
                case Zatrideni.PoradcePredsedyVlady:
                    return GetByEvent(e =>
                            e.Type == (int)OsobaEvent.Types.PolitickaPracovni
                            && (e.AddInfo.ToLower().StartsWith("poradce předsedy vlády") ||
                                e.AddInfo.ToLower().StartsWith("poradkyně předsedy vlády"))
                            && e.Organizace.ToLower().StartsWith("úřad vlády čr")
                            && (e.DatumDo == null || e.DatumDo >= toDate)
                            && (e.DatumOd == null || e.DatumOd <= toDate))
                        .ToList();
                
                default:
                    throw new ArgumentOutOfRangeException(nameof(zatrideni), zatrideni, null);
            }
        }
        
    }
}