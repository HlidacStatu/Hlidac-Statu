using HlidacStatu.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Nest;
using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Tiktoken;

namespace HlidacStatu.LibCore.Extensions
{
    public static class HttpContextExtensions
    {
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
        public static string GetAuthTokenValue(this HttpContext httpContext)
        {
            var val = httpContext.GetAuthToken();
            val = val?.Replace("Bearer ", "").Replace("Token ", "").Trim();
            return val ?? "";
        }

        public static ApplicationUser GetAuthUser(this HttpContext httpContext)
        {
            ApplicationUser? user = null;
            string authToken = httpContext.GetAuthTokenValue();
            if (!string.IsNullOrEmpty(authToken) && Guid.TryParse(authToken, out var guid))
            {
                using (DbEntities db = new())
                {

                    user = db.Users.FromSqlInterpolated(
                            $"select u.* from AspNetUsers u join AspNetUserApiTokens a on u.Id = a.Id where a.Token = {guid}")
                        .AsNoTracking()
                        .FirstOrDefault();

                }
            }
            return user;
        }

        public static ClaimsPrincipal GetAuthPrincipal(this HttpContext httpContext)
        {
            ClaimsPrincipal claimPrincipal = null;

            ApplicationUser user = GetAuthUser(httpContext);

            if (user != null)
            {
                var roles = user.GetRoles();

                var claims = new List<Claim> {
                                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                                new(ClaimTypes.Name, user.UserName),
                                new(ClaimTypes.Email, user.Email),
                            };

                foreach (var role in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }

                var identity = new ClaimsIdentity(claims, "Api");
                var principal = new ClaimsPrincipal(identity);
                claimPrincipal = principal;

                // var ticket = new AuthenticationTicket(principal, Scheme.Name);
                // context.User.AddIdentity(identity);
                // context.User
            }
            return claimPrincipal;
        }
        public static string GetRemoteIp(this HttpContext httpContext)
        {
            return HlidacStatu.Util.RealIpAddress.GetIp(httpContext)?.ToString() ?? "unknown IP";
        }
    }
}