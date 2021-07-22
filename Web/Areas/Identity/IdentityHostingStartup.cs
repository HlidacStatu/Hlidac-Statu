using Microsoft.AspNetCore.Hosting;

[assembly: HostingStartup(typeof(HlidacStatu.Web.Areas.Identity.IdentityHostingStartup))]
namespace HlidacStatu.Web.Areas.Identity
{
    public class IdentityHostingStartup : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            builder.ConfigureServices((context, services) => {
            });
        }
    }
}