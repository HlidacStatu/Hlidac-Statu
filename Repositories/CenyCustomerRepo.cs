using HlidacStatu.Entities;

using Microsoft.EntityFrameworkCore;
using Devmasters.Enums;
using System.Linq;
using System.Threading.Tasks;


namespace HlidacStatu.Repositories
{
    public partial class CenyCustomerRepo
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

        public static async Task<AccessDetail> HasAccessAsync(string username, string analyza, int rok)
        {
            username = username?.Trim()?.ToLower();
            analyza = analyza?.Trim()?.ToUpper();

            if (analyza == "DEMO" && rok == 2018)
                return new AccessDetail( AccessDetail.AccessDetailLevel.PRO);

            using (var db = new DbEntities())
            {
                var ret = await db.CenyCustomer.AsQueryable()
                    .FirstOrDefaultAsync(m => m.Username == username && m.Analyza == analyza && m.Rok == rok && m.Paid.HasValue);
                if (ret == null)
                    return AccessDetail.NoAccess();
                else
                {
                    //TODO zmenit podle castky
                    return new AccessDetail(AccessDetail.AccessDetailLevel.PRO );

                }
            }
        }



    }
}