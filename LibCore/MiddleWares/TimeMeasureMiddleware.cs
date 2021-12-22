using System.Collections.Generic;
using HlidacStatu.LibCore.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Devmasters.Log;

namespace HlidacStatu.LibCore.MiddleWares
{
    public class TimeMeasureMiddleware
    {
        public static Devmasters.Log.Logger _logger = Devmasters.Log.Logger.CreateLogger("HlidacStatu.PageTimes",
                            Devmasters.Log.Logger.DefaultConfiguration()
                                .Enrich.WithProperty("codeversion", System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString())
                                .AddFileLoggerFilePerLevel("c:/Data/Logs/HlidacStatu/Web.PageTimes", "slog.txt",
                                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {SourceContext} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                                    rollingInterval: Serilog.RollingInterval.Day,
                                    fileSizeLimitBytes: null,
                                    retainedFileCountLimit: 9,
                                    shared: true
                                    ));

        private readonly RequestDelegate _next;

        public TimeMeasureMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var url = httpContext.Request.Path;
            var query = httpContext.Request.QueryString.ToString();
            var sw = new Stopwatch();
            sw.Start();
            await _next(httpContext);
            sw.Stop();

            httpContext.Items.TryAdd("timeToProcessRequest", sw.ElapsedMilliseconds);
            
            if (sw.ElapsedMilliseconds > 2000)
            {
                //_logger.Warning($"Loading time of {url} was {sw.ElapsedMilliseconds} ms");
                var msg = new Devmasters.Log.LogMessage();
                //<conversionPattern value="%date|%property{page}|%property{params}|%property{user}|%property{elapsedtime}" />
                msg.SetCustomKeyValue("web_page", url);
                msg.SetCustomKeyValue("web_params", query);
                msg.SetCustomKeyValue("web_elapsedtime", sw.ElapsedMilliseconds);

                if (httpContext.User != null && httpContext.User.Identity != null && httpContext.User.Identity.Name != null)
                    msg.SetCustomKeyValue("web_user", httpContext.User.Identity.Name);
                _logger.Warning(msg);
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