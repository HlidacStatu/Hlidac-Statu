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
                _logger.Error(e, "Total unhadled exception: {path}?{query}\tmiddleware:{middleware}\nException:{exception}", 
                    httpContext.Request.Path, httpContext.Request.QueryString,
                    typeof(OnHTTPErrorMiddleware).Name, e.ToString());

                throw;
            }

            if (httpContext.Response.StatusCode >= 500)
            {
                try
                {
                    var feature = httpContext.Features.Get<IExceptionHandlerFeature>();
                    Exception? ex = feature?.Error;
                    
                    _logger.Error(ex,
                        "Unhadled exception: {path}?{query}\tmiddleware:{middleware}\nException:{exception}",
                        httpContext.Request.Path, 
                        httpContext.Request.QueryString, 
                        typeof(OnHTTPErrorMiddleware).Name,
                        ex?.ToString());
                }
                catch (Exception e)
                {
                    _logger.Fatal(e,
                        $"OnHTTPErrorMiddleware Invoke exc: {httpContext.Response.StatusCode} {httpContext.Request.Path}?{httpContext.Request.QueryString.ToString()}");
                }
            }
            else if (httpContext.Response.StatusCode >= 400)
            {
                StringValues referer = default;
                StringValues userAgent = default;
                _ = httpContext?.Request?.Headers?.TryGetValue("Referer", out referer);
                _ = httpContext?.Request?.Headers?.TryGetValue("User-Agent", out userAgent);


                _logger.Warning("HTTP {StatusCode}: ip:{IP}\t{Path}{QueryString}\tref:{Referer} useragent:{UserAgent} {middleware}", 
                    RealIpAddress.GetIp(httpContext),
                    httpContext?.Response?.StatusCode,
                    httpContext?.Request?.Path.ToString(),
                    httpContext?.Request?.QueryString.ToString(),
                    referer.ToString(),
                    userAgent.ToString(),
                    typeof(OnHTTPErrorMiddleware).Name
                    );
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