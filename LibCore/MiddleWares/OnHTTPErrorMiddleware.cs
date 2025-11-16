using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;
using Serilog;
using Microsoft.Extensions.Primitives;
using HlidacStatu.Util;

namespace HlidacStatu.LibCore.MiddleWares
{
    public class OnHTTPErrorMiddleware
    {
        public const string ItemKeyName = "errorPageCtx";
        public const string ItemKeyNameObj = "errorPageCtxObj";
        public const string ItemKeyRefererName = "referrerUrl";

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
                Helpers.LogHttpRequestDetail(_logger, Serilog.Events.LogEventLevel.Fatal, httpContext, e, 
                    "Total unhadled exception", typeof(OnHTTPErrorMiddleware).Name);

                throw;
            }

            if (httpContext.Response.StatusCode >= 500)
            {
                try
                {
                    var feature = httpContext.Features.Get<IExceptionHandlerFeature>();
                    Exception? ex = feature?.Error;

                    Helpers.LogHttpRequestDetail(_logger, Serilog.Events.LogEventLevel.Fatal, httpContext, ex, "Unhadled exception", typeof(OnHTTPErrorMiddleware).Name);
                }
                catch (Exception e)
                {
                    Helpers.LogHttpRequestDetail(_logger, Serilog.Events.LogEventLevel.Fatal, httpContext, e, "OnHTTPErrorMiddleware Invoke exc", typeof(OnHTTPErrorMiddleware).Name);
                }
            }
            else if (httpContext.Response.StatusCode >= 400)
            {
                Helpers.LogHttpRequestDetail(_logger, Serilog.Events.LogEventLevel.Warning, httpContext, null, null, typeof(OnHTTPErrorMiddleware).Name);
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