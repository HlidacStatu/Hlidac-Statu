using Microsoft.AspNetCore.Identity;

using System;
using System.Linq;

namespace HlidacStatu.Entities
{
    //[Table("AspNetUserApiTokens")]
    public partial class AspNetUserApiToken
    {
        public static AspNetUserApiToken CreateNew(IdentityUser user)
        {
            return CreateNew(user.Id);
        }
        public static AspNetUserApiToken CreateNew(string userId)
        {
            using (DbEntities db = new DbEntities())
            {

                var t = new AspNetUserApiToken() { Id = userId, Count = 0, Created = DateTime.Now, LastAccess = null, Token = Guid.NewGuid() };
                db.AspNetUserApiTokens.Add(t);
                db.SaveChanges();
                return t;
            }
        }

        public static AspNetUserApiToken GetToken(string username)
        {
            using (DbEntities db = new DbEntities())
            {

                var user = db.Users.AsQueryable()
                    .Where(m => m.UserName == username)
                    .FirstOrDefault();
                if (user == null)
                    return CreateNew(user.Id);

                var token = db.AspNetUserApiTokens.AsQueryable()
                    .Where(m => m.Id == user.Id)
                    .FirstOrDefault();
                if (token == null)
                    return CreateNew(user.Id);
                else
                    return token;
            }

        }

    }
}
