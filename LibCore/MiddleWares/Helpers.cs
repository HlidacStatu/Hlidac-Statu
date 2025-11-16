using Elastic.CommonSchema;
using HlidacStatu.Util;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

namespace HlidacStatu.LibCore.MiddleWares
{
    public static class Helpers
    {
        public static void LogHttpRequestDetail(ILogger _logger, Serilog.Events.LogEventLevel level,
            HttpContext httpContext, Exception ex,
            string prefix, string middleware)
        {
            if (_logger == null)
                return;
            if (httpContext == null)
                throw new NullReferenceException(nameof(httpContext));

            prefix = prefix ?? "HTTP";

            StringValues referer = default;
            StringValues userAgent = default;
            _ = httpContext?.Request?.Headers?.TryGetValue("Referer", out referer);
            _ = httpContext?.Request?.Headers?.TryGetValue("User-Agent", out userAgent);

            if (ex != null)
                _logger.Write(level, ex, prefix + " {StatusCode}: ip:{IP}\tcdn:{fromCDN}\t{Path}{QueryString}\tref:{Referer} useragent:{UserAgent} {middleware}",
                     httpContext?.Response?.StatusCode,
                     RealIpAddress.GetIp(httpContext),
                     RealIpAddress.IpFromVedos(httpContext),
                     httpContext?.Request?.Path.ToString(),
                     httpContext?.Request?.QueryString.ToString(),
                     referer.ToString(),
                     userAgent.ToString(),
                     middleware
                     );
            else
                _logger.Write(level, prefix + " {StatusCode}: ip:{IP}\t{Path}{QueryString}\tref:{Referer} useragent:{UserAgent} {middleware}",
                    httpContext?.Response?.StatusCode,
                    RealIpAddress.GetIp(httpContext),
                     RealIpAddress.IpFromVedos(httpContext),
                    httpContext?.Request?.Path.ToString(),
                    httpContext?.Request?.QueryString.ToString(),
                    referer.ToString(),
                    userAgent.ToString(),
                    middleware
                    );
        }
    }
}
