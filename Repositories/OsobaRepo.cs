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
using HlidacStatu.Repositories.Cache;
using static HlidacStatu.Entities.Osoba;

namespace HlidacStatu.Repositories
{
    public static partial class OsobaRepo
    {
        private static readonly ILogger _logger = Log.ForContext(typeof(OsobaRepo));
        

        public static async Task<Osoba> GetOrCreateNewAsync(string titulPred, string jmeno, string prijmeni, string titulPo,
            DateTime? narozeni, Osoba.StatusOsobyEnum status, string user, DateTime? umrti = null)
        {
            var p = new Osoba
            {
                TitulPred = Osoba.NormalizeTitul(titulPred, true),
                TitulPo = Osoba.NormalizeTitul(titulPo, false),
                Jmeno = Osoba.NormalizeJmeno(jmeno),
                Prijmeni = Osoba.NormalizePrijmeni(prijmeni)
            };

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
                await SaveAsync(p);
                AuditRepo.Add(Audit.Operations.Create, user, p, null);
                return p;
            }

            var exiO = await Searching.GetByNameAsync(p.Jmeno, p.Prijmeni, narozeni.Value);


            if (exiO == null)
            {
                p.Umrti = umrti;
                p.Status = (int)status;
                p.Narozeni = narozeni;
                await SaveAsync(p);
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


        public static async Task<List<Osoba>> ParentOsobyAsync(this Osoba osoba, Relation.AktualnostType minAktualnost)
        {
            var parentIds = GetAllParents(osoba.InternalId, minAktualnost).ToList();
            var parents = new List<Osoba>();

            foreach (var id in parentIds)
            {
                var parent = await OsobaCache.GetPersonByIdAsync(id);
                if (parent != null)
                {
                    parents.Add(parent);
                }
            }
            
            return parents;
        }

        private static HashSet<int> GetAllParents(int osobaInternalId, Relation.AktualnostType minAktualnost,
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
                    var newParents = GetAllParents(Convert.ToInt32(f.From.Id), minAktualnost, currList);
                    foreach (var np in newParents)
                    {
                        currList.Add(np);
                    }
                }
            }
            return currList;
        }


        public static async Task<string> GetUniqueNamedIdAsync(Osoba osoba)
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
            
            await using DbEntities db = new DbEntities();
            do
            {
                exists = await db.Osoba.AsNoTracking()
                    .Where(m => m.NameId.StartsWith(checkUniqueName))
                    .OrderByDescending(m => m.NameId)
                    .FirstOrDefaultAsync();
                
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

            return basic;
        }

        /// <summary>
        /// If you want to update, then use Update method
        /// </summary>
        /// <param name="osoba"></param>
        /// <param name="externalIds"></param>
        /// <returns></returns>
        public static async Task<Osoba> SaveAsync(Osoba osoba, params OsobaExternalId[] externalIds)
        {
            await using DbEntities db = new DbEntities();
            osoba.JmenoAscii = osoba.Jmeno.RemoveDiacritics();
            osoba.PrijmeniAscii = osoba.Prijmeni.RemoveDiacritics();
            osoba.PuvodniPrijmeniAscii = osoba.PuvodniPrijmeni.RemoveDiacritics();

            //trim all
            osoba.Jmeno = osoba.Jmeno?.Trim();
            osoba.Prijmeni = osoba.Prijmeni?.Trim();
            osoba.PuvodniPrijmeni = osoba.PuvodniPrijmeni?.Trim();
            osoba.TitulPred = osoba.TitulPred?.Trim();
            osoba.TitulPo = osoba.TitulPo?.Trim();

            if (string.IsNullOrEmpty(osoba.NameId))
            {
                osoba.NameId = await GetUniqueNamedIdAsync(osoba);
            }

            osoba.LastUpdate = DateTime.Now;

            if (osoba.Pohlavi != "f" || osoba.Pohlavi != "m")
            {
                osoba.Pohlavi = osoba.PohlaviCalculated();
            }

            db.Osoba.Attach(osoba);


            db.Entry(osoba).State = osoba.InternalId == 0 ? EntityState.Added : EntityState.Modified;

            try
            {
                await db.SaveChangesAsync();
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

            return osoba;
        }

        public static async Task<Osoba> GetByNameIdAsync(string nameId, bool canReturnDuplicate = false)
        {
            await using DbEntities db = new DbEntities();
            var osoba = await db.Osoba.AsQueryable()
                .Where(m =>
                    m.NameId == nameId
                )
                .FirstOrDefaultAsync();

            if(osoba is null)
                return null;
            
            if (canReturnDuplicate)
                return osoba;

            return await osoba.GetOriginalAsync();
        }
        
        public static async Task<Osoba> GetByNameIdTrackedAsync(DbEntities db, string nameId, bool canReturnDuplicate = false)
        {
            var osoba = await db.Osoba.FirstOrDefaultAsync(m => m.NameId == nameId);

            if (canReturnDuplicate)
                return osoba;

            return await osoba.GetOriginalTrackedAsync(db);
        }

        public static async Task<Osoba> GetByInternalIdAsync(int id, bool canReturnDuplicate = false)
        {
            await using DbEntities db = new DbEntities();
            return await GetByInternalIdTrackedAsync(db, id, canReturnDuplicate);
        }
        
        public static async Task<Osoba> GetByInternalIdTrackedAsync(DbEntities db, int id, bool canReturnDuplicate = false)
        {
            var osoba = await db.Osoba.FirstOrDefaultAsync(m => m.InternalId == id);

            if (canReturnDuplicate)
                return osoba;
            
            return await osoba.GetOriginalTrackedAsync(db);
        }

        private static async Task<Osoba> GetOriginalAsync(this Osoba osoba)
        {
            if (osoba.Status != (int)Osoba.StatusOsobyEnum.Duplicita)
                return osoba;

            await using DbEntities db = new DbEntities();
            return await osoba?.GetOriginalTrackedAsync(db);
        }
        
        public static async Task<Osoba> GetOriginalTrackedAsync(this Osoba osoba, DbEntities db)
        {
            while (osoba is not null && osoba.Status == (int)Osoba.StatusOsobyEnum.Duplicita)
            {
                osoba = await db.Osoba.FirstOrDefaultAsync(m => m.InternalId == osoba.OriginalId);
            }

            return osoba;
        }

        public static async Task<Osoba> GetByExternalIDAsync(string exId, OsobaExternalId.Source source)
        {
            await using DbEntities db = new DbEntities();
            var oei = await db.OsobaExternalId.AsNoTracking()
                .FirstOrDefaultAsync(m => m.ExternalId == exId && m.ExternalSource == (int)source);
            
            if (oei == null)
                return null;
            else
                return await GetByInternalIdAsync(oei.OsobaId);
        }

        public static async Task<List<Osoba>> GetByEventAsync(Expression<Func<OsobaEvent, bool>> predicate)
        {
            await using DbEntities db = new DbEntities();
            var events = db.OsobaEvent
                .Where(predicate);

            var people = db.Osoba.AsNoTracking().Where(o => events.Any(e => e.OsobaId == o.InternalId));

            return await people.Distinct().ToListAsync();
        }

        public static async Task SetManualTimeStampAsync(int osobaId, string author)
        {
            await using DbEntities db = new DbEntities();
            var osobaToUpdate = await db.Osoba.FirstOrDefaultAsync(m => m.InternalId == osobaId);

            if (osobaToUpdate != null)
            {
                osobaToUpdate.ManuallyUpdated = DateTime.Now;
                osobaToUpdate.ManuallyUpdatedBy = author;
                await db.SaveChangesAsync();
            }
        }

        public static async Task<Osoba> UpdateAsync(Osoba osoba, string user)
        {
            await using DbEntities db = new DbEntities();
            var osobaDb = await db.Osoba
                .AsQueryable().FirstOrDefaultAsync(m => m.InternalId == osoba.InternalId);

            if (osobaDb is null)
            {
                return osoba;
            }
                
            var osobaToUpdate = await osobaDb.GetOriginalAsync();

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

                await SaveAsync(osobaToUpdate);

                AuditRepo.Add<Osoba>(Audit.Operations.Update, user, osobaToUpdate, osobaOriginal);

                return osobaToUpdate;
            }

            return osoba;
        }


        public static async Task<Osoba> MergeWithAsync(this Osoba original, Osoba duplicated, string user)
        {
            if (original.InternalId == duplicated.InternalId)
                return original;

            //todo: předělat do transakce tak, aby se neukládaly samostatné části ?!
            await SponzoringRepo.MergeDonatingOsobaAsync(original.InternalId, duplicated.InternalId, user);

            List<OsobaEvent> duplicateEvents = await OsobaEventRepo.GetByOsobaIdAsync(duplicated.InternalId);
            List<OsobaEvent> originalEvents = await OsobaEventRepo.GetByOsobaIdAsync(original.InternalId);
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
                    await original.AddOrUpdateEventAsync(dEv, user);
                }
            }

            OsobaExternalId[] dEids = (await duplicated.ExternalIdsAsync())
                .Where(m => m.ExternalSource != (int)OsobaExternalId.Source.HlidacSmluvGuid)
                .ToArray();
            List<OsobaExternalId> addExternalIds = new List<OsobaExternalId>();
            foreach (var dEid in dEids)
            {
                bool exists = false;
                foreach (var eid in await original.ExternalIdsAsync())
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

            await SaveAsync(original, addExternalIds.ToArray());

            if (duplicated.InternalId != 0)
            {
                duplicated.OriginalId = original.InternalId;
                duplicated.Status = (int)Osoba.StatusOsobyEnum.Duplicita;
                await SaveAsync(duplicated);
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
            return GetPhotoPath(osoba, phototype.ToNiceDisplayName(), ignoreMissingFile);
        }
        private static string GetPhotoPath(this Osoba osoba, string anotherName = null, bool ignoreMissingFile = false)
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
            return GetPhotoUrl(osoba, local, phototype.ToString() + (randomizeUrl ? $"&rnd={Random.Shared.Next()}" : ""));
        }
        private static string GetPhotoUrl(this Osoba osoba, bool local = false, string option = "")
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
            var path = osoba.GetPhotoPath(PhotoTypes.Small, true);
            return File.Exists(path);
        }

        public static async Task<OsobaExternalId[]> ExternalIdsAsync(this Osoba osoba)
        {
            await using DbEntities db = new DbEntities();
            return await db.OsobaExternalId.AsNoTracking().Where(o => o.OsobaId == osoba.InternalId).ToArrayAsync();
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
                .AsNoTracking()
                .ToListAsync(cancellationToken: cancellationToken);
            return results;
        }

        private static async Task<List<Osoba>> PeopleWithAnySponzoringRecordAsync(Expression<Func<Osoba, bool>> predicate)
        {
            predicate = predicate ?? (s => true);
            await using var db = new DbEntities();

            var sponzor = (int)Osoba.StatusOsobyEnum.Sponzor;

            var query1 = await db.Osoba
                .Where(os => db.Sponzoring.Any(s => s.OsobaIdDarce == os.InternalId))
                .Where(predicate)
                .AsNoTracking()
                .Distinct()
                .ToArrayAsync();

            var query2 = await db.Osoba
                .Where(os => os.Status == sponzor)
                .Where(predicate)
                .AsNoTracking()
                .Distinct()
                .ToArrayAsync();

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
                .AsNoTracking()
                .ToListAsync(cancellationToken: cancellationToken);
            return results;
        }

        public static async Task<JSON> ExportAsync(this Osoba osoba, bool allData = false)
        {
            var t = osoba;
            var r = new Osoba.JSON
            {
                Gender = (t.Pohlavi == "f") ? Osoba.JSON.gender.Žena : Osoba.JSON.gender.Muž,
                NameId = t.NameId,
                Jmeno = t.Jmeno
            };

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

            r.Sponzoring = (await osoba.SponzoringAsync(s => s.IcoPrijemce != null))
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

            
            var vazby = (await Repositories.Graph.VsechnyDcerineVazbyAsync(osoba, Relation.CharakterVazbyEnum.VlastnictviKontrola))
                .Where(m => angazovanostDesc.Contains(m.Descr))
                .ToList();

            var vazbyList = new List<Osoba.JSON.vazba>();
            foreach (var m in vazby)
            {
                var vazbaKOsoba = await m.To.PrintNameAsync();
                vazbyList.Add(new Osoba.JSON.vazba()
                {
                    DatumOd = m.RelFrom?.ToString("yyyy-MM-dd") ?? "",
                    DatumDo = m.RelTo?.ToString("yyyy-MM-dd") ?? "",
                    Popis = m.Descr,
                    TypVazby = (Osoba.JSON.typVazby)Enum.Parse(typeof(Osoba.JSON.typVazby), m.Descr, true),
                    VazbaKIco = m.To.Id,
                    VazbaKOsoba = vazbaKOsoba
                });
            }
            r.Vazbyfirmy = vazbyList.ToArray();
            
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

        public static async Task<List<Osoba>> GetByZatrideniAsync(Zatrideni zatrideni, DateTime? toDate)
        {
            toDate ??= DateTime.Now;

            switch (zatrideni)
            {
                case Zatrideni.Politik:
                    var politickeEventy = new HashSet<int>()
                    {
                        (int)OsobaEvent.Types.Politicka,
                        (int)OsobaEvent.Types.PolitickaExekutivni,
                        // Patří sem i volená?
                    };

                    return await GetByEventAsync(e =>
                            politickeEventy.Any(pe => pe == e.Type)
                            && (e.DatumDo == null || e.DatumDo >= toDate)
                            && (e.DatumOd == null || e.DatumOd <= toDate));

                case Zatrideni.Ministr:
                    return await GetByEventAsync(e =>
                            e.Type == (int)OsobaEvent.Types.PolitickaExekutivni
                            && e.AddInfo.ToLower().StartsWith("ministr")
                            && (e.Organizace.ToLower().StartsWith("ministerstvo")
                                || UradyConstants.Ica.Vlada.Contains(e.Ico.Trim()))
                            && (e.DatumDo == null || e.DatumDo >= toDate)
                            && (e.DatumOd == null || e.DatumOd <= toDate));
                
                case Zatrideni.PredsedaVlady:
                    return await GetByEventAsync(e =>
                            e.Type == (int)OsobaEvent.Types.PolitickaExekutivni
                            && (e.AddInfo.ToLower().StartsWith("předseda vlády")
                                || e.AddInfo.ToLower().StartsWith("předsedkyně vlády"))
                            && (e.Organizace.ToLower().StartsWith("úřad vlády")
                                || e.Ico.Trim() == UradyConstants.Ica.UradVlady)
                            && (e.DatumDo == null || e.DatumDo >= toDate)
                            && (e.DatumOd == null || e.DatumOd <= toDate));

                case Zatrideni.Hejtman:
                    return await GetByEventAsync(e =>
                            e.Type == (int)OsobaEvent.Types.PolitickaExekutivni
                            && e.AddInfo.ToLower().StartsWith("hejtman")
                            && (e.DatumDo == null || e.DatumDo >= toDate)
                            && (e.DatumOd == null || e.DatumOd <= toDate));

                case Zatrideni.Poslanec:
                    return await GetByEventAsync(e =>
                            e.Type == (int)OsobaEvent.Types.VolenaFunkce
                            && (e.AddInfo.ToLower().StartsWith("poslanec") ||
                                e.AddInfo.ToLower().StartsWith("poslankyně"))
                            && (e.Organizace.ToLower().StartsWith("poslanecká sněmovna pčr")
                                || e.Ico.Trim() == UradyConstants.Ica.KancelarPoslaneckeSnemovny)
                            && (e.DatumDo == null || e.DatumDo >= toDate)
                            && (e.DatumOd == null || e.DatumOd <= toDate));

                case Zatrideni.Europoslanec:
                    return await GetByEventAsync(e =>
                            e.Type == (int)OsobaEvent.Types.VolenaFunkce
                            && (e.AddInfo.ToLower().StartsWith("poslanec ep") ||
                                e.AddInfo.ToLower().StartsWith("poslankyně ep"))
                            && e.Organizace.ToLower().StartsWith("evropský parlament")
                            && (e.DatumDo == null || e.DatumDo >= toDate)
                            && (e.DatumOd == null || e.DatumOd <= toDate));

                case Zatrideni.SefUradu:
                    return await GetByEventAsync(e =>
                            e.Ceo == 1
                            && (e.DatumDo == null || e.DatumDo >= toDate)
                            && (e.DatumOd == null || e.DatumOd <= toDate));

                case Zatrideni.Senator:
                    return await GetByEventAsync(e =>
                            e.Type == (int)OsobaEvent.Types.VolenaFunkce
                            && e.AddInfo.ToLower().StartsWith("senát")
                            && (e.Organizace.ToLower().StartsWith("senát")
                                || e.Ico.Trim() == UradyConstants.Ica.Senat)
                            && (e.DatumDo == null || e.DatumDo >= toDate)
                            && (e.DatumOd == null || e.DatumOd <= toDate));

                case Zatrideni.PoradcePredsedyVlady:
                    return await GetByEventAsync(e =>
                            e.Type == (int)OsobaEvent.Types.PolitickaExekutivni
                            && (e.AddInfo.ToLower().StartsWith("poradce předsedy vlády") ||
                                e.AddInfo.ToLower().StartsWith("poradkyně předsedy vlády"))
                            && (e.Organizace.ToLower().StartsWith("úřad vlády")
                                || e.Ico.Trim() == UradyConstants.Ica.UradVlady)
                            && (e.DatumDo == null || e.DatumDo >= toDate)
                            && (e.DatumOd == null || e.DatumOd <= toDate));
                
                case Zatrideni.KrajskyZastupitel:
                    return await GetByEventAsync(e =>
                            e.Type == (int)OsobaEvent.Types.VolenaFunkce
                            && e.AddInfo.ToLower().StartsWith("zastupitel")
                            && UradyConstants.Ica.Kraje.Contains(e.Ico.Trim())
                            && (e.DatumDo == null || e.DatumDo >= toDate)
                            && (e.DatumOd == null || e.DatumOd <= toDate));

                default:
                    throw new ArgumentOutOfRangeException(nameof(zatrideni), zatrideni, null);
            }
        }
        
        /// <summary>
        /// vrací všechny osoby pro daný rok
        /// </summary>
        public static async Task<List<Osoba>> GetByZatrideniAsync(Zatrideni zatrideni, int year)
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

                    return await GetByEventAsync(e =>
                            politickeEventy.Any(pe => pe == e.Type)
                            && (e.DatumDo == null || e.DatumDo.Value.Year >= year)
                            && (e.DatumOd == null || e.DatumOd.Value.Year <= year));

                case Zatrideni.Ministr:
                    return await GetByEventAsync(e =>
                            e.Type == (int)OsobaEvent.Types.PolitickaExekutivni
                            && e.AddInfo.ToLower().StartsWith("ministr")
                            && (e.Organizace.ToLower().StartsWith("ministerstvo")
                                || UradyConstants.Ica.Vlada.Contains(e.Ico.Trim()))
                            && (e.DatumDo == null || e.DatumDo.Value.Year >= year)
                            && (e.DatumOd == null || e.DatumOd.Value.Year <= year));
                
                case Zatrideni.PredsedaVlady:
                    return await GetByEventAsync(e =>
                            e.Type == (int)OsobaEvent.Types.PolitickaExekutivni
                            && (e.AddInfo.ToLower().StartsWith("předseda vlády")
                                || e.AddInfo.ToLower().StartsWith("předsedkyně vlády"))
                            && (e.Organizace.ToLower().StartsWith("úřad vlády")
                                || e.Ico.Trim() == UradyConstants.Ica.UradVlady)
                            && (e.DatumDo == null || e.DatumDo.Value.Year >= year)
                            && (e.DatumOd == null || e.DatumOd.Value.Year <= year));

                case Zatrideni.Hejtman:
                    return await GetByEventAsync(e =>
                            e.Type == (int)OsobaEvent.Types.PolitickaExekutivni
                            && e.AddInfo.ToLower().StartsWith("hejtman")
                            && (e.DatumDo == null || e.DatumDo.Value.Year >= year)
                            && (e.DatumOd == null || e.DatumOd.Value.Year <= year));

                case Zatrideni.Poslanec:
                    return await GetByEventAsync(e =>
                            e.Type == (int)OsobaEvent.Types.VolenaFunkce
                            && (e.AddInfo.ToLower().StartsWith("poslanec") ||
                                e.AddInfo.ToLower().StartsWith("poslankyně"))
                            && (e.Organizace.ToLower().StartsWith("poslanecká sněmovna pčr")
                                || e.Ico.Trim() == UradyConstants.Ica.KancelarPoslaneckeSnemovny)
                            && (e.DatumDo == null || e.DatumDo.Value.Year >= year)
                            && (e.DatumOd == null || e.DatumOd.Value.Year <= year));

                case Zatrideni.Europoslanec:
                    return await GetByEventAsync(e =>
                            e.Type == (int)OsobaEvent.Types.VolenaFunkce
                            && (e.AddInfo.ToLower().StartsWith("poslanec ep") ||
                                e.AddInfo.ToLower().StartsWith("poslankyně ep"))
                            && e.Organizace.ToLower().StartsWith("evropský parlament")
                            && (e.DatumDo == null || e.DatumDo.Value.Year >= year)
                            && (e.DatumOd == null || e.DatumOd.Value.Year <= year));

                case Zatrideni.SefUradu:
                    return await GetByEventAsync(e =>
                            e.Ceo == 1
                            && (e.DatumDo == null || e.DatumDo.Value.Year >= year)
                            && (e.DatumOd == null || e.DatumOd.Value.Year <= year));

                case Zatrideni.Senator:
                    return await GetByEventAsync(e =>
                            e.Type == (int)OsobaEvent.Types.VolenaFunkce
                            && e.AddInfo.ToLower().StartsWith("senát")
                            && (e.Organizace.ToLower().StartsWith("senát")
                                || e.Ico.Trim() == UradyConstants.Ica.Senat)
                            && (e.DatumDo == null || e.DatumDo.Value.Year >= year)
                            && (e.DatumOd == null || e.DatumOd.Value.Year <= year));

                case Zatrideni.PoradcePredsedyVlady:
                    return await GetByEventAsync(e =>
                            e.Type == (int)OsobaEvent.Types.PolitickaExekutivni
                            && (e.AddInfo.ToLower().StartsWith("poradce předsedy vlády") ||
                                e.AddInfo.ToLower().StartsWith("poradkyně předsedy vlády"))
                            && (e.Organizace.ToLower().StartsWith("úřad vlády")
                                || e.Ico.Trim() == UradyConstants.Ica.UradVlady)
                            && (e.DatumDo == null || e.DatumDo.Value.Year >= year)
                            && (e.DatumOd == null || e.DatumOd.Value.Year <= year));
                
                case Zatrideni.KrajskyZastupitel:
                    return await GetByEventAsync(e =>
                            e.Type == (int)OsobaEvent.Types.VolenaFunkce
                            && e.AddInfo.ToLower().StartsWith("zastupitel")
                            && UradyConstants.Ica.Kraje.Contains(e.Ico.Trim())
                            && (e.DatumDo == null || e.DatumDo.Value.Year >= year)
                            && (e.DatumOd == null || e.DatumOd.Value.Year <= year));

                default:
                    throw new ArgumentOutOfRangeException(nameof(zatrideni), zatrideni, null);
            }
        }

    }
}