using Microsoft.AspNetCore.Hosting;

[assembly: HostingStartup(typeof(HlidacStatu.JobsWeb.Areas.Identity.IdentityHostingStartup))]
namespace HlidacStatu.JobsWeb.Areas.Identity
{
    public class IdentityHostingStartup : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            builder.ConfigureServices((context, services) =>
            {
            });
        }
    }
}