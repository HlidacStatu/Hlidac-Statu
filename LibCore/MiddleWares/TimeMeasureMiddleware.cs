using HlidacStatu.LibCore.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using System.Diagnostics;
using System.Threading.Tasks;

namespace HlidacStatu.LibCore.MiddleWares
{
    public class TimeMeasureMiddleware
    {
        public static Devmasters.Logging.Logger _logger = new Devmasters.Logging.Logger("HlidacStatu.PageTimes");

        private readonly RequestDelegate _next;

        public TimeMeasureMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext, AttackerDictionaryService attackerDictionary)
        {
            var url = httpContext.Request.GetDisplayUrl();
            var sw = new Stopwatch();
            sw.Start();
            await _next(httpContext);
            sw.Stop();
            if (sw.ElapsedMilliseconds > 2000)
            {
                _logger.Warning($"Loading time of {url} was {sw.ElapsedMilliseconds} ms");
            }
        }
    }
    

    public static class TimeMeasureMiddlewareExtension
    {
        public static IApplicationBuilder UseTimeMeasureMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<TimeMeasureMiddleware>();
        }
    }
}