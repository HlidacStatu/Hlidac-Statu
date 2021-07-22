using System.Security.Claims;
using HlidacStatu.Entities;
using Microsoft.AspNetCore.Http;

namespace HlidacStatu.Web.Framework
{
    public static class OtherExtensions
    {
        public static string GetUserId(this ClaimsPrincipal principal) =>
            principal.FindFirstValue(ClaimTypes.NameIdentifier);

        public static bool HasEmailConfirmed(this ClaimsPrincipal user)
        {
            return ApplicationUser.GetByEmail(user.Identity?.Name).EmailConfirmed;
        }

        public static string GetRemoteIp(this HttpContext httpContext)
        {
            return httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown IP";
        }

        public static string GetAuthToken(this HttpContext httpContext)
        {
            if (httpContext.Request.Headers.ContainsKey("Authorization"))
            {
                return httpContext.Request.Headers["Authorization"];
            }
            else if (httpContext.Request.Query.ContainsKey("Authorization"))
            {
                return httpContext.Request.Query["Authorization"];
            }

            return "";
        }
    }
}