using HlidacStatu.Entities;
using HlidacStatu.LibCore.Extensions;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace HlidacStatu.LibCore.MiddleWares
{
    public class ApiAuthenticationMiddleware
    {
        private readonly RequestDelegate _next;

        public ApiAuthenticationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context != null)
            {
                var userName = context?.User?.Identity?.Name;
                if (string.IsNullOrEmpty(userName)
                    &&
                    (context.Request.Path.StartsWithSegments("/api") || context.Request.Path.StartsWithSegments("/health"))
                    )
                {
                    //var authToken = context.GetAuthTokenValue();
                    //authToken = authToken.Replace("Token ", "").Trim();

                    var authUser = context.GetAuthPrincipal();
                    if (authUser != null && authUser.Identity.IsAuthenticated)
                    {
                        context.User = authUser;

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