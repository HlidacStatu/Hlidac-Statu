using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HlidacStatu.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public static ApplicationUser GetByEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
                return null;
            using (DbEntities db = new DbEntities())
            {
                var u = db.Users.AsNoTracking().FirstOrDefault(m => m.Email == email);
                return u;
            }
        }

        public string[] GetRoles()
        {
            using var db = new DbEntities();

            var roles = db.Roles.FromSqlInterpolated(
                    $"select ro.* from AspNetRoles ro join AspNetUserRoles ur on ro.Id = ur.RoleId where ur.UserId = {Id}")
                .AsNoTracking()
                .Select(r => r.Name)
                .ToArray();

            return roles;
        }

        public string GetAPIToken()
        {
            return AspNetUserApiToken.GetToken(Email).Token.ToString("N");
        }

        public bool IsInRole(string role)
        {
            using (DbEntities db = new DbEntities())
            {
                return db.Roles
                    .FromSqlInterpolated(
                        $"select ro.* from AspNetRoles ro join AspNetUserRoles ur on ro.Id = ur.RoleId where ur.UserId = {Id}")
                    .Any(r => r.Name.ToLower() == role.ToLower());
            }
        }


        object _watchdogAllInOneLock = new object();
        private WatchdogAllInOne _watchdogAllInOne = null;

        [NotMapped]
        public bool SentWatchdogOneByOne
        {
            get
            {
                InitWatchdogAllInOne();
                return _watchdogAllInOne.GetValue();
            }
            set
            {
                InitWatchdogAllInOne();
                _watchdogAllInOne.SetValue(value);
                _watchdogAllInOne.Save();
            }
        }

        private void InitWatchdogAllInOne()
        {
            if (_watchdogAllInOne == null)
            {
                lock (_watchdogAllInOneLock)
                {
                    if (_watchdogAllInOne == null)
                        _watchdogAllInOne = new WatchdogAllInOne(this);
                }
            }
        }

        public class WatchdogAllInOne : UserOptions<bool>
        {
            public WatchdogAllInOne(ApplicationUser user)
                : base(user, ParameterType.WatchdogAllInOne, null)
            {
            }

            protected override string SerializeToString(bool value)
            {
                return value ? "1" : "0";
            }

            protected override bool DeserializeFromString(string value)
            {
                if (value == null)
                    return false;

                if (value == "1")
                    return true;
                else
                    return false;
            }
        }
    }
}