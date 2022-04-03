using Devmasters.Enums;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HlidacStatu.Entities
{
    public partial class CenyCustomer
    {
        public class AccessDetail
        {
            public static AccessDetail NoAccess() { return new AccessDetail(AccessDetailLevel.NONE); }

            private AccessDetail() { }

            [ShowNiceDisplayName()]
            public enum AccessDetailLevel
            {
                [NiceDisplayName("Nemá přístup")]
                NONE = 0,
                [NiceDisplayName("BASIC")]
                BASIC = 1,
                [NiceDisplayName("PRO")]
                PRO = 2
            }
            public AccessDetail(AccessDetailLevel level)
            {
                AnalyzeLevel = level;
                if (level != AccessDetailLevel.NONE)
                    Access = true;
            }

            public bool Access { get; set; } = false;
            public AccessDetailLevel AnalyzeLevel { get; set; } = AccessDetailLevel.NONE;
            public string AnalyzeLevelText
            {
                get
                {
                    switch (AnalyzeLevel)
                    {
                        case AccessDetailLevel.BASIC:
                        case AccessDetailLevel.PRO:
                        case AccessDetailLevel.NONE:
                            return AnalyzeLevel.ToNiceDisplayName();
                        default:
                            return "Nemá přístup";
                    }
                }
            }
        }

    }
}
