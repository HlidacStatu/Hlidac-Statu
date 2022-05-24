using HlidacStatu.Entities;
using System.Linq;

namespace HlidacStatu.Extensions
{
    public static class WatchdogExtension
    {
        public static ApplicationUser User(this WatchDog watchDog)
        {
            using (DbEntities db = new DbEntities())
            {
                return db.Users.FirstOrDefault(m => m.EmailConfirmed && m.Id == watchDog.UserId);
            }
        }
    }
}