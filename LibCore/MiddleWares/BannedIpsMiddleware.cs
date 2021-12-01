using HlidacStatu.LibCore.Services;
using HlidacStatu.Repositories;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace HlidacStatu.LibCore.MiddleWares
{
    public class BannedIpsMiddleware
    {
        private class RadwareNetwork : Devmasters.Net.Crawlers.CrawlerBase
        {
            public override string Name => "RadwareNetwork";

            public override string[] IP => new string[] {
                "141.226.101.0/24",
                "66.22.0.0/17",
                "159.122.76.110",
                "141.226.97.0/24"
            };

            public override string[] HostName => null;

            public override string[] UserAgent => null;
        }

        static Devmasters.Net.Crawlers.ICrawler radware = new RadwareNetwork();

        private readonly RequestDelegate _next;

        private readonly string[] _badWords = new[]
        {
            "wp-login.php"
        };

        public BannedIpsMiddleware(RequestDelegate next)
        {
            _next = next;
        }


        // IMyScopedService is injected into Invoke
        public async Task Invoke(HttpContext httpContext, AttackerDictionaryService attackerDictionary)
        {
            //if on Radware, use header X-Forwarded-For

            IPAddress? remoteIp = HlidacStatu.Util.RealIpAddress.GetIp(httpContext);
            IPAddress.TryParse(httpContext.Request.Headers["X-Forwarded-For"], out remoteIp);

            if (remoteIp == null)
                remoteIp = HlidacStatu.Util.RealIpAddress.GetIp(httpContext);


            var requestedUrl = httpContext.Request.GetDisplayUrl();

            // autoban for robots
            if (_badWords.Any(b => requestedUrl.Contains(b)))
            {
                await BanIp(remoteIp, DateTime.Now.AddHours(6), 555, requestedUrl);
            }


            // block banned ip
            if (IsBanned(remoteIp))
            {
                Util.Consts.Logger.Info($"Remote IP [{remoteIp}] is banned.");
                httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                httpContext.Response.Headers.Append("content-type", "text/html; charset=utf-8");
                await httpContext.Response.WriteAsync(BannedResponse(remoteIp.ToString()), Encoding.UTF8);
                return;
            }

            string? lastPath = httpContext.Request.GetEncodedPathAndQuery();

            await _next(httpContext);

            // add new ip to blocked ones in case it is attacker
            int statusCode = httpContext.Response.StatusCode;
            if (attackerDictionary.IsAttacker(remoteIp, statusCode, lastPath))
            {
                string pathList = attackerDictionary.PathsForIp(remoteIp);
                await BanIp(remoteIp, DateTime.Now.AddHours(6), statusCode, pathList);
            }

        }


        private bool IsBanned(IPAddress? ipAddress)
        {
            var ipString = ipAddress?.ToString() ?? "_empty";

            var bannedIps = BannedIpRepoCached.GetBannedIps();

            return bannedIps.Exists(b => b.Ip.Contains(ipString));
        }

        private async Task BanIp(IPAddress? ipAddress, DateTime expiration, int lastStatusCode, string pathList)
        {
            var ipString = ipAddress?.ToString() ?? "_empty";
            Util.Consts.Logger.Info($"Adding IP [{ipString}] to ban list.");

            if (AttackerDictionaryService.whitelistedIps.Contains(ipString))
                return;

            await BannedIpRepoCached.BanIp(ipString, expiration, lastStatusCode, pathList);
        }

        private async Task AllowIp(IPAddress? ipAddress)
        {
            var ipString = ipAddress?.ToString() ?? "_empty";

            await BannedIpRepoCached.AllowIp(ipString);
        }

        private string BannedResponse(string ipAddress)
        {
            return $@"<!DOCTYPE html PUBLIC ""-//W3C//DTD XHTML 1.0 Transitional//EN"" ""http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd"">
<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>Aplikace Hlídač státu pro vás není přístupná</title>
    <meta http-equiv=""content-type"" content=""text/html; charset=utf-8"" />
    <style>
      body              {{ color: #999999; background-color: #eeeeee; font-family: Consolas, 'Courier New', monospace; }}
      a:link, a:visited {{ color: #666666; }}
      a:hover           {{ color: #000000; }}
      h1                {{ border-bottom: dashed 1px #999999; padding-bottom: .2ex; font-size: x-large; font-weight: normal; }}
      .content          {{ width: 700px; margin: 50px auto; background-color: #ffffff; padding: 3ex; border: solid 1px #999999; }}
    </style>
</head>
<body>
    <div class=""content"">
        <h1>Aplikace Hlídač státu pro vás není přístupná</h1>
        <p>
            Aplikace Hlídač státu pro vás není přístupná, pravděpodobně z důvodu <b>přetěžování</b> či z důvodu opakovaných <b>pokusů o narušení bezpečnosti</b> Hlídače státu.
            Vaše IP adresa <b>{ipAddress}</b> byla zalogována.
        </p>
        <p>
            Pokud si myslíte, že jde o omyl, napište nám.
        </p>
        <p>
            <strong>
                podpora@hlidacstatu.cz
            </strong>
        </p>

        <hr />

        <h1>You are banned from Hlídač státu application.</h1>
        <p>
            The Hlídač státu application is not accessible to you, possibly because of <b>overloading</b> or repeated <b>attempts to violate the security</b> of the application. 
            Your IP address <b>{ipAddress}</b> has been logged in.
        </p>
        <p>
            If you think this is a mistake, please email us.
        </p>
        <p>
            <strong>
                podpora@hlidacstatu.cz
            </strong>
        </p>
    </div>
</body>
</html>";
        }
    }


    public static class BannedIpsMiddlewareExtension
    {
        public static IApplicationBuilder UseBannedIpsMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<BannedIpsMiddleware>();
        }
    }
}