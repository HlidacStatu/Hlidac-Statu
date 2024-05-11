using Corsinvest.ProxmoxVE.Api;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
            public string[] ExpectedRunningVMs { get; set; } = new string[0];
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
                bool warn = false;
                bool unhealth = false;
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
                            warn = true;
                            result.AppendLine($"VM '{vm}' doesn't exists");
                        }
                        else if (allVMs
                            .Where(m => m.status == "running")
                            .Any(m => m.name.Equals(vm, StringComparison.InvariantCultureIgnoreCase)) == false)
                        {
                            warn = true;
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
                            warn = true;
                            result.AppendLine($"VM '{vm}' is in unknown state");
                        }

                    }


                    //check CPU & Memory
                    foreach (var vm in allVMs)
                    {
                        if (vm.cpu > 0.9)
                        {
                            unhealth = true;
                            result.AppendLine($"VM '{vm.name}' {vm.cpu:P1} exhausted CPU." );
                        }
                        if (vm.mem > (vm.maxmem * .93))
                        {
                            warn = true;
                            result.AppendLine($"VM '{vm.name}' {(vm.mem / 1_000_000_000):N2} / {(vm.mem / 1_000_000_000):N2} exhausted Memory.");

                        }
                        if (vm.mem > (vm.maxmem*.98))
                        {
                            unhealth = true;
                            result.AppendLine($"VM '{vm.name}' {(vm.mem/1_000_000_000):N2} / {(vm.mem / 1_000_000_000):N2} exhausted Memory.");
                        }
                    }

                    //if (nonListedVM.Count()>0)
                    //    result.AppendLine($"Other running VMs: {string.Join(',', nonListedVM)}");
                    if (unhealth)
                        return HealthCheckResult.Unhealthy(result.ToString());
                    if (warn)
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
