using HlidacStatu.Web.HealthChecks.Uris;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods to configure <see cref="UriHealthCheck"/>.
/// </summary>
public static class UrisHealthCheck2BuilderExtensions
{
    private const string NAME = "uri-group2";

    /// <summary>
    /// Add a health check for single uri.
    /// </summary>
    /// <param name="builder">The <see cref="IHealthChecksBuilder"/>.</param>
    /// <param name="uri">The uri to check.</param>
    /// <param name="name">The health check name. Optional. If <c>null</c> the type name 'uri-group' will be used for the name.</param>
    /// <param name="failureStatus">
    /// The <see cref="HealthStatus"/> that should be reported when the health check fails. Optional. If <c>null</c> then
    /// the default status of <see cref="HealthStatus.Unhealthy"/> will be reported.
    /// </param>
    /// <param name="tags">A list of tags that can be used to filter sets of health checks. Optional.</param>
    /// <param name="timeout">An optional <see cref="TimeSpan"/> representing the timeout of the check.</param>
    /// <param name="configureClient">An optional setup action to configure the Uris HealthCheck http client.</param>
    /// <param name="configurePrimaryHttpMessageHandler">An optional setup action to configure the Uris HealthCheck http client message handler.</param>
    /// <returns>The specified <paramref name="builder"/>.</returns>
    public static IHealthChecksBuilder AddUrlGroup2(
        this IHealthChecksBuilder builder,
        System.Uri uri,
        string? name = default,
        HealthStatus? failureStatus = default,
        IEnumerable<string>? tags = default,
        TimeSpan? timeout = default,
        Action<IServiceProvider, HttpClient>? configureClient = null,
        Func<IServiceProvider, HttpMessageHandler>? configurePrimaryHttpMessageHandler = null)
    {
        var registrationName = name ?? NAME;
        ConfigureUrisClient(builder, configureClient, configurePrimaryHttpMessageHandler, registrationName);

        return builder.Add(new HealthCheckRegistration(
            registrationName,
            sp =>
            {
                var options = new UriHealthCheck2Options()
                    .AddUri(uri);

                return CreateHealthCheck2(sp, registrationName, options);
            },
            failureStatus,
            tags,
            timeout));
    }

    /// <summary>
    /// Add a health check for single uri.
    /// </summary>
    /// <param name="builder">The <see cref="IHealthChecksBuilder"/>.</param>
    /// <param name="uri">The uri to check.</param>
    /// <param name="httpMethod">The http method to use on check.</param>
    /// <param name="name">The health check name. Optional. If <c>null</c> the type name 'uri-group' will be used for the name.</param>
    /// <param name="failureStatus">
    /// The <see cref="HealthStatus"/> that should be reported when the health check fails. Optional. If <c>null</c> then
    /// the default status of <see cref="HealthStatus.Unhealthy"/> will be reported.
    /// </param>
    /// <param name="tags">A list of tags that can be used to filter sets of health checks. Optional.</param>
    /// <param name="timeout">An optional <see cref="TimeSpan"/> representing the timeout of the check.</param>
    /// <param name="configureClient">An optional setup action to configure the Uris HealthCheck http client.</param>
    /// <param name="configurePrimaryHttpMessageHandler">An optional setup action to configure the Uris HealthCheck http client message handler.</param>
    /// <returns>The specified <paramref name="builder"/>.</returns>
    public static IHealthChecksBuilder AddUrlGroup2(
        this IHealthChecksBuilder builder,
        Uri uri,
        HttpMethod httpMethod,
        string? name = default,
        HealthStatus? failureStatus = default,
        IEnumerable<string>? tags = default,
        TimeSpan? timeout = default,
        Action<IServiceProvider, HttpClient>? configureClient = null,
        Func<IServiceProvider, HttpMessageHandler>? configurePrimaryHttpMessageHandler = null)
    {
        var registrationName = name ?? NAME;
        ConfigureUrisClient(builder, configureClient, configurePrimaryHttpMessageHandler, registrationName);

        return builder.Add(new HealthCheckRegistration(
            registrationName,
            sp =>
            {
                var options = new UriHealthCheck2Options()
                    .AddUri(uri)
                    .UseHttpMethod(httpMethod);

                return CreateHealthCheck2(sp, registrationName, options);
            },
            failureStatus,
            tags,
            timeout));
    }

    /// <summary>
    /// Add a health check for multiple uri's.
    /// </summary>
    /// <param name="builder">The <see cref="IHealthChecksBuilder"/>.</param>
    /// <param name="uris">The collection of uri's to be checked.</param>
    /// <param name="name">The health check name. Optional. If <c>null</c> the type name 'uri-group' will be used for the name.</param>
    /// <param name="failureStatus">
    /// The <see cref="HealthStatus"/> that should be reported when the health check fails. Optional. If <c>null</c> then
    /// the default status of <see cref="HealthStatus.Unhealthy"/> will be reported.
    /// </param>
    /// <param name="tags">A list of tags that can be used to filter sets of health checks. Optional.</param>
    /// <param name="timeout">An optional <see cref="TimeSpan"/> representing the timeout of the check.</param>
    /// <param name="configureClient">An optional setup action to configure the Uris HealthCheck http client.</param>
    /// <param name="configurePrimaryHttpMessageHandler">An optional setup action to configure the Uris HealthCheck http client message handler.</param>
    /// <returns>The specified <paramref name="builder"/>.</returns>
    public static IHealthChecksBuilder AddUrlGroup2(
        this IHealthChecksBuilder builder, IEnumerable<Uri> uris,
        string? name = default,
        HealthStatus? failureStatus = default,
        IEnumerable<string>? tags = default,
        TimeSpan? timeout = default,
        Action<IServiceProvider, HttpClient>? configureClient = null,
        Func<IServiceProvider, HttpMessageHandler>? configurePrimaryHttpMessageHandler = null)
    {
        var registrationName = name ?? NAME;
        ConfigureUrisClient(builder, configureClient, configurePrimaryHttpMessageHandler, registrationName);

        return builder.Add(new HealthCheckRegistration(
            registrationName,
            sp => CreateHealthCheck2(sp, registrationName, UriHealthCheck2Options.CreateFromUris(uris)),
            failureStatus,
            tags,
            timeout));
    }

    /// <summary>
    /// Add a health check for multiple uri's.
    /// </summary>
    /// <param name="builder">The <see cref="IHealthChecksBuilder"/>.</param>
    /// <param name="uris">The collection of uri's to be checked.</param>
    /// <param name="httpMethod">The http method to be used.</param>
    /// <param name="name">The health check name. Optional. If <c>null</c> the type name 'uri-group' will be used for the name.</param>
    /// <param name="failureStatus">
    /// The <see cref="HealthStatus"/> that should be reported when the health check fails. Optional. If <c>null</c> then
    /// the default status of <see cref="HealthStatus.Unhealthy"/> will be reported.
    /// </param>
    /// <param name="tags">A list of tags that can be used to filter sets of health checks. Optional.</param>
    /// <param name="timeout">An optional <see cref="TimeSpan"/> representing the timeout of the check.</param>
    /// <param name="configureClient">An optional setup action to configure the Uris HealthCheck http client.</param>
    /// <param name="configurePrimaryHttpMessageHandler">An optional setup action to configure the Uris HealthCheck http client message handler.</param>
    /// <returns>The specified <paramref name="builder"/>.</returns>
    public static IHealthChecksBuilder AddUrlGroup2(
        this IHealthChecksBuilder builder,
        IEnumerable<Uri> uris,
        HttpMethod httpMethod,
        string? name = default,
        HealthStatus? failureStatus = default,
        IEnumerable<string>? tags = default,
        TimeSpan? timeout = default,
        Action<IServiceProvider, HttpClient>? configureClient = null,
        Func<IServiceProvider, HttpMessageHandler>? configurePrimaryHttpMessageHandler = null)
    {
        var registrationName = name ?? NAME;
        ConfigureUrisClient(builder, configureClient, configurePrimaryHttpMessageHandler, registrationName);

        return builder.Add(new HealthCheckRegistration(
            registrationName,
            sp =>
            {
                var options = UriHealthCheck2Options
                    .CreateFromUris(uris)
                    .UseHttpMethod(httpMethod);

                return CreateHealthCheck2(sp, registrationName, options);
            },
            failureStatus,
            tags,
            timeout));
    }

    /// <summary>
    /// Add a health check for multiple uri's.
    /// </summary>
    /// <param name="builder">The <see cref="IHealthChecksBuilder"/>.</param>
    /// <param name="uriOptions">The action used to configured uri values and specified http methods to be checked.</param>
    /// <param name="name">The health check name. Optional. If <c>null</c> the type name 'uri-group' will be used for the name.</param>
    /// <param name="failureStatus">
    /// The <see cref="HealthStatus"/> that should be reported when the health check fails. Optional. If <c>null</c> then
    /// the default status of <see cref="HealthStatus.Unhealthy"/> will be reported.
    /// </param>
    /// <param name="tags">A list of tags that can be used to filter sets of health checks. Optional.</param>
    /// <param name="timeout">An optional <see cref="TimeSpan"/> representing the timeout of the check.</param>
    /// <param name="configureClient">An optional setup action to configure the Uris HealthCheck http client.</param>
    /// <param name="configurePrimaryHttpMessageHandler">An optional setup action to configure the Uris HealthCheck http client message handler.</param>
    /// <returns>The specified <paramref name="builder"/>.</returns>
    public static IHealthChecksBuilder AddUrlGroup2(
        this IHealthChecksBuilder builder,
        Action<UriHealthCheck2Options>? uriOptions,
        string? name = default,
        HealthStatus? failureStatus = default,
        IEnumerable<string>? tags = default,
        TimeSpan? timeout = default,
        Action<IServiceProvider, HttpClient>? configureClient = null,
        Func<IServiceProvider, HttpMessageHandler>? configurePrimaryHttpMessageHandler = null)
    {
        var registrationName = name ?? NAME;
        ConfigureUrisClient(builder, configureClient, configurePrimaryHttpMessageHandler, registrationName);

        return builder.Add(new HealthCheckRegistration(
            registrationName,
            sp =>
            {
                var options = new UriHealthCheck2Options();
                uriOptions?.Invoke(options);

                return CreateHealthCheck2(sp, registrationName, options);
            },
            failureStatus,
            tags,
            timeout));
    }

    /// <summary>
    /// Add a health check for single uri.
    /// </summary>
    /// <param name="builder">The <see cref="IHealthChecksBuilder"/>.</param>
    /// <param name="uriProvider">Factory for providing the uri to check.</param>
    /// <param name="name">The health check name. Optional. If <c>null</c> the type name 'uri-group' will be used for the name.</param>
    /// <param name="failureStatus"></param>
    /// The <see cref="HealthStatus"/> that should be reported when the health check fails. Optional. If <c>null</c> then
    /// the default status of <see cref="HealthStatus.Unhealthy"/> will be reported.
    /// <param name="tags">A list of tags that can be used to filter sets of health checks. Optional.</param>
    /// <param name="timeout">An optional <see cref="TimeSpan"/> representing the timeout of the check.</param>
    /// <param name="configureClient">An optional setup action to configure the Uris HealthCheck http client.</param>
    /// <param name="configurePrimaryHttpMessageHandler">An optional setup action to configure the Uris HealthCheck http client message handler.</param>
    /// <returns>The specified <paramref name="builder"/>.</returns>
    public static IHealthChecksBuilder AddUrlGroup2(
        this IHealthChecksBuilder builder,
        Func<IServiceProvider, Uri> uriProvider,
        string? name = null,
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null,
        TimeSpan? timeout = null,
        Action<IServiceProvider, HttpClient>? configureClient = null,
        Func<IServiceProvider, HttpMessageHandler>? configurePrimaryHttpMessageHandler = null)
    {
        var registrationName = name ?? NAME;
        ConfigureUrisClient(builder, configureClient, configurePrimaryHttpMessageHandler, registrationName);

        return builder.Add(
            new HealthCheckRegistration(
                registrationName,
                sp =>
                {
                    var uri = uriProvider(sp);
                    var uriHealthCheckOptions = new UriHealthCheck2Options().AddUri(uri, null);

                    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();

                    return new UriHealthCheck2(
                        uriHealthCheckOptions,
                        () => httpClientFactory.CreateClient(registrationName));
                },
                failureStatus,
                tags,
                timeout));
    }

    private static UriHealthCheck2 CreateHealthCheck2(IServiceProvider sp, string name, UriHealthCheck2Options options)
    {
        var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
        return new UriHealthCheck2(options, () => httpClientFactory.CreateClient(name));
    }

    private static readonly Action<IServiceProvider, HttpClient> _emptyHttpClientCallback = (_, _) => { };
    private static readonly Func<IServiceProvider, HttpMessageHandler> _defaultHttpMessageHandlerCallback = _ => new HttpClientHandler();

    private static void ConfigureUrisClient(
        IHealthChecksBuilder builder,
        Action<IServiceProvider, HttpClient>? configureHttpclient,
        Func<IServiceProvider, HttpMessageHandler>? configurePrimaryHttpMessageHandler,
        string registrationName)
    {
        builder.Services.AddHttpClient(registrationName)
            .ConfigureHttpClient(configureHttpclient ?? _emptyHttpClientCallback)
            .ConfigurePrimaryHttpMessageHandler(configurePrimaryHttpMessageHandler ?? _defaultHttpMessageHandlerCallback);
    }
}
