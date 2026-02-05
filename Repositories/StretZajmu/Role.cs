using HlidacStatu.Entities;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HlidacStatu.Repositories.StretZajmu
{
    public class Role
    {
        public DateTime? ZakonDatumLide { get; set; }
        public DateTime? ZakonDatumParagraf { get; set; }

        Devmasters.DT.DateInterval _pravniInterval = null;
        public Devmasters.DT.DateInterval PravniInterval
        {
            get
            {
                if (_pravniInterval == null)
                {
                    if (ZakonDatumLide == null && ZakonDatumParagraf == null)
                        _pravniInterval = new Devmasters.DT.DateInterval((DateOnly?)null, (DateOnly?)null);
                    else if (ZakonDatumLide == null)
                        _pravniInterval = new Devmasters.DT.DateInterval(ZakonDatumParagraf, null);
                    else if (ZakonDatumParagraf == null)
                        _pravniInterval = new Devmasters.DT.DateInterval(ZakonDatumLide, null);
                    else
                        _pravniInterval = new Devmasters.DT.DateInterval(ZakonDatumParagraf > ZakonDatumLide ? ZakonDatumParagraf : ZakonDatumLide, null);
                }
                return _pravniInterval;
            }
        }
        public List<Osoba_With_Event> Osoby { get; set; } = new();

        public class Osoba_With_Event
        {
            public Osoba Osoba { get; set; }
            public OsobaEvent Event { get; set; }
        }


        public static async Task<Role> FillRoleAsync(string sql, DateTime? zakonDatumLide, DateTime? zakonDatumParagraf)
        {

            IEnumerable<Tuple<string, int>> _o1 = await HlidacStatu.Connectors.DirectDB.Instance.GetListAsync<string, int>
            (
                sql, System.Data.CommandType.Text,
                new SqlParameter[] { new("@zakonDatumOd", zakonDatumLide > zakonDatumParagraf ? zakonDatumLide : zakonDatumParagraf) }
            );
            return await FillRoleAsync(_o1, zakonDatumLide, zakonDatumParagraf);
        }

        public static async Task<Role> FillRoleAsync(IEnumerable<Tuple<string, int>> osoby, DateTime? zakonDatumLide, DateTime? zakonDatumParagraf)
        {

            var r = new Role()
            {
                ZakonDatumLide = zakonDatumLide,
                ZakonDatumParagraf = zakonDatumParagraf,
            };
            foreach (var _oo in osoby)
            {
                r.Osoby.Add(new Role.Osoba_With_Event()
                {
                    Osoba = await HlidacStatu.Repositories.Cache.OsobaCache.GetPersonByNameIdAsync(_oo.Item1),
                    Event = await HlidacStatu.Repositories.OsobaEventRepo.GetByIdAsync((int)_oo.Item2)
                });
            }

            return r;
        }
    }
}