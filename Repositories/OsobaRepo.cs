using Devmasters;
using Devmasters.Enums;
using EnumsNET;
using HlidacStatu.Connectors;
using HlidacStatu.DS.Graphs;
using HlidacStatu.Entities;
using HlidacStatu.Extensions;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HlidacStatu.Entities.Entities;
using static HlidacStatu.Entities.Osoba;

namespace HlidacStatu.Repositories
{
    public static partial class OsobaRepo
    {
        private static readonly ILogger _logger = Log.ForContext(typeof(OsobaRepo));
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

            //try to create name from surnames
            if (string.IsNullOrWhiteSpace(p.Jmeno) && !string.IsNullOrWhiteSpace(p.Prijmeni))
            {
                var (name, surname) = SplitNameOrSurname(p.Prijmeni);
                p.Jmeno = name;
                p.Prijmeni = surname;
            }

            //try to create surname from names
            if (string.IsNullOrWhiteSpace(p.Prijmeni) && !string.IsNullOrWhiteSpace(p.Jmeno))
            {
                var (name, surname) = SplitNameOrSurname(p.Jmeno);
                p.Jmeno = name;
                p.Prijmeni = surname;
            }

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

        private static (string Name, string Surname) SplitNameOrSurname(string fullname)
        {
            char[] separators = [' ', '\t', '\n'];
            var splittedName = fullname.Split(separators, StringSplitOptions.TrimEntries
                                                              | StringSplitOptions.RemoveEmptyEntries);

            if (splittedName.Length > 1)
                return (splittedName[0], string.Join(" ", splittedName[1..]));
            return (string.Empty, fullname);

        }


        public static Osoba[] ParentOsoby(this Osoba osoba, Relation.AktualnostType minAktualnost)
        {
            var _parents = _getAllParents(osoba.InternalId, minAktualnost)
                    .Select(m => Osoby.GetById.Get(m))
                    .Where(m => m != null)
                    .ToArray();

            return _parents;
        }
        public static HashSet<int> _getAllParents(int osobaInternalId, Relation.AktualnostType minAktualnost,
    HashSet<int> currList = null)
        {
            currList = currList ?? new HashSet<int>();

            HlidacStatu.DS.Graphs.Graph.Edge[] _parentF = Graph.GetDirectParentRelationsOsoby(osobaInternalId).ToArray();
            var _parentVazby = _parentF
                .Where(m => m.Aktualnost >= minAktualnost);

            foreach (var f in _parentVazby)
            {
                if (currList.Contains(Convert.ToInt32(f.To.Id)))
                {
                    //skip
                }
                else
                {
                    currList.Add(Convert.ToInt32(f.From.Id));
                    var newParents = _getAllParents(Convert.ToInt32(f.From.Id), minAktualnost, currList);
                    foreach (var np in newParents)
                    {
                        currList.Add(np);
                    }
                }
            }
            return currList;
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

        /// <summary>
        /// If you want to update, then use Update method
        /// </summary>
        /// <param name="osoba"></param>
        /// <param name="externalIds"></param>
        /// <returns></returns>
        public static Osoba Save(Osoba osoba, params OsobaExternalId[] externalIds)
        {
            using (DbEntities db = new DbEntities())
            {
                osoba.JmenoAscii = TextUtil.RemoveDiacritics(osoba.Jmeno);
                osoba.PrijmeniAscii = TextUtil.RemoveDiacritics(osoba.Prijmeni);
                osoba.PuvodniPrijmeniAscii = TextUtil.RemoveDiacritics(osoba.PuvodniPrijmeni);

                //trim all
                osoba.Jmeno = osoba.Jmeno?.Trim();
                osoba.Prijmeni = osoba.Prijmeni?.Trim();
                osoba.PuvodniPrijmeni = osoba.PuvodniPrijmeni?.Trim();
                osoba.TitulPred = osoba.TitulPred?.Trim();
                osoba.TitulPo = osoba.TitulPo?.Trim();

                if (string.IsNullOrEmpty(osoba.NameId))
                {
                    osoba.NameId = GetUniqueNamedId(osoba);
                }

                osoba.LastUpdate = DateTime.Now;

                if (osoba.Pohlavi != "f" || osoba.Pohlavi != "m")
                {
                    osoba.Pohlavi = osoba.PohlaviCalculated();
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
                    _logger.Error(e, $"Saving osoba {osoba.NameId}");
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
        
        public static Osoba GetByNameIdTracked(DbEntities db, string nameId, bool canReturnDuplicate = false)
        {
            var osoba = db.Osoba.FirstOrDefault(m => m.NameId == nameId);

            if (canReturnDuplicate)
                return osoba;

            return osoba.GetOriginalTracked(db);
        }

        public static Osoba GetByInternalId(int id, bool canReturnDuplicate = false)
        {
            using (DbEntities db = new DbEntities())
            {
                return GetByInternalIdTracked(db, id, canReturnDuplicate);
            }
        }
        
        public static Osoba GetByInternalIdTracked(DbEntities db, int id, bool canReturnDuplicate = false)
        {
            var osoba = db.Osoba.FirstOrDefault(m => m.InternalId == id);

            if (canReturnDuplicate)
                return osoba;
            
            return osoba.GetOriginalTracked(db);
        }

        private static Osoba GetOriginal(this Osoba osoba)
        {
            if (osoba.Status != (int)Osoba.StatusOsobyEnum.Duplicita)
                return osoba;

            using (DbEntities db = new DbEntities())
            {
                return osoba?.GetOriginalTracked(db);
            }
        }
        
        private static Osoba GetOriginalTracked(this Osoba osoba, DbEntities db)
        {
            while (osoba is not null && osoba.Status == (int)Osoba.StatusOsobyEnum.Duplicita)
            {
                osoba = db.Osoba.FirstOrDefault(m => m.InternalId == osoba.OriginalId);
            }

            return osoba;
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

                    AuditRepo.Add<Osoba>(Audit.Operations.Update, user, osobaToUpdate, osobaOriginal);

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
                foreach (var fn in Enums.GetValues<PhotoTypes>().Select(en => en.ToNiceDisplayName()))
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


        public static void DeleteAllPhotoVariants(this Osoba osoba, bool includingOriginalUpload = false)
        {

            foreach (var ft in Enums.GetValues<PhotoTypes>())
            {
                if (ft == PhotoTypes.UploadedOriginal)
                {
                    if (includingOriginalUpload)
                        Devmasters.IO.IOTools.DeleteFile(osoba.GetPhotoPath(ft, true));
                }
                else
                    Devmasters.IO.IOTools.DeleteFile(osoba.GetPhotoPath(ft, true));

            }
        }

        public static string GetSourceOfPhoto(this Osoba osoba)
        {
            var fn = osoba.GetPhotoPath(PhotoTypes.SourceOfPhoto, true);
            if (System.IO.File.Exists(fn))
                return System.IO.File.ReadAllText(fn);
            else
                return string.Empty;
        }
        public static string GetPhotoPath(this Osoba osoba, PhotoTypes phototype = PhotoTypes.Small, bool ignoreMissingFile = false)
        {
            return _getPhotoPath(osoba, phototype.ToNiceDisplayName(), ignoreMissingFile);
        }
        private static string _getPhotoPath(this Osoba osoba, string anotherName = null, bool ignoreMissingFile = false)
        {
            var defaultFn = Init.OsobaFotky.GetFullPath(osoba, PhotoTypes.Small.ToNiceDisplayName());
            if (System.IO.File.Exists(defaultFn) || ignoreMissingFile)
            {
                if (!string.IsNullOrEmpty(anotherName)
                    && (System.IO.File.Exists(Init.OsobaFotky.GetFullPath(osoba, anotherName)) || ignoreMissingFile)

                    )
                {
                    return Init.OsobaFotky.GetFullPath(osoba, anotherName);
                }
                var path = Init.OsobaFotky.GetFullPath(osoba, PhotoTypes.Small.ToNiceDisplayName());
                return path;
            }
            else
                return Init.WebAppRoot + @"Content\Img\personNoPhoto.png";
        }

        public static string GetPhotoUrl(this Osoba osoba, bool local = false, PhotoTypes phototype = PhotoTypes.Small, bool randomizeUrl = false)
        {
            return _getPhotoUrl(osoba, local, phototype.ToString() + (randomizeUrl ? $"&rnd={Random.Shared.Next()}" : ""));
        }
        private static string _getPhotoUrl(this Osoba osoba, bool local = false, string option = "")
        {
            if (local)
            {
                return "/Photo/" + osoba.NameId + (string.IsNullOrEmpty(option) ? "" : $"?phototype={option}");
            }
            else
            {
                return "https://www.hlidacstatu.cz/Photo/" + osoba.NameId + (string.IsNullOrEmpty(option) ? "" : $"?phototype={option}");
            }
        }

        public static bool HasPhoto(this Osoba osoba)
        {
            var path = osoba.GetPhotoPath(PhotoTypes.Small, true); //Init.OsobaFotky.GetFullPath(osoba, PhotoTypes.Small.ToNiceDisplayName()); // "small.jpg"
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
            await using var db = new DbEntities();
            var results = await db.Osoba.FromSqlInterpolated($@"
                    Select * 
                      from Osoba os
                     where os.status = {(int)Osoba.StatusOsobyEnum.NeniPolitik}
                       and exists (select 1 from Sponzoring
                                    where os.internalid = osobaiddarce)
                     union 
                    SELECT * 
                      from Osoba os
                     where os.status = {(int)Osoba.StatusOsobyEnum.Sponzor}")
                .ToListAsync(cancellationToken: cancellationToken);
            return results;
        }

        public static  List<Osoba> PeopleWithAnySponzoringRecord(Expression<Func<Osoba, bool>> predicate)
        {
            predicate = predicate ?? (s => true);
            using var db = new DbEntities();

            var neniPolitik = (int)Osoba.StatusOsobyEnum.NeniPolitik;
            var sponzor = (int)Osoba.StatusOsobyEnum.Sponzor;

            var query1 = db.Osoba
                .Where(os => db.Sponzoring.Any(s => s.OsobaIdDarce == os.InternalId))
                .Where(predicate)
                .Distinct()
                .ToArray();

            var query2 = db.Osoba
                .Where(os => os.Status == sponzor)
                .Where(predicate)
                .Distinct()
                .ToArray();

            var results = query1
                .Union(query2)
                .Distinct()
                .ToList();

            return results;
        }


        public static async Task<List<Osoba>> PeopleWithAnyEventAsync(CancellationToken cancellationToken = default)
        {
            await using var db = new DbEntities();
            FormattableString sql = $@"
                select distinct * from osoba o
                    where o.InternalId in 
                    (
	                    select distinct osobaid from osobaEvent
                         union 
                        SELECT InternalId
                          from Osoba os
                         where os.status = {(int)Osoba.StatusOsobyEnum.Sponzor}
                        union
                        SELECT InternalId
                          from Osoba os
                         where os.status = {(int)Osoba.StatusOsobyEnum.Politik}
                        union
                        SELECT InternalId
                          from Osoba os
                         where os.status = {(int)Osoba.StatusOsobyEnum.ByvalyPolitik}
                        union
                        SELECT InternalId
                          from Osoba os
                         where os.status = {(int)Osoba.StatusOsobyEnum.VazbyNaPolitiky}
                        union
                        SELECT InternalId
                          from Osoba os
                         where os.status = {(int)Osoba.StatusOsobyEnum.Sponzor}
                        union
                        SELECT InternalId
                          from Osoba os
                         where os.status = {(int)Osoba.StatusOsobyEnum.VysokyUrednik}
                    ) 

                ";
            var results = await db.Osoba.FromSqlInterpolated(sql)
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
            string[] angazovanostDesc = Devmasters.Enums.EnumTools.EnumToEnumerable(typeof(HlidacStatu.DS.Graphs.Relation.RelationSimpleEnum))
                .Where(m => Convert.ToInt32(m.Id) < 0)
                .Select(m => m.Name).ToArray();


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
            [NiceDisplayName("Předseda vlády")]
            PredsedaVlady,
            [NiceDisplayName("Krajský zastupitel")]
            KrajskyZastupitel,

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
                        (int)OsobaEvent.Types.PolitickaExekutivni,
                        // Patří sem i volená?
                    };

                    return GetByEvent(e =>
                            politickeEventy.Any(pe => pe == e.Type)
                            && (e.DatumDo == null || e.DatumDo >= toDate)
                            && (e.DatumOd == null || e.DatumOd <= toDate))
                        .ToList();

                case Zatrideni.Ministr:
                    return GetByEvent(e =>
                            e.Type == (int)OsobaEvent.Types.PolitickaExekutivni
                            && e.AddInfo.ToLower().StartsWith("ministr")
                            && (e.Organizace.ToLower().StartsWith("ministerstvo")
                                || Constants.Ica.Vlada.Contains(e.Ico.Trim()))
                            && (e.DatumDo == null || e.DatumDo >= toDate)
                            && (e.DatumOd == null || e.DatumOd <= toDate))
                        .ToList();
                
                case Zatrideni.PredsedaVlady:
                    return GetByEvent(e =>
                            e.Type == (int)OsobaEvent.Types.PolitickaExekutivni
                            && (e.AddInfo.ToLower().StartsWith("předseda vlády")
                                || e.AddInfo.ToLower().StartsWith("předsedkyně vlády"))
                            && (e.Organizace.ToLower().StartsWith("úřad vlády")
                                || e.Ico.Trim() == Constants.Ica.UradVlady)
                            && (e.DatumDo == null || e.DatumDo >= toDate)
                            && (e.DatumOd == null || e.DatumOd <= toDate))
                        .ToList();

                case Zatrideni.Hejtman:
                    return GetByEvent(e =>
                            e.Type == (int)OsobaEvent.Types.PolitickaExekutivni
                            && e.AddInfo.ToLower().StartsWith("hejtman")
                            && (e.DatumDo == null || e.DatumDo >= toDate)
                            && (e.DatumOd == null || e.DatumOd <= toDate))
                        .ToList();

                case Zatrideni.Poslanec:
                    return GetByEvent(e =>
                            e.Type == (int)OsobaEvent.Types.VolenaFunkce
                            && (e.AddInfo.ToLower().StartsWith("poslanec") ||
                                e.AddInfo.ToLower().StartsWith("poslankyně"))
                            && (e.Organizace.ToLower().StartsWith("poslanecká sněmovna pčr")
                                || e.Ico.Trim() == Constants.Ica.KancelarPoslaneckeSnemovny)
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
                            && (e.Organizace.ToLower().StartsWith("senát")
                                || e.Ico.Trim() == Constants.Ica.Senat)
                            && (e.DatumDo == null || e.DatumDo >= toDate)
                            && (e.DatumOd == null || e.DatumOd <= toDate))
                        .ToList();

                case Zatrideni.PoradcePredsedyVlady:
                    return GetByEvent(e =>
                            e.Type == (int)OsobaEvent.Types.PolitickaExekutivni
                            && (e.AddInfo.ToLower().StartsWith("poradce předsedy vlády") ||
                                e.AddInfo.ToLower().StartsWith("poradkyně předsedy vlády"))
                            && (e.Organizace.ToLower().StartsWith("úřad vlády")
                                || e.Ico.Trim() == Constants.Ica.UradVlady)
                            && (e.DatumDo == null || e.DatumDo >= toDate)
                            && (e.DatumOd == null || e.DatumOd <= toDate))
                        .ToList();
                
                case Zatrideni.KrajskyZastupitel:
                    return GetByEvent(e =>
                            e.Type == (int)OsobaEvent.Types.VolenaFunkce
                            && e.AddInfo.ToLower().StartsWith("zastupitel")
                            && Constants.Ica.Kraje.Contains(e.Ico.Trim())
                            && (e.DatumDo == null || e.DatumDo >= toDate)
                            && (e.DatumOd == null || e.DatumOd <= toDate))
                        .ToList();

                default:
                    throw new ArgumentOutOfRangeException(nameof(zatrideni), zatrideni, null);
            }
        }
        
        /// <summary>
        /// vrací všechny osoby pro daný rok
        /// </summary>
        public static List<Osoba> GetByZatrideni(Zatrideni zatrideni, int year)
        {
            switch (zatrideni)
            {
                case Zatrideni.Politik:
                    var politickeEventy = new HashSet<int>()
                    {
                        (int)OsobaEvent.Types.Politicka,
                        (int)OsobaEvent.Types.PolitickaExekutivni,
                        // Patří sem i volená?
                    };

                    return GetByEvent(e =>
                            politickeEventy.Any(pe => pe == e.Type)
                            && (e.DatumDo == null || e.DatumDo.Value.Year >= year)
                            && (e.DatumOd == null || e.DatumOd.Value.Year <= year))
                        .ToList();

                case Zatrideni.Ministr:
                    return GetByEvent(e =>
                            e.Type == (int)OsobaEvent.Types.PolitickaExekutivni
                            && e.AddInfo.ToLower().StartsWith("ministr")
                            && (e.Organizace.ToLower().StartsWith("ministerstvo")
                                || Constants.Ica.Vlada.Contains(e.Ico.Trim()))
                            && (e.DatumDo == null || e.DatumDo.Value.Year >= year)
                            && (e.DatumOd == null || e.DatumOd.Value.Year <= year))
                        .ToList();
                
                case Zatrideni.PredsedaVlady:
                    return GetByEvent(e =>
                            e.Type == (int)OsobaEvent.Types.PolitickaExekutivni
                            && (e.AddInfo.ToLower().StartsWith("předseda vlády")
                                || e.AddInfo.ToLower().StartsWith("předsedkyně vlády"))
                            && (e.Organizace.ToLower().StartsWith("úřad vlády")
                                || e.Ico.Trim() == Constants.Ica.UradVlady)
                            && (e.DatumDo == null || e.DatumDo.Value.Year >= year)
                            && (e.DatumOd == null || e.DatumOd.Value.Year <= year))
                        .ToList();

                case Zatrideni.Hejtman:
                    return GetByEvent(e =>
                            e.Type == (int)OsobaEvent.Types.PolitickaExekutivni
                            && e.AddInfo.ToLower().StartsWith("hejtman")
                            && (e.DatumDo == null || e.DatumDo.Value.Year >= year)
                            && (e.DatumOd == null || e.DatumOd.Value.Year <= year))
                        .ToList();

                case Zatrideni.Poslanec:
                    return GetByEvent(e =>
                            e.Type == (int)OsobaEvent.Types.VolenaFunkce
                            && (e.AddInfo.ToLower().StartsWith("poslanec") ||
                                e.AddInfo.ToLower().StartsWith("poslankyně"))
                            && (e.Organizace.ToLower().StartsWith("poslanecká sněmovna pčr")
                                || e.Ico.Trim() == Constants.Ica.KancelarPoslaneckeSnemovny)
                            && (e.DatumDo == null || e.DatumDo.Value.Year >= year)
                            && (e.DatumOd == null || e.DatumOd.Value.Year <= year))
                        .ToList();

                case Zatrideni.Europoslanec:
                    return GetByEvent(e =>
                            e.Type == (int)OsobaEvent.Types.VolenaFunkce
                            && (e.AddInfo.ToLower().StartsWith("poslanec ep") ||
                                e.AddInfo.ToLower().StartsWith("poslankyně ep"))
                            && e.Organizace.ToLower().StartsWith("evropský parlament")
                            && (e.DatumDo == null || e.DatumDo.Value.Year >= year)
                            && (e.DatumOd == null || e.DatumOd.Value.Year <= year))
                        .ToList();

                case Zatrideni.SefUradu:
                    return GetByEvent(e =>
                            e.Ceo == 1
                            && (e.DatumDo == null || e.DatumDo.Value.Year >= year)
                            && (e.DatumOd == null || e.DatumOd.Value.Year <= year))
                        .ToList();

                case Zatrideni.Senator:
                    return GetByEvent(e =>
                            e.Type == (int)OsobaEvent.Types.VolenaFunkce
                            && e.AddInfo.ToLower().StartsWith("senát")
                            && (e.Organizace.ToLower().StartsWith("senát")
                                || e.Ico.Trim() == Constants.Ica.Senat)
                            && (e.DatumDo == null || e.DatumDo.Value.Year >= year)
                            && (e.DatumOd == null || e.DatumOd.Value.Year <= year))
                        .ToList();

                case Zatrideni.PoradcePredsedyVlady:
                    return GetByEvent(e =>
                            e.Type == (int)OsobaEvent.Types.PolitickaExekutivni
                            && (e.AddInfo.ToLower().StartsWith("poradce předsedy vlády") ||
                                e.AddInfo.ToLower().StartsWith("poradkyně předsedy vlády"))
                            && (e.Organizace.ToLower().StartsWith("úřad vlády")
                                || e.Ico.Trim() == Constants.Ica.UradVlady)
                            && (e.DatumDo == null || e.DatumDo.Value.Year >= year)
                            && (e.DatumOd == null || e.DatumOd.Value.Year <= year))
                        .ToList();
                
                case Zatrideni.KrajskyZastupitel:
                    return GetByEvent(e =>
                            e.Type == (int)OsobaEvent.Types.VolenaFunkce
                            && e.AddInfo.ToLower().StartsWith("zastupitel")
                            && Constants.Ica.Kraje.Contains(e.Ico.Trim())
                            && (e.DatumDo == null || e.DatumDo.Value.Year >= year)
                            && (e.DatumOd == null || e.DatumOd.Value.Year <= year))
                        .ToList();

                default:
                    throw new ArgumentOutOfRangeException(nameof(zatrideni), zatrideni, null);
            }
        }

    }
}