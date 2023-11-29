using HlidacStatu.Repositories;

namespace HlidacStatu.Web.Framework
{

    //TODO
    //refactoring, move to InsolvenceRepo.IsLimitedAccess
    public static class InsolvenceLimitedView
    {
        public static bool IsLimited(System.Security.Principal.IPrincipal user, string[]? validRoles = null)
        {
            return InsolvenceRepo.IsLimitedAccess(user, validRoles);
        }
    }
}