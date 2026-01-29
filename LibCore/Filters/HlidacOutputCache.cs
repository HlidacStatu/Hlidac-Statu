namespace HlidacStatu.LibCore.Filters;


using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class HlidacOutputCacheAttribute : TypeFilterAttribute
{
    public HlidacOutputCacheAttribute(double durationInSeconds, string queryKeys = "", bool differAuth = false)
        : this((long)durationInSeconds, queryKeys, differAuth)
    { }

    public HlidacOutputCacheAttribute(long durationInSeconds, string queryKeys = "", bool differAuth = false)
        : base(typeof(HlidacOutputCacheFilter))
    {
        Arguments = new object[] { durationInSeconds, queryKeys ?? string.Empty, differAuth };
    }
}

public sealed class HlidacOutputCacheFilter : IAsyncResultFilter
{
    private readonly long _duration;
    private readonly string[] _queryKeys;
    private readonly bool _differAuth;
    private readonly IMemoryCache _cache;
    private readonly ILogger<HlidacOutputCacheFilter> _logger;

    public HlidacOutputCacheFilter(
        long durationInSeconds,
        string queryKeys,
        bool differAuth,
        IMemoryCache cache,
        ILogger<HlidacOutputCacheFilter> logger)
    {
        if (queryKeys == "*")
            queryKeys = string.Empty;
        _duration = durationInSeconds;
        _queryKeys = (queryKeys ?? string.Empty)
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        _differAuth = differAuth;
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        var httpContext = context.HttpContext;

        if (!HttpMethods.IsGet(httpContext.Request.Method))
        {
            await next();
            return;
        }

        var cacheKey = BuildCacheKey(httpContext);

        // 1) Zkusíme najít HTML v cache
        if (_cache.TryGetValue(cacheKey, out string cachedHtml))
        {
            _logger.LogDebug("HlidacOutputCache HIT: {Key}", cacheKey);

            context.Result = new ContentResult
            {
                Content = cachedHtml,
                ContentType = "text/html; charset=utf-8",
                StatusCode = StatusCodes.Status200OK
            };

            /*
            nastavíš context.Result na svůj ContentResult s HTML z cache,
	    •	zavoláš await next(), aby se tenhle nový result opravdu vykonal,
	    •	nic neřešíš s Response.Body.
             */
            await next();   
            return;
        }

        _logger.LogDebug("HlidacOutputCache MISS: {Key}", cacheKey);

        // 2) Cache MISS – odchytíme výstup resultu
        var originalBodyStream = httpContext.Response.Body;
        await using var memoryStream = new MemoryStream();
        httpContext.Response.Body = memoryStream;

        var executedContext = await next(); // TADY se vykreslí ViewResult → zapisuje do memoryStream

        httpContext.Response.Body = originalBodyStream;

        if (executedContext.Exception != null && !executedContext.ExceptionHandled)
        {
            memoryStream.Position = 0;
            await memoryStream.CopyToAsync(originalBodyStream);
            return;
        }

        memoryStream.Position = 0;
        string body;
        using (var reader = new StreamReader(memoryStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true))
        {
            body = await reader.ReadToEndAsync();
        }

        // zapíšeme klientovi
        var bodyBytes = Encoding.UTF8.GetBytes(body);
        await originalBodyStream.WriteAsync(bodyBytes, 0, bodyBytes.Length);

        // 3) uložíme do cache
        if (!string.IsNullOrEmpty(body) && httpContext.Response.StatusCode == StatusCodes.Status200OK)
        {
            // nastavíme TTL podle toho, jestli je připojen debugger
            var lifetime = System.Diagnostics.Debugger.IsAttached
                ? TimeSpan.FromSeconds(2)
                : TimeSpan.FromSeconds(_duration);

            _cache.Set(cacheKey, body,
                new MemoryCacheEntryOptions().SetAbsoluteExpiration(lifetime));

            _logger.LogDebug("HlidacOutputCache STORE: {Key}, TTL={Lifetime}", cacheKey, lifetime);
        }
    }

    private string BuildCacheKey(HttpContext context)
    {
        var request = context.Request;
        var sb = new StringBuilder("hsCtrlrOutputCache");

        sb.Append('_').Append(request.Path.ToString().ToLowerInvariant()).Append('/');

        foreach (var queryKey in _queryKeys)
        {
            sb.Append(queryKey).Append('=');

            if (request.Query.TryGetValue(queryKey, out var values))
            {
                sb.Append(values.ToString());
            }

            sb.Append(';');
        }

        if (_differAuth)
        {
            sb.Append(context.User?.Identity?.IsAuthenticated == true ? "@auth" : "@notauth");
        }

        return sb.ToString();
    }
}