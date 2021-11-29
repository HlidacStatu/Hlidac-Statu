using System.Security.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace HlidacStatu.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseKestrel(kestrelServerOptions =>
                    {
                        kestrelServerOptions.ConfigureHttpsDefaults(adapterOptions =>
                        {
                            adapterOptions.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;
                        });
                    });
                    
                    webBuilder.UseStartup<Startup>();
                });
    }
}