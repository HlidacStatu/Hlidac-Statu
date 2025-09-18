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
                var str = Devmasters.Net.WebContextLogger.LogFatalWebError(e, httpContext, true, "");

                if (httpContext.Items.ContainsKey(ItemKeyName))
                {
                    var prevStr = httpContext.Items[ItemKeyName] as string;
                    httpContext.Items[ItemKeyName] = prevStr + "\n\n====== NextException =======" + str;
                }
                else
                    httpContext.Items.Add(ItemKeyName, str);

                httpContext.Items[ItemKeyRefererName] = httpContext?.Request?.Headers?.ContainsKey("Referer") == true
                    ? httpContext.Request.Headers["Referer"].ToString()
                    : null;

                _logger.Error(e, "Unhadled exception {middleware} {path}?{query}\n" + str, "OnHTTPErrorMidddleware",
                    httpContext.Request.Path, httpContext.Request.QueryString);

                throw;
            }

            if (httpContext.Response.StatusCode >= 500)
            {
                try
                {
                    var exceptionHandlerPathFeature = httpContext.Features.Get<IExceptionHandlerPathFeature>();
                    var str = Devmasters.Net.WebContextLogger.LogFatalWebError(exceptionHandlerPathFeature?.Error,
                        httpContext, true, "");
                    if (httpContext.Items.ContainsKey(ItemKeyName))
                    {
                        var prevStr = httpContext.Items[ItemKeyName] as string;
                        httpContext.Items[ItemKeyName] = prevStr + "\n\n====== NextException =======" + str;
                    }
                    else
                        httpContext.Items.Add(ItemKeyName, str);

                    httpContext.Items[ItemKeyRefererName] = httpContext?.Request?.Headers?.ContainsKey("Referer") == true
                        ? httpContext.Request.Headers["Referer"].ToString()
                        : null;

                    _logger.Error(
                        "Unhadled exception > 500 {middleware} {path}?{query}\n" + httpContext.Items[ItemKeyName],
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
                try
                {
                    var str = Devmasters.Net.WebContextLogger.Log404WebError(httpContext?.Request);
                    if (httpContext.Items.ContainsKey(ItemKeyName))
                    {
                        var prevStr = httpContext.Items[ItemKeyName] as string;
                        httpContext.Items[ItemKeyName] = prevStr + "\n\n====== Next404 =======" + str;
                    }
                    else
                        httpContext.Items.Add(ItemKeyName, str);

                    httpContext.Items[ItemKeyRefererName] = httpContext?.Request?.Headers?.ContainsKey("Referer") == true
                        ? httpContext.Request.Headers["Referer"].ToString()
                        : null;

                    httpContext.Items[ItemKeyNameObj] =
                        Devmasters.Net.WebContextLogger.Get404ErrorContextInfo(httpContext);
                }
                catch (Exception e)
                {
                    _logger.Error(e,
                        $"OnHTTPErrorMiddleware Invoke exc: {httpContext.Response.StatusCode} {httpContext.Request.Path.ToString()}?{httpContext.Request.QueryString.ToString()}");
                }
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