using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HlidacStatu.DS.Api
{
    public class DumpedData
    {
        public DumpedData() { }
        public DumpedData(string id, DateTime lastupdate) { this.id = id; this.lastUpdate = lastupdate; }
        public string id { get; set; }
        public DateTime lastUpdate { get; set; }
    }
}
