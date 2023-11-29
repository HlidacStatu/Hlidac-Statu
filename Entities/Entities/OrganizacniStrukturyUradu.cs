using System;
using System.Collections.Generic;

namespace HlidacStatu.Entities
{
    public class OrganizacniStrukturyUradu
    {
        public Dictionary<string, List<OrgStrukturyStatu.JednotkaOrganizacni>> Urady { get; set; }
        public DateTime PlatneKDatu { get; set; }
    }
}
