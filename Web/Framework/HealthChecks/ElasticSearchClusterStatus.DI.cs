
using Microsoft.Extensions.Diagnostics.HealthChecks;

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ElasticSearchClusterStatusHealthCheckBuilderExtensions
    {
        const string NAME = "ElasticSearchClusterStatus";
        public static IHealthChecksBuilder AddElasticSearchClusterStatus(
            this IHealthChecksBuilder builder, 
            HlidacStatu.Web.Framework.HealthChecks.ElasticSearchClusterStatus.Options options, 
            string name = default, HealthStatus? failureStatus = default, IEnumerable<string> tags = default, TimeSpan? timeout = default)
        {

           
            return builder.Add(new HealthCheckRegistration(
                name ?? NAME,
                sp => new HlidacStatu.Web.Framework.HealthChecks.ElasticSearchClusterStatus(options),
                failureStatus,
                tags,
                timeout));
        }
    }
}