
using HlidacStatu.Web.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class GenericHealthCheckBuilderExtensions
    {
        public static IHealthChecksBuilder AddHealthCheckWithOptions<T>(
            this IHealthChecksBuilder builder,
            string name = default, HealthStatus? failureStatus = default, IEnumerable<string> tags = default, TimeSpan? timeout = default)
        where T : IHealthCheck
        {
            name = name ?? typeof(T).Name;
            T instance = (T)Activator.CreateInstance(typeof(T));

            return builder.Add(new HealthCheckRegistration(
                name,
                instance,
                failureStatus,
                tags,
                timeout));
        }

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
                new WithLogging(instance),
                failureStatus,
                tags,
                timeout));
        }


        public static IHealthChecksBuilder AddHealthCheckWithLogging(
            this IHealthChecksBuilder builder,
            IHealthCheck instance,
            string name = default, HealthStatus? failureStatus = default, IEnumerable<string> tags = default, TimeSpan? timeout = default)
        {
            if (instance == null)
                throw new ArgumentNullException("instance");

            name = name ?? instance.GetType().Name;
            return builder.Add(new HealthCheckRegistration(
                name,
                new HlidacStatu.Web.HealthChecks.WithLogging(instance),
                failureStatus,
                tags,
                timeout));
        }
        public static IHealthChecksBuilder AddHealthCheckWithResponseTime(
            this IHealthChecksBuilder builder,
            IHealthCheck instance,
            string name = default, HealthStatus? failureStatus = default, IEnumerable<string> tags = default, TimeSpan? timeout = default)
        {
            if (instance == null)
                throw new ArgumentNullException("instance");

            name = name ?? instance.GetType().Name;
            return builder.Add(new HealthCheckRegistration(
                name,
                new HlidacStatu.Web.HealthChecks.WithResponseTime(instance),
                failureStatus,
                tags,
                timeout));
        }


        public static IHealthChecksBuilder AddCachedHealthCheck(
            this IHealthChecksBuilder builder,
            IHealthCheck instance,
            TimeSpan cacheTime,
            string name = default, HealthStatus? failureStatus = default, IEnumerable<string> tags = default, TimeSpan? timeout = default)
        {
            if (instance == null)
                throw new ArgumentNullException("instance");

            name = name ?? instance.GetType().Name;
            return builder.Add(new HealthCheckRegistration(
                name,
                new HlidacStatu.Web.HealthChecks.CachedResult(instance, cacheTime),
                failureStatus,
                tags,
                timeout));
        }

        public static IHealthChecksBuilder AddCachedHealthCheckWithOptions(
            this IHealthChecksBuilder builder,
            IHealthCheck instance,
            TimeSpan cacheTime,
            string name = default, HealthStatus? failureStatus = default, IEnumerable<string> tags = default, TimeSpan? timeout = default)
        {
            if (instance == null)
                throw new ArgumentNullException("instance");

            var cachedInstance = new HlidacStatu.Web.HealthChecks.CachedResult(instance, cacheTime);

            return builder.Add(new HealthCheckRegistration(
                name,
                cachedInstance,
                failureStatus,
                tags,
                timeout));
        }

        public static IHealthChecksBuilder AddCachedHealthCheckWithOptions<T, TOptions>(
            this IHealthChecksBuilder builder,
            TimeSpan cacheTime,
            TOptions options,
            string name = default, HealthStatus? failureStatus = default, IEnumerable<string> tags = default, TimeSpan? timeout = default)
        where T : IHealthCheck
        {
            name = name ?? typeof(T).Name;
            T instance = (T)Activator.CreateInstance(typeof(T), options);

            return AddCachedHealthCheckWithOptions(builder,instance, cacheTime, name, failureStatus, tags, timeout);

        }

    }
}