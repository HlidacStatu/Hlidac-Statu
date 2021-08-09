
using Microsoft.Extensions.Diagnostics.HealthChecks;

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class GenericHealthCheckBuilderExtensions
    {
        public static IHealthChecksBuilder AddHealthCheckWithOptions<T, TOptions>(
            this IHealthChecksBuilder builder, 
            TOptions options, 
            string name = default, HealthStatus? failureStatus = default, IEnumerable<string> tags = default, TimeSpan? timeout = default)
        where T : IHealthCheck
        {
            name = name ?? typeof(T).Name;
            T instance = (T)Activator.CreateInstance(typeof(T), options);

            return builder.Add(new HealthCheckRegistration(
                name,
                instance,
                failureStatus,
                tags,
                timeout));
        }

    }
}