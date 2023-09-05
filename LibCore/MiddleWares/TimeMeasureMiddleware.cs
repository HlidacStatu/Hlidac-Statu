using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Devmasters.Log;

namespace HlidacStatu.LibCore.MiddleWares
{
    public class TimeMeasureMiddleware
    {
        private readonly Logger _logger;
        private readonly RequestDelegate _next;
        private readonly List<string> _exceptions;

        public TimeMeasureMiddleware(RequestDelegate next, Devmasters.Log.Logger logger, List<string> exceptions)
        {
            _next = next;
            _logger = logger;
            _exceptions = exceptions;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var url = httpContext.Request.Path;
            var query = httpContext.Request.QueryString.ToString();
            var sw = new Stopwatch();
            sw.Start();
            await _next(httpContext);
            sw.Stop();
            
            if(_exceptions.Any(e => url.ToString().StartsWith(e, StringComparison.InvariantCultureIgnoreCase)))
                return;

            httpContext.Items.TryAdd("timeToProcessRequest", sw.ElapsedMilliseconds);
            
            if (sw.ElapsedMilliseconds >=1000 && sw.ElapsedMilliseconds < 2000)
            {

                if (httpContext.User != null && httpContext.User.Identity != null && httpContext.User.Identity.Name != null)
                    _logger.Debug("{webpage} with {query_params} for {user} done in {elapsed} ms", url, query, httpContext.User.Identity.Name, sw.ElapsedMilliseconds);
                else
                    _logger.Debug("{webpage} with {query_params} done in {elapsed} ms", url, query, sw.ElapsedMilliseconds);

            }
            else if (sw.ElapsedMilliseconds >= 2000)
            {

                if (httpContext.User != null && httpContext.User.Identity != null && httpContext.User.Identity.Name != null)
                    _logger.Warning("{webpage} with {query_params} for {user} done in {elapsed} ms", url, query, httpContext.User.Identity.Name, sw.ElapsedMilliseconds);
                else
                    _logger.Warning("{webpage} with {query_params} done in {elapsed} ms", url, query, sw.ElapsedMilliseconds);

            }

        }

    }
    

    public static class TimeMeasureMiddlewareExtension
    {
        public static IApplicationBuilder UseTimeMeasureMiddleware(this IApplicationBuilder builder, Devmasters.Log.Logger logger, List<string>? exceptions = null)
        {
            return builder.UseMiddleware<TimeMeasureMiddleware>(logger, exceptions ?? new List<string>());
        }
    }
}