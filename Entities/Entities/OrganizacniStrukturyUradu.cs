using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HlidacStatu.Entities
{
    public class OrganizacniStrukturyUradu
    {
        public Dictionary<string, List<OrgStrukturyStatu.JednotkaOrganizacni>> Urady { get; set; }
        public DateTime PlatneKDatu { get; set; }
    }
}
