using HlidacStatu.Entities;


using Microsoft.EntityFrameworkCore;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HlidacStatu.Repositories
{

    public class Osoby
    {
        public static volatile Devmasters.Cache.LocalMemory.Manager<IEnumerable<OsobaEvent>, int> CachedEvents
            = Devmasters.Cache.LocalMemory.Manager<IEnumerable<OsobaEvent>, int>
                .GetSafeInstance("osobyEvents",
                osobaInternalId =>
                {
                    using (DbEntities db = new DbEntities())
                    {
                        return db.OsobaEvent
                            .AsNoTracking()
                            .Where(m => m.OsobaId == osobaInternalId)
                            .ToArray();
                    }
                },
                TimeSpan.FromMinutes(2));

        public static volatile Devmasters.Cache.LocalMemory.Manager<IEnumerable<Sponzoring>, int> CachedFirmySponzoring
            = Devmasters.Cache.LocalMemory.Manager<IEnumerable<Sponzoring>, int>
                .GetSafeInstance("osobyFirmySponzoring",
                osobaInternalId =>
                {
                    using (DbEntities db = new DbEntities())
                    {
                        var res = db.Sponzoring.FromSqlRaw($@"
                            select fe.* from Sponzoring fe with (nolock)
	                          join osobaVazby ov with (nolock) on ov.vazbakico=fe.IcoDarce 
                               and dbo.IsSomehowInInterval(fe.DarovanoDne,fe.DarovanoDne, ov.datumOd, ov.DatumDo)=1
                            and osobaid={osobaInternalId}")
                            .AsNoTracking()
                            .ToList();

                        var res1 = res.Select(m => new Sponzoring()
                        {
                            OsobaIdDarce = osobaInternalId,
                            IcoDarce = m.IcoDarce,
                            OsobaIdPrijemce = m.OsobaIdPrijemce,
                            UpdatedBy = m.UpdatedBy,
                            IcoPrijemce = m.IcoPrijemce,
                            Hodnota = m.Hodnota,
                            Created = m.Created,
                            Edited = m.Edited,
                            DarovanoDne = m.DarovanoDne,
                            Typ = (int)Sponzoring.TypDaru.DarFirmy,
                            Popis = m.Popis,
                            Zdroj = m.Zdroj
                        })
                        .ToArray();
                        return res1;
                    }
                },
                TimeSpan.FromMinutes(2));




        static Osoba nullObj = new Osoba() { NameId = "____NOTHING____" };
        private class OsobyMCMById : Devmasters.Cache.Memcached.Manager<Osoba, int>
        {
            public OsobyMCMById() : base("PersonById", GetByIdAsync, TimeSpan.FromMinutes(10),
                    Devmasters.Config.GetWebConfigValue("HazelcastServers").Split(','))
            { }

            public override Osoba Get(int key)
            {
                var o = base.Get(key);
                if (o.NameId == nullObj.NameId)
                    return null;
                else
                    return o;
            }
            private static async Task<Osoba> GetByIdAsync(int key)
            {
                var o = await OsobaRepo.GetByInternalIdAsync(key);
                return o ?? nullObj;
            }

        }


        private static object lockObj = new object();

        private static OsobyMCMById instanceById;
        public static Devmasters.Cache.Memcached.Manager<Osoba, int> GetById
        {
            get
            {
                if (instanceById == null)
                {
                    lock (lockObj)
                    {
                        if (instanceById == null)
                        {
                            instanceById = new OsobyMCMById();
                        }
                    }
                }
                return instanceById;
            }
        }
    }
}
