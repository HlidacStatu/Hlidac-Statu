using System.Collections.Generic;

namespace HlidacStatu.Entities
{
    public partial class Firma
    {
        public partial class Statistics
        {
            public partial class VZ : Lib.Analytics.StatisticsSubjectPerYear<VZ.Data>
            {
                public static VZ NullObj = new VZ() { ICO = "--------" };



                public VZ() : base() { }

                public VZ(string ico, Dictionary<int, Data> data)
                    : base(ico, data)
                {

                }

            }
        }
    }
}
