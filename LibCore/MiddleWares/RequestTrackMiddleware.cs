using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using HlidacStatu.Entities;
using HlidacStatu.LibCore.Services;
using HlidacStatu.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

namespace HlidacStatu.LibCore.MiddleWares
{
    public class RequestTrackMiddleware
    {
        public static string TrackDataKey { get; } = "HS_track_data";

        private readonly RequestDelegate _next;
        private readonly Options _options;

        public RequestTrackMiddleware(RequestDelegate next, Options options)
        {
            _next = next;
            _options = options;
        }
        

        public async Task Invoke(HttpContext context)
        {
            string? ip = context.Connection.RemoteIpAddress?.ToString();
            string traceIdentifier = context.TraceIdentifier;
            string method = context.Request.Method;
            string requestUrl = context.Request.GetEncodedUrl();

            await _next(context);
            
            // do this tracking only for limited paths /API
            if(_options.LimitToPaths.Count > 0
                && !_options.LimitToPaths.Any(path => context.Request.Path.StartsWithSegments(path))) 
                return;

            string? userName = context.User.Identity?.Name;
            
            int statusCode = context.Response.StatusCode;

            string customData = "";
            if (context.Items.TryGetValue(TrackDataKey, out object? trackData))
            {
                var dataList = (TrackDataList)trackData;
                customData = dataList.ToJson();
            }
            
            //get request time...
            long timeElapsed = 0;
            if (context.Items.TryGetValue("timeToProcessRequest", out object? requestTime))
            {
                timeElapsed = (long)requestTime;
            }
            
            if(timeElapsed <= _options.MinimumRequestTimeToTrackMs)
                return;
            
            string exceptionDetail = "";
            if (context.Items.TryGetValue("errorPageCtx", out object? exception))
            {
                exceptionDetail = (string)exception;
            }

            //assemble string here
            var audit = new Audit()
            {
                machineName = Environment.MachineName,
                applicationName = _options.ApplicationName,
                date = DateTime.Now,
                operation = Audit.Operations.Call.ToString(),
                userId = userName,
                IP = ip,
                traceId = traceIdentifier,
                method = method,
                requestUrl = requestUrl,
                statusCode = statusCode,
                additionalData = customData,
                timeElapsed = timeElapsed,
                exception = exceptionDetail,
                
            };
            
            AuditRepo.Add(audit);
        }
        
        public class Options
        {
            public List<string> LimitToPaths { get; set; } = new List<string>();
            public int MinimumRequestTimeToTrackMs { get; set; } = 0;
            public string ApplicationName { get; set; }
        }
    }

    public static class RequestTrackMiddlewareExtension
    {
        public static IApplicationBuilder UseRequestTrackMiddleware(this IApplicationBuilder builder
            , RequestTrackMiddleware.Options options)
        {
            return builder.UseMiddleware<RequestTrackMiddleware>(options);
        }

        public static void SetTrackData(this HttpContext context, string dataString)
        {
            if (context.Items.TryGetValue(RequestTrackMiddleware.TrackDataKey, out var trackData))
            {
                var dataList = (TrackDataList)trackData;
                dataList.Add(dataString);
            }
            else
            {
                var dataList = new TrackDataList();
                dataList.Add(dataString);
                
                context.Items.TryAdd(RequestTrackMiddleware.TrackDataKey, dataList);
            }

        }
    }

    public class TrackDataList
    {
        private List<string> Storage { get; } = new();
        
        public void Add(string item)
        {
            Storage.Add(item);
        }

        public string ToJson()
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(Storage);
        }
    }
    
}