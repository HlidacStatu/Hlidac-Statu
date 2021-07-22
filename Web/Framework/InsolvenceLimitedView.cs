using System.Linq;

namespace HlidacStatu.Web.Framework
{
    public static class InsolvenceLimitedView
    {
        static string[] defaultRolesWithoutLimitation = new string[] { "Admin", "novinar" };

        public static bool IsLimited(System.Security.Principal.IPrincipal user, string[]? validRoles = null)
        {
            validRoles ??= defaultRolesWithoutLimitation;
            if (user?.Identity?.IsAuthenticated == true)
            {
                if (validRoles.Count() == 0)
                    return false;

                foreach (var role in validRoles)
                {
                    if (user.IsInRole(role.Trim()))
                        return false;
                }
                return true;
            }
            return true;

        }
    }
}