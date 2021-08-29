
using Microsoft.Extensions.Diagnostics.HealthChecks;

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace HlidacStatu.Web.HealthChecks
{
    public class NetworkDiskStorage : IHealthCheck
    {
        private Options options;

        public enum Service
        {
            KeyValue,
            Views,
            Query,
            Search,
            Config,
            Analytics
        }

        public class Options
        {
            public string UNCPath { get; set; }
            public long UnHealthtMinimumFreeMegabytes { get; set; } = 100;
            public long DegradedMinimumFreeMegabytes { get; set; } = 10;
        }

        public NetworkDiskStorage(Options options)
        {
            this.options = options;
        }
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            ulong uFreeBytesAvailable;
            ulong uTotalNumberOfBytes;
            ulong uTotalNumberOfFreeBytes;

            try
            {
                bool success = GetDiskFreeSpaceEx(options.UNCPath,
                      out uFreeBytesAvailable,
                      out uTotalNumberOfBytes,
                      out uTotalNumberOfFreeBytes);
                if (!success)
                    return Task.FromResult(HealthCheckResult.Degraded("Unknown status, cannot read data from network disk"));
                //throw new System.ComponentModel.Win32Exception();

                decimal freeMBytesAvailable = (decimal)((decimal)uFreeBytesAvailable / 1048576m); //1024*2014
                decimal totalMBytes = (decimal)((decimal)uTotalNumberOfBytes / 1048576m); //1024*2014

                decimal percentFree = freeMBytesAvailable / totalMBytes;
                string report = $"Free space: {freeMBytesAvailable:N0}MB, {percentFree:P2} of total";

                if (freeMBytesAvailable < options.UnHealthtMinimumFreeMegabytes)
                    return Task.FromResult(HealthCheckResult.Unhealthy(description: report));

                else if (freeMBytesAvailable < options.DegradedMinimumFreeMegabytes)
                    return Task.FromResult(HealthCheckResult.Degraded(description: report));

                else
                    return Task.FromResult(HealthCheckResult.Healthy(description: report));
            }
            catch (Exception e)
            {
                return Task.FromResult(HealthCheckResult.Degraded("Unknown status, cannot read data from network disk", e));
            }


        }



        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetDiskFreeSpaceEx(string lpDirectoryName,
   out ulong lpFreeBytesAvailable,
   out ulong lpTotalNumberOfBytes,
   out ulong lpTotalNumberOfFreeBytes);

    }
}


