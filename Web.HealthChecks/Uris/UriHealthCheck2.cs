using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace HlidacStatu.Web.HealthChecks.Uris;
internal static class Guard
{
    /// <summary>Throws an <see cref="ArgumentNullException"/> if <paramref name="argument"/> is null.</summary>
    /// <param name="argument">The reference type argument to validate as non-null.</param>
    /// <param name="throwOnEmptyString">Only applicable to strings.</param>
    /// <param name="paramName">The name of the parameter with which <paramref name="argument"/> corresponds.</param>
    public static T ThrowIfNull<T>([NotNull] T? argument, bool throwOnEmptyString = false, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
        where T : class
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(argument, paramName);
        if (throwOnEmptyString && argument is string s && string.IsNullOrEmpty(s))
            throw new ArgumentNullException(paramName);
#else
        if (argument is null || throwOnEmptyString && argument is string s && string.IsNullOrEmpty(s))
            throw new ArgumentNullException(paramName);
#endif
        return argument;
    }
}
public class UriHealthCheck2 : IHealthCheck
{
    private readonly UriHealthCheck2Options _options;
    private readonly Func<HttpClient> _httpClientFactory;

    public UriHealthCheck2(UriHealthCheck2Options options, Func<HttpClient> httpClientFactory)
    {
        _options = Guard.ThrowIfNull(options);
        _httpClientFactory = Guard.ThrowIfNull(httpClientFactory);
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var defaultHttpMethod = _options.HttpMethod;
        var defaultExpectedStatusCodes = _options.ExpectedHttpCodes;
        var defaultTimeout = _options.Timeout;
        int idx = 0;

        try
        {
            foreach (var item in _options.UrisOptions)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return new HealthCheckResult(context.Registration.FailureStatus, description: $"{nameof(UriHealthCheck2)} execution is cancelled.");
                }

                var method = item.HttpMethod ?? defaultHttpMethod;
                var (Min, Max) = item.ExpectedHttpCodes ?? defaultExpectedStatusCodes;
                var timeout = item.Timeout != TimeSpan.Zero ? item.Timeout : defaultTimeout;

                var httpClient = _httpClientFactory();

                using var requestMessage = new HttpRequestMessage(method, item.Uri);

#if NET5_0_OR_GREATER
                requestMessage.Version = httpClient.DefaultRequestVersion;
                requestMessage.VersionPolicy = httpClient.DefaultVersionPolicy;
#endif

                foreach (var (Name, Value) in item.Headers)
                {
                    requestMessage.Headers.Add(Name, Value);
                }

                using (var timeoutSource = new CancellationTokenSource(timeout))
                using (var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutSource.Token, cancellationToken))
                {
                    using var response = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, linkedSource.Token).ConfigureAwait(false);

                    if (!((int)response.StatusCode >= Min && (int)response.StatusCode <= Max))
                    {
                        return new HealthCheckResult(context.Registration.FailureStatus, description: $"Discover endpoint #{idx} is not responding with code in {Min}...{Max} range, the current status is {response.StatusCode}.");
                    }

                    if (item.ExpectedContent != null)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        switch (item.ExpectedContentComparisonMethod)
                        {
                            case UriOptions.StringComparisonMethod.Exact:
                                if (responseBody?.Equals(item.ExpectedContent, StringComparison.InvariantCultureIgnoreCase ) == false)
                                    return new HealthCheckResult(context.Registration.FailureStatus, description: $"The expected value '{item.ExpectedContent}' was not found in the response body.");
                                break;
                            case UriOptions.StringComparisonMethod.Contains:
                                if (responseBody?.Contains(item.ExpectedContent, StringComparison.InvariantCultureIgnoreCase) == false)
                                    return new HealthCheckResult(context.Registration.FailureStatus, description: $"The expected value '{item.ExpectedContent}' was not found anywhere the response body.");
                                break;
                            case UriOptions.StringComparisonMethod.StartsWith:
                                if (responseBody?.StartsWith(item.ExpectedContent, StringComparison.InvariantCultureIgnoreCase) == false)
                                    return new HealthCheckResult(context.Registration.FailureStatus, description: $"The expected value '{item.ExpectedContent}' was not found at the start of the response body.");
                                break;
                            case UriOptions.StringComparisonMethod.EndsWith:
                                if (responseBody?.EndsWith(item.ExpectedContent, StringComparison.InvariantCultureIgnoreCase) == false)
                                    return new HealthCheckResult(context.Registration.FailureStatus, description: $"The expected value '{item.ExpectedContent}' was not found at the end of the response body.");
                                break;
                            case UriOptions.StringComparisonMethod.ExactCaseSensitive:
                                if (responseBody?.Equals(item.ExpectedContent, StringComparison.InvariantCulture) == false)
                                    return new HealthCheckResult(context.Registration.FailureStatus, description: $"The expected value '{item.ExpectedContent}' was not found in the response body.");
                                break;
                            case UriOptions.StringComparisonMethod.ContainsCaseSensitive:
                                if (responseBody?.Contains(item.ExpectedContent, StringComparison.InvariantCulture) == false)
                                    return new HealthCheckResult(context.Registration.FailureStatus, description: $"The expected value '{item.ExpectedContent}' was not found anywhere in the response body.");
                                break;
                            case UriOptions.StringComparisonMethod.StartsWithCaseSensitive:
                                if (responseBody?.StartsWith(item.ExpectedContent, StringComparison.InvariantCulture) == false)
                                    return new HealthCheckResult(context.Registration.FailureStatus, description: $"The expected value '{item.ExpectedContent}' was not found at the start of the response body.");  
                                break;
                            case UriOptions.StringComparisonMethod.EndsWithCaseSensitive:
                                if (responseBody?.EndsWith(item.ExpectedContent, StringComparison.InvariantCulture) == false)
                                    return new HealthCheckResult(context.Registration.FailureStatus, description: $"The expected value '{item.ExpectedContent}' was not found at the end of the response body.");
                                break;
                        }
                    }

                    ++idx;
                }
            }
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, exception: ex);
        }
    }
}
