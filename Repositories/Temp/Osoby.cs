using HlidacStatu.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

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
        
    }
}
