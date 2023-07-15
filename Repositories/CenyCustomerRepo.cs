using HlidacStatu.Entities;

using Microsoft.EntityFrameworkCore;
using System.Linq;
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
                var retAll = await db.CenyCustomer.AsQueryable()
                    .Where(m => m.Username == username && m.Analyza == analyza && m.Rok == rok && m.Paid.HasValue)
                    .ToListAsync();

                if (retAll == null || retAll.Count()==0)
                    return CenyCustomer.AccessDetail.NoAccess();
                else
                {
                    //TODO zmenit podle levelu
                    if (retAll.Max(m=>m.Level) == 1)
                        return new CenyCustomer.AccessDetail(CenyCustomer.AccessDetail.AccessDetailLevel.BASIC);
                    else if (retAll.Max(m => m.Level) == 2)
                        return new CenyCustomer.AccessDetail(CenyCustomer.AccessDetail.AccessDetailLevel.PRO );
                    else
                        return CenyCustomer.AccessDetail.NoAccess();

                }
            }
        }



    }
}