using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace HlidacStatu.LibCore.MiddleWares
{
    public class LogRequestMiddleware
    {
        private readonly ILogger _logger = Log.ForContext<LogRequestMiddleware>();

        private readonly RequestDelegate _next;
        private readonly Options _options;

        public LogRequestMiddleware(RequestDelegate next, Options options)
        {
            _next = next;
            _options = options;
        }


        public async Task Invoke(HttpContext context)
        {


            var sw = new Stopwatch();
            sw.Start();
            await _next(context);
            sw.Stop();

            try
            {
                string method = context.Request.Method;
                string hostname = context.Request.Host.Host;
                string request = context.Request.Path;


                string? ip = HlidacStatu.Util.RealIpAddress.GetIp(context)?.ToString();
                string traceIdentifier = context.TraceIdentifier;
                List<KeyValuePair<string, string>> querystring = new();
                if (context?.Request?.Query != null)
                    foreach (var item in context.Request.Query)
                    {
                        foreach (var itemval in item.Value)
                        {
                            querystring.Add(new(item.Key, itemval));
                        }
                    }

                List<KeyValuePair<string, string>> formstring = new();

                if (context.Request.Method == "POST" && context?.Request?.Form != null)
                    foreach (var item in context.Request.Form)
                    {
                        foreach (var itemval in item.Value)
                        {
                            formstring.Add(new(item.Key, itemval));
                        }
                    }


                if (this._options.RequestToLogFilter != null)
                {
                    if (_options.RequestToLogFilter(context))
                    {

                        _logger.Write(_options.LogEventLevel,
                            "{hostname} {method} {request} {elapsedMs} {statuscode} {useragent} {@query} {@form}",
                            hostname, method, request, sw.ElapsedMilliseconds, context?.Response?.StatusCode,
                            context.Request?.Headers["User-Agent"], querystring, formstring);
                    }

                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error in processing {nameof(LogRequestMiddleware)}");
            }

        }

        public class Options
        {
            public Serilog.Events.LogEventLevel LogEventLevel { get; set; } = Serilog.Events.LogEventLevel.Debug;

            public Func<HttpContext, bool> RequestToLogFilter { get; set; } = null;

            //serilog {} style
            public string LogMessageTemplate { get; set; } = "{hostname} {method} {request} {statuscode} {useragent} {@query} {@form}";

            /// <summary>
            /// Same
            /// </summary>
            Func<HttpContext, object>[] LogMessagePropertyValueSelectors { get; set; } = new Func<HttpContext, object>[]
            {
                context=>context?.Request?.Host,
                context=>context?.Request?.Method,
                context=>context?.Request?.Path,
                context=>context?.Response?.StatusCode,
                context=>context.Request?.Headers["User-Agent"],
                context => GetQueryStringParams(context),
                context => GetFormStringParams(context),


            };
        }

        public static List<KeyValuePair<string, string>> GetQueryStringParams(HttpContext context)
        {
            List<KeyValuePair<string, string>> querystring = new();
            if (context?.Request?.Query != null)
                foreach (var item in context.Request.Query)
                {
                    foreach (var itemval in item.Value)
                    {
                        querystring.Add(new(item.Key, itemval));
                    }
                }

            return querystring;
        }

        public static List<KeyValuePair<string, string>> GetFormStringParams(HttpContext context)
        {
            List<KeyValuePair<string, string>> querystring = new();
            if (context?.Request?.Query != null)
                foreach (var item in context.Request.Form)
                {
                    foreach (var itemval in item.Value)
                    {
                        querystring.Add(new(item.Key, itemval));
                    }
                }

            return querystring;
        }
    }

    public static class LogRequestMiddlewareExtension
    {
        public static IApplicationBuilder UseLogRequestMiddleware(this IApplicationBuilder builder
            , LogRequestMiddleware.Options options)
        {
            return builder.UseMiddleware<LogRequestMiddleware>(options);
        }

    }



}