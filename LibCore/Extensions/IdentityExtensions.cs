using System.Security.Claims;
using HlidacStatu.Entities;

namespace HlidacStatu.LibCore.Extensions
{
    public static class IdentityExtensions
    {
        public static string GetUserId(this ClaimsPrincipal principal) =>
            principal.FindFirstValue(ClaimTypes.NameIdentifier);

        public static bool HasEmailConfirmed(this ClaimsPrincipal user)
        {
            return ApplicationUser.GetByEmail(user.Identity?.Name).EmailConfirmed;
        }
    }
}