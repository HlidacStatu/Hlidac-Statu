using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace HlidacStatu.LibCore.MiddleWares
{
    public class TimeMeasureMiddleware
    {
        private readonly ILogger _logger = Log.ForContext<TimeMeasureMiddleware>();
        private readonly RequestDelegate _next;
        private readonly List<string> _ignorePaths;
        private readonly long _infoTimeThresholdMs;
        private readonly long _warningTimeThresholdMs;
        private readonly long _errorTimeThresholdMs;

        public TimeMeasureMiddleware(RequestDelegate next, List<string> ignorePaths,
            long infoTimeThresholdMs = 1000, long warningTimeThresholdMs = 2000, long errorTimeThresholdMs = 3000)
        {
            _next = next;
            _ignorePaths = ignorePaths;
            _infoTimeThresholdMs = infoTimeThresholdMs;
            _warningTimeThresholdMs = warningTimeThresholdMs;
            _errorTimeThresholdMs = errorTimeThresholdMs;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var url = httpContext.Request.Path;
            var query = httpContext.Request.QueryString.ToString();
            var sw = new Stopwatch();
            sw.Start();
            await _next(httpContext);
            sw.Stop();
            
            if(_ignorePaths.Any(e => url.ToString().StartsWith(e, StringComparison.InvariantCultureIgnoreCase)))
                return;

            httpContext.Items.TryAdd("timeToProcessRequest", sw.ElapsedMilliseconds);

            if (sw.ElapsedMilliseconds >= _infoTimeThresholdMs)
            {
                StringValues referer = default;
                StringValues userAgent = default;
                _ = httpContext?.Request?.Headers?.TryGetValue("Referer", out referer);
                _ = httpContext?.Request?.Headers?.TryGetValue("User-Agent", out userAgent);

                if (sw.ElapsedMilliseconds >= _errorTimeThresholdMs)
                {
                    _logger.Warning("page processing: {elapsed} ms\t{webpage}{query_params}\tuser:{user}\tuserAgent:{userAgent}\tReferer:{referer}",
                        sw.ElapsedMilliseconds,
                        url,
                        query,
                        httpContext?.User?.Identity?.Name,
                        userAgent,
                        referer);
                }
                else if (sw.ElapsedMilliseconds >= _warningTimeThresholdMs)
                {
                    _logger.Information("page processing: {elapsed} ms\t{webpage}{query_params}\tuser:{user}\tuserAgent:{userAgent}\tReferer:{referer}",
                        sw.ElapsedMilliseconds,
                        url,
                        query,
                        httpContext?.User?.Identity?.Name,
                        userAgent,
                        referer);
                }
                else if (sw.ElapsedMilliseconds >= _infoTimeThresholdMs)
                {
                    _logger.Debug("page processing: {elapsed} ms\t{webpage}{query_params}\tuser:{user}\tuserAgent:{userAgent}\tReferer:{referer}",
                        sw.ElapsedMilliseconds,
                        url,
                        query,
                        httpContext?.User?.Identity?.Name,
                        userAgent,
                        referer);
                }
            }
        }

    }
    

    public static class TimeMeasureMiddlewareExtension
    {
        public static IApplicationBuilder UseTimeMeasureMiddleware(this IApplicationBuilder builder, List<string>? ignorePaths = null)
        {
            return builder.UseMiddleware<TimeMeasureMiddleware>(ignorePaths ?? new List<string>());
        }
    }
}