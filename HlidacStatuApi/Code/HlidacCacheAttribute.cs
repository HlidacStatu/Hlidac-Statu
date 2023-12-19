using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;

namespace HlidacStatuApi.Code
{
    public class HlidacCacheAttribute : Attribute, IResourceFilter
    {
        private readonly int _duration;
        private readonly string[] _queryKeys;
        private readonly bool _differAuth;
        
        private readonly Serilog.ILogger _logger = Serilog.Log.ForContext<HlidacCacheAttribute>();

        public HlidacCacheAttribute(int durationInSeconds, string queryKeys, bool differAuth)
        {
            _duration = durationInSeconds;
            _queryKeys = queryKeys.Split(';', StringSplitOptions.RemoveEmptyEntries);
            _differAuth = differAuth;
        }

        public void OnResourceExecuting(ResourceExecutingContext context)
        {
            var key = BuildCacheKey(context.HttpContext);

            IMemoryCache? cacheService = (IMemoryCache)context.HttpContext.RequestServices.GetService(typeof(IMemoryCache));
            if (cacheService is null)
                _logger.Error("IMemoryCache service was not found - HlidacCacheAttribute.cs:29");

            if (cacheService.TryGetValue(key, out IActionResult result))
            {
                context.Result = result;
            }
        }

        public void OnResourceExecuted(ResourceExecutedContext context)
        {
            var key = BuildCacheKey(context.HttpContext);

            IMemoryCache? cacheService = (IMemoryCache)context.HttpContext.RequestServices.GetService(typeof(IMemoryCache));
            if (cacheService is null)
                _logger.Error("IMemoryCache service was not found - HlidacCacheAttribute.cs:43");

            if (System.Diagnostics.Debugger.IsAttached)
                cacheService.Set(key, context.Result, TimeSpan.FromSeconds(2));
            else
                cacheService.Set(key, context.Result, TimeSpan.FromSeconds(_duration));
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