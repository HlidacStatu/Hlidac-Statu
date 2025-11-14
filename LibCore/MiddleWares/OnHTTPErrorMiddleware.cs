using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;
using Serilog;
using Microsoft.Extensions.Primitives;

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
                _logger.Error(e, "Unhadled exception {middleware} {path}?{query}\n", "OnHTTPErrorMidddleware",
                    httpContext.Request.Path, httpContext.Request.QueryString);

                throw;
            }

            if (httpContext.Response.StatusCode >= 500)
            {
                try
                {
                    var feature = httpContext.Features.Get<IExceptionHandlerFeature>();
                    var ex = feature?.Error;
                    
                    _logger.Error(ex,
                        "Unhadled exception > 500 {middleware} {path}?{query}\n",
                        "OnHTTPErrorMidddleware", httpContext.Request.Path, httpContext.Request.QueryString);
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
                _ = httpContext?.Request?.Headers?.TryGetValue("Referer", out referer);
                _logger.Warning("OnHTTPErrorMiddleware 400 HTTP error: {StatusCode} {Path} {QueryString} {Referer}", 
                    httpContext?.Response?.StatusCode,
                    httpContext?.Request?.Path.ToString(),
                    httpContext?.Request?.QueryString.ToString(),
                    referer.ToString()
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