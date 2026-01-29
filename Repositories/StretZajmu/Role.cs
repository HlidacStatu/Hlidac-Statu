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
                if (_pravniInterval != null)
                    _pravniInterval = new Devmasters.DT.DateInterval(ZakonDatumParagraf > ZakonDatumLide ? ZakonDatumParagraf : ZakonDatumLide, null);
                return _pravniInterval;
            }
        }
        public List<OsobyDates> Osoby { get; set; } = new();

        public class OsobyDates
        {
            public Osoba Osoba { get; set; }
            public OsobaEvent Event { get; set; }
        }


        public static async Task<Role> FillRoleAsync(string sql, DateTime? zakonDatumLide, DateTime? zakonDatumParagraf)
        {

            IEnumerable<Tuple<string, long>> _o1 = await HlidacStatu.Connectors.DirectDB.Instance.GetListAsync<string, long>
            (
                sql, System.Data.CommandType.Text,
                new SqlParameter[] { new("@zakonDatumOd", zakonDatumLide > zakonDatumParagraf ? zakonDatumLide : zakonDatumParagraf) }
            );
            return await FillRoleAsync(_o1, zakonDatumLide, zakonDatumParagraf);
        }

        public static async Task<Role> FillRoleAsync(IEnumerable<Tuple<string, long>> osoby, DateTime? zakonDatumLide, DateTime? zakonDatumParagraf)
        {

            var r = new Role()
            {
                ZakonDatumLide = zakonDatumLide,
                ZakonDatumParagraf = zakonDatumParagraf,
            };
            foreach (var _oo in osoby)
            {
                r.Osoby.Add(new Role.OsobyDates()
                {
                    Osoba = await HlidacStatu.Repositories.Cache.OsobaCache.GetPersonByNameIdAsync(_oo.Item1),
                    Event = await HlidacStatu.Repositories.OsobaEventRepo.GetByIdAsync((int)_oo.Item2)
                });
            }

            return r;
        }
    }
}