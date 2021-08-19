using Couchbase;

using Microsoft.Extensions.Diagnostics.HealthChecks;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HlidacStatu.Web.HealthChecks
{
    public class DockerContainer : IHealthCheck
    {

        private Options options;

        public class Options
        {
            public string DockerAPIUri { get; set; }
            public string[] ContainerNames { get; set; }
        }

        public DockerContainer(Options options)
        {
            this.options = options;
        }
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                using (Docker.DotNet.DockerClient cli = new Docker.DotNet.DockerClientConfiguration(
                    new Uri(options.DockerAPIUri), null, TimeSpan.FromSeconds(3)
                    )
                    .CreateClient())
                {

                    string result = "";
                    bool bad = false;
                    foreach (var containerName in options.ContainerNames)
                    {
                        Docker.DotNet.Models.ContainerState stat = null;
                        try
                        {
                            stat = cli.Containers.InspectContainerAsync(containerName).Result?.State;
                        }
                        catch (Docker.DotNet.DockerContainerNotFoundException)
                        {
                            bad = true;
                            result += $"{containerName}: doesnt exists\n";
                            stat = new Docker.DotNet.Models.ContainerState() {  ExitCode = -1 };
                        }
                        catch (System.AggregateException aex)
                        {
                            if (aex.InnerException?.GetType() == typeof(Docker.DotNet.DockerContainerNotFoundException))
                            {
                                bad = true;
                                result += $"{containerName}: doesn't exists\n";
                            }
                            else
                            {
                                bad = true;
                                result += $"{containerName}: error {aex.ToString()}\n";
                            }
                        }
                        catch (Exception e)
                        {
                            bad = true;
                            result += $"{containerName}: error {e.ToString()}\n";

                        }
                        if (stat == null)
                        {
                            bad = true;
                            result += $"{containerName}: docker not responding\n";
                        }
                        else if (stat.Running == false && stat.ExitCode != -1)
                        {
                            bad = true;
                            result += $"{containerName}: is {stat.Status}";
                            if (!string.IsNullOrEmpty(stat.Error)) result += $" Error {stat.Error}";
                            result += "\n";
                        }
                        else if (stat.ExitCode != -1)
                        {
                            result += $"{containerName}: is {stat.Status}\n";
                        }
                    }

                    if (bad)
                        return Task.FromResult(HealthCheckResult.Degraded(result));
                    else
                        return Task.FromResult(HealthCheckResult.Healthy(result));
                }
            }
            catch (Exception e)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(exception: e));
            }

        }
    }
}


