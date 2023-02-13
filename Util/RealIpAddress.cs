using Microsoft.AspNetCore.Http;
using System.Net;

namespace HlidacStatu.Util
{
    public static class RealIpAddress
    {
        public static IPAddress GetIp(HttpContext httpContext)
        {
            //if on Radware, use header X-Forwarded-For

            if (httpContext?.Request?.Headers == null)
                return null;

            IPAddress.TryParse(httpContext.Request.Headers["X-Forwarded-For"], out var remoteIp);

            return remoteIp ?? httpContext.Connection?.RemoteIpAddress;
        }

    }
}
