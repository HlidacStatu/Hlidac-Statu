using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using HlidacStatu.LibCore.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

namespace HlidacStatu.LibCore.MiddleWares
{
    public class RequestTrackMiddleware
    {
        public static string TrackDataKey { get; } = "HS_track_data";

        private readonly RequestDelegate _next;

        public RequestTrackMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            string? ip = context.Connection.RemoteIpAddress?.ToString();
            string traceIdentifier = context.TraceIdentifier;
            string method = context.Request.Method;
            string requestUrl = context.Request.GetEncodedUrl();

            await _next(context);

            string? userName = context.User.Identity?.Name;
            int statusCode = context.Response.StatusCode;

            string customData = "";
            if (context.Items.TryGetValue(TrackDataKey, out object? trackData))
            {
                var dataList = (TrackDataList)trackData;
                customData = dataList.Draw();
            }

            //assemble string here
        }
    }

    public static class RequestTrackMiddlewareExtension
    {
        public static IApplicationBuilder UseRequestTrackMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<TimeMeasureMiddleware>();
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

        public string Draw()
        {
            return string.Join('\n', Storage);
        }
    }
    
}