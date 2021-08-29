using System;
using System.Collections.Generic;

namespace HlidacStatu.Web.Models
{
    public class ApiV1Models
    {
        public class DumpInfoModel
        {
            public string url { get; set; }
            public DateTime? date { get; set; }
            public long size { get; set; }
            public bool fulldump { get; set; }
            public DateTime created { get; set; }
            public string dataType { get; set; }
        }

        public class ClassificatioListItemModel
        {
            public Contract[] contracts { get; set; }

            public class Contract
            {
                public string contractId { get; set; }
                public string url { get; set; }
            }
        }

        public class PolitikTypeAhead : IEqualityComparer<PolitikTypeAhead>
        {
            public string name; public string nameId;

            public bool Equals(PolitikTypeAhead x, PolitikTypeAhead y)
            {
                if (x == null && y == null)
                    return true;
                if ((x == null && y != null) || (x != null && y == null))
                    return false;
                return (x.nameId == y.nameId && x.name == y.name);
            }

            public int GetHashCode(PolitikTypeAhead obj)
            {
                return obj.GetHashCode();
            }
        }

    }
}