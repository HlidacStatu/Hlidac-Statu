using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using HlidacStatu.Entities;
using HlidacStatu.LibCore.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace HlidacStatu.LibCore.MiddleWares
{
    public class ApiAuthenticationMiddleware
    {
        private readonly RequestDelegate _next;

        public ApiAuthenticationMiddleware(RequestDelegate next)
        {
            _next = next;
        }
        
        public async Task Invoke(HttpContext context)
        {
            var userName = context.User.Identity.Name;
                if (string.IsNullOrEmpty(userName) && context.Request.Path.StartsWithSegments("/api"))
                {
                    var authToken = context.GetAuthToken(); 
                    authToken = authToken.Replace("Token ", "").Trim();

                    ApplicationUser user = null;
                
                    if (!string.IsNullOrEmpty(authToken) && Guid.TryParse(authToken, out var guid))
                    {
                        using (DbEntities db = new())
                        {
                            
                            user = await db.Users.FromSqlInterpolated(
                                    $"select u.* from AspNetUsers u join AspNetUserApiTokens a on u.Id = a.Id where a.Token = {guid}")
                                .AsQueryable()
                                .FirstOrDefaultAsync();
                            
                        }

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
                            context.User = principal;
                            
                            // var ticket = new AuthenticationTicket(principal, Scheme.Name);
                            // context.User.AddIdentity(identity);
                            // context.User
                        }
                    }
                }
                
                await _next(context);
        }
       
    }

    public static class ApiAuthenticationMiddlewareExtension
    {
        public static IApplicationBuilder UseApiAuthenticationMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ApiAuthenticationMiddleware>();
        }
    }

    
}