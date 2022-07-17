using Devmasters.Log;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

using System;
using System.Threading.Tasks;

namespace HlidacStatu.LibCore.MiddleWares
{
    public class OnHTTPErrorMiddleware
    {
        public const string ItemKeyName = "errorPageCtx";
        private readonly RequestDelegate _next;
        private readonly Logger logger;

        public OnHTTPErrorMiddleware(RequestDelegate next, Devmasters.Log.Logger logger)
        {
            _next = next;
            logger = logger ?? HlidacStatu.Util.Consts.Logger;
            this.logger = logger;
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

                logger.Error("Unhadled exception {middleware} {path}?{query}\n"+str,e, "OnHTTPErrorMidddleware",httpContext.Request.Path, httpContext.Request.QueryString);

                throw;
            }

            if (httpContext.Response.StatusCode >= 500)
            {
                try
                {
                    var exceptionHandlerPathFeature = httpContext.Features.Get<IExceptionHandlerPathFeature>();
                    var str = Devmasters.Net.WebContextLogger.LogFatalWebError(exceptionHandlerPathFeature?.Error, httpContext, true, "");
                    if (httpContext.Items.ContainsKey(ItemKeyName))
                    {
                        var prevStr = httpContext.Items[ItemKeyName] as string;
                        httpContext.Items[ItemKeyName] = prevStr + "\n\n====== NextException =======" + str;
                    }
                    else
                        httpContext.Items.Add(ItemKeyName, str);
                    logger.Error("Unhadled exception OnHTTPErrorMidddleware.");
                }
                catch (Exception e)
                {
                    HlidacStatu.Util.Consts.Logger.Fatal(
                        $"OnHTTPErrorMiddleware Invoke exc: {httpContext.Response.StatusCode} {httpContext.Request.Path}?{httpContext.Request.QueryString.ToString()}", e);
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

                }
                catch (Exception e)
                {
                    HlidacStatu.Util.Consts.Logger.Fatal(
                        $"OnHTTPErrorMiddleware Invoke exc: {httpContext.Response.StatusCode} {httpContext.Request.Path.ToString()}?{httpContext.Request.QueryString.ToString()}", e);
                }
            }
            return;
        }
    }

    public static class OnHTTPErrorMiddlewareExtension
    {
        public static IApplicationBuilder UseOnHTTPErrorMiddleware(this IApplicationBuilder builder, Devmasters.Log.Logger logger = null)
        {
            return builder.UseMiddleware<OnHTTPErrorMiddleware>(logger);
        }
    }


}
