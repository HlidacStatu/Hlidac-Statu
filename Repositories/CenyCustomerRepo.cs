using HlidacStatu.Entities;

using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace HlidacStatu.Repositories
{
    public partial class CenyCustomerRepo
    {

        public static async Task<CenyCustomer.AccessDetail> HasAccessAsync(string username, string analyza, int rok)
        {
            username = username?.Trim()?.ToLower();
            analyza = analyza?.Trim()?.ToUpper();

            if (analyza == "DEMO" && rok == 2018)
                return new CenyCustomer.AccessDetail( CenyCustomer.AccessDetail.AccessDetailLevel.PRO);

            using (var db = new DbEntities())
            {
                var ret = await db.CenyCustomer.AsQueryable()
                    .FirstOrDefaultAsync(m => m.Username == username && m.Analyza == analyza && m.Rok == rok && m.Paid.HasValue);
                if (ret == null)
                    return CenyCustomer.AccessDetail.NoAccess();
                else
                {
                    //TODO zmenit podle castky
                    return new CenyCustomer.AccessDetail(CenyCustomer.AccessDetail.AccessDetailLevel.PRO );

                }
            }
        }



    }
}