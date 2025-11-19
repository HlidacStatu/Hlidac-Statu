using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;
using Serilog;

namespace HlidacStatu.LibCore.MiddleWares
{
    public class OnHTTPErrorMiddleware
    {
        private readonly RequestDelegate _next;

        private readonly ILogger _logger = Log.ForContext<OnHTTPErrorMiddleware>();

        public OnHTTPErrorMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception e)
            {
                Helpers.LogHttpRequestDetail(_logger, Serilog.Events.LogEventLevel.Error, httpContext, e, 
                    "Untreated exception", nameof(OnHTTPErrorMiddleware));
            }

            if (httpContext.Response.StatusCode >= 400)
            {
                Helpers.LogHttpRequestDetail(_logger, Serilog.Events.LogEventLevel.Warning, httpContext, null, "Not found", nameof(OnHTTPErrorMiddleware));
            }
        }
    }

    public static class OnHTTPErrorMiddlewareExtension
    {
        public static IApplicationBuilder UseOnHTTPErrorMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<OnHTTPErrorMiddleware>();
        }
    }
}