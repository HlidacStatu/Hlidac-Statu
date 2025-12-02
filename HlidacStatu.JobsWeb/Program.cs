using System.Threading.Tasks;
using HlidacStatu.LibCore.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using WatchdogAnalytics.Services;

namespace WatchdogAnalytics
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var hostbuilder = CreateHostBuilder(args).Build();
            await JobService.RecalculateAsync();
            await hostbuilder.RunAsync();
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureHostForWeb(args)
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });
    }
}