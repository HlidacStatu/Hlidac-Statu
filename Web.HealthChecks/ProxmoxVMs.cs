using Microsoft.Extensions.Diagnostics.HealthChecks;

using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Corsinvest.ProxmoxVE.Api;
using Newtonsoft.Json;

namespace HlidacStatu.Web.HealthChecks
{
    public class ProxmoxVMs : IHealthCheck
    {

        private Options options;

        public class Options : IHCConfig
        {
            public string DefaultSectionName => "Proxmox.VMs";
            public string ServerUri { get; set; }
            public string NodeName { get; set; }
            public string ApiToken { get; set; }
            public string[] ExpectedRunningVMs { get; set; }
        }
        public class VM
        {
            public string vmid { get; set; }
            public int cpus { get; set; }
            public int disk { get; set; }
            public int diskwrite { get; set; }
            public long mem { get; set; }
            public float cpu { get; set; }
            public string status { get; set; }
            public long maxdisk { get; set; }
            public string name { get; set; }
            public long maxmem { get; set; }
            public string template { get; set; }
            public int uptime { get; set; }
            public string pid { get; set; }
            public long netin { get; set; }
            public int diskread { get; set; }
            public long netout { get; set; }
        }

        public ProxmoxVMs(Options options)
        {
            this.options = options;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                bool bad = false;
                StringBuilder result = new StringBuilder(1024);
                Uri url = new Uri(this.options.ServerUri);
                var client = new PveClient(url.Host, url.Port != 80 ? url.Port : 8006);

                client.ApiToken = this.options.ApiToken;

                var resp = await client.Nodes[this.options.NodeName].Qemu.Vmlist();
                if (resp.ResponseInError == false && resp.IsSuccessStatusCode)
                {
                    VM[] allVMs = JsonConvert.DeserializeObject<VM[]>(JsonConvert.SerializeObject(resp.Response.data));
                    foreach (var vm in (this.options.ExpectedRunningVMs ?? Array.Empty<string>()))
                    {
                        if (allVMs.Any(m => m.name.Equals(vm, StringComparison.InvariantCultureIgnoreCase)) == false)
                        {
                            bad = true;
                            result.AppendLine($"VM '{vm}' doesn't exists");
                        }
                        else if (allVMs
                            .Where(m => m.status == "running")
                            .Any(m => m.name.Equals(vm, StringComparison.InvariantCultureIgnoreCase)) == false)
                        {
                            bad = true;
                            result.AppendLine($"VM '{vm}' is stopped");
                        }
                        else if (allVMs
                            .Where(m => m.status == "running")
                            .Any(m => m.name.Equals(vm, StringComparison.InvariantCultureIgnoreCase)) == true)
                        {
                            //result.AppendLine($"VM '{vm}' is running");
                        }
                        else
                        {
                            bad = true;
                            result.AppendLine($"VM '{vm}' is in unknown state");
                        }

                    }

                    var nonListedVM = allVMs
                        .Where(m=>m.status=="running")
                        .Select(m => m.name)
                        .Except(this.options.ExpectedRunningVMs, System.StringComparer.InvariantCultureIgnoreCase);
                    
                    //if (nonListedVM.Count()>0)
                    //    result.AppendLine($"Other running VMs: {string.Join(',', nonListedVM)}");

                    if (bad)
                        return HealthCheckResult.Degraded(result.ToString());
                    else
                        return HealthCheckResult.Healthy(result.ToString());
                }
                else
                    return HealthCheckResult.Unhealthy();


            }
            catch (Exception e)
            {
                return HealthCheckResult.Unhealthy(exception: e);
            }

        }
    }
}
