using System;
using System.Text;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
using Serilog;

namespace HlidacStatu.Lib.Web.UI.Attributes
{
    public class HlidacCacheAttribute : Attribute, IResourceFilter
    {
        private readonly long _duration;
        private readonly string[] _queryKeys;
        private readonly bool _differAuth;
        
        private static readonly ILogger _logger = Log.ForContext<HlidacCacheAttribute>();

        public HlidacCacheAttribute(double durationInSecond, string queryKeys = "", bool differAuth = false)
            : this((long)durationInSecond,queryKeys, differAuth)
        {
        }

        public HlidacCacheAttribute(long durationInSeconds, string queryKeys = "", bool differAuth = false)
        {
            queryKeys = queryKeys ?? "";
            _duration = durationInSeconds;
            _queryKeys = queryKeys.Split(';', StringSplitOptions.RemoveEmptyEntries);
            _differAuth = differAuth;
        }

        public void OnResourceExecuting(ResourceExecutingContext context)
        {

            IMemoryCache? cacheService = (IMemoryCache)context.HttpContext.RequestServices.GetService(typeof(IMemoryCache));
            if (cacheService is null)
            {
                _logger.Error("IMemoryCache service was not found - HlidacCacheAttribute.cs");
                return;  // Exit early!
            }
            var key = BuildCacheKey(context.HttpContext);

            if (cacheService.TryGetValue(key, out IActionResult? cachedResult) && cachedResult is not null)
            {
                context.Result = cachedResult;
                // Mark that we served from cache (so OnResourceExecuted can skip re-saving)
                context.HttpContext.Items["_HlidacCacheHit"] = true;
            }

        }

        public void OnResourceExecuted(ResourceExecutedContext context)
        {
            // Skip if we already served from cache
            if (context.HttpContext.Items.ContainsKey("_HlidacCacheHit"))
                return;

            // Skip if result is null (error occurred)
            if (context.Result is null)
                return;


            IMemoryCache? cacheService = (IMemoryCache)context.HttpContext.RequestServices.GetService(typeof(IMemoryCache));
            if (cacheService is null)
            {
                _logger.Error("IMemoryCache service was not found - HlidacCacheAttribute.cs:43");
                return;
            }

            var key = BuildCacheKey(context.HttpContext);
            var duration = System.Diagnostics.Debugger.IsAttached
                ? TimeSpan.FromHours(2)
                : TimeSpan.FromSeconds(_duration);

            cacheService.Set(key, context.Result, duration);

        }

        private string BuildCacheKey(HttpContext context)
        {
            var request = context.Request;

            var sb = new StringBuilder("hsCtrlrCache");
            sb.Append($"_{request.Path}/");

            foreach (var queryKey in _queryKeys)
            {
                sb.Append($"{queryKey}=");
                if (request.Query.TryGetValue(queryKey, out var stringValues))
                {
                    sb.Append(stringValues);
                }

                sb.Append(";");
            }

            if (_differAuth)
                sb.Append(context.User.Identity?.IsAuthenticated == true ? "@auth" : "@notauth");

            return sb.ToString();
        }

    }
}