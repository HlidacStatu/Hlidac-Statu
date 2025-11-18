using HlidacStatu.Util;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Serilog;
using System;

namespace HlidacStatu.LibCore.MiddleWares
{
    public static class Helpers
    {
        public static void LogHttpRequestDetail(ILogger? logger, Serilog.Events.LogEventLevel level,
            HttpContext? httpContext, Exception? exeption,
            string prefix, string middleware)
        {
            if (logger == null)
                return;

            if (httpContext == null)
                return;

            StringValues referer = default;
            StringValues userAgent = default;
            _ = httpContext?.Request?.Headers?.TryGetValue("Referer", out referer);
            _ = httpContext?.Request?.Headers?.TryGetValue("User-Agent", out userAgent);
            string ipAddress = string.Empty;
            string wedosIpAddress = string.Empty;
            string? path = string.Empty;
            string? queryString = string.Empty;
            int? statusCode = null;
            try
            {
                path = httpContext?.Request?.Path.ToString();
                statusCode = httpContext?.Response?.StatusCode;
                ipAddress = RealIpAddress.GetIp(httpContext).ToString();
                wedosIpAddress = RealIpAddress.IpFromVedos(httpContext).ToString();
                queryString = httpContext?.Request?.QueryString.ToString();
            }
            catch
            {
                //sanitizing inputs
            }


            if (exeption != null)
                logger.Write(level,
                    exeption,
                    "{messagePrefix} {StatusCode}: ip:{IP}\tcdn:{fromCDN}\t{Path}{QueryString}\tref:{Referer} useragent:{UserAgent} {logSource}",
                    prefix,
                    statusCode,
                    ipAddress,
                    wedosIpAddress,
                    path,
                    queryString,
                    referer.ToString(),
                    userAgent.ToString(),
                    middleware
                );
            else
                logger.Write(level,
                    "{messagePrefix} {StatusCode}: ip:{IP}\tcdn:{fromCDN}\t{Path}{QueryString}\tref:{Referer} useragent:{UserAgent} {logSource}",
                    prefix,
                    statusCode,
                    ipAddress,
                    wedosIpAddress,
                    path,
                    queryString,
                    referer.ToString(),
                    userAgent.ToString(),
                    middleware
                );
        }
    }
}