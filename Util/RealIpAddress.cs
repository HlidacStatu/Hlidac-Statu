using Microsoft.AspNetCore.Http;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace HlidacStatu.Util
{
    public static class RealIpAddress
    {
        public static IPAddress GetIp(HttpContext httpContext)
        {
            //if on Radware, use header X-Forwarded-For

            if (httpContext?.Request?.Headers == null)
                return null;

            IPAddress? remoteIp = null;
            IPAddress.TryParse(httpContext.Request.Headers["X-Forwarded-For"], out remoteIp);

            if (remoteIp == null)
            {
                remoteIp = httpContext.Connection?.RemoteIpAddress;
            }
            return remoteIp;
        }

    }
}
