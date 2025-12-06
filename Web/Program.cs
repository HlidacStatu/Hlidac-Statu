using HlidacStatu.LibCore.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Microsoft.Extensions.Logging;

namespace HlidacStatu.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            StartupLogger.Write("Application started.");
            AppContext.SetSwitch("Switch.Microsoft.Data.SqlClient.UseManagedNetworkingOnWindows", true);

            
            var builder = WebApplication.CreateBuilder(args);
            StartupLogger.Write("Builder created.");
            
            builder.ConfigureHostForWeb(args);
            var configuration = builder.Configuration;
            //inicializace statických proměnných
            System.Globalization.CultureInfo.DefaultThreadCurrentCulture = Util.Consts.czCulture;
            System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = Util.Consts.csCulture;
            StartupLogger.Write("Configuration loaded and Logger initialized.");
            Devmasters.Config.Init(configuration);
            StartupLogger.Write("Devmasters Init inited");

            builder.WebHost.UseStaticWebAssets();
            
#if DEBUG
            //dont check ssl for local debugging with local api
            System.Net.ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
#endif

            // Setup logging to be exported via OpenTelemetry
            _ = builder.Logging.AddOpenTelemetry(logging =>
            {
                logging.IncludeFormattedMessage = true;
                logging.IncludeScopes = true;
            });

            var OtlpEndpoint = Devmasters.Config.GetWebConfigValue("OTEL_EXPORTER_OTLP_ENDPOINT");


            var otel = builder.Services.AddOpenTelemetry()
                .ConfigureResource(b => b
                    .AddService(
                        serviceName: "Web",
                        serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown"
                    )
                )
                ;
            _ = otel.WithMetrics(metrics =>
            {
                // Metrics provider from OpenTelemetry
                metrics.AddAspNetCoreInstrumentation();
                metrics.AddRuntimeInstrumentation();
                //Our custom metrics
                //metrics.AddMeter(greeterMeter.Name);
                // Metrics provides by ASP.NET Core in .NET 8
                metrics.AddMeter("Microsoft.AspNetCore.Hosting");
                metrics.AddMeter("Microsoft.AspNetCore.Server.Kestrel");
                // Metrics provided by System.Net libraries
                metrics.AddMeter("System.Net.Http");
                metrics.AddMeter("System.Net.NameResolution");
                //metrics.AddMeter(SocialbannerInstrumentationSource.MeterName);
                metrics.AddFusionCacheInstrumentation(o => o.IncludeDistributedLevel = true);
            }
                );
            _ = otel.WithTracing(tracing =>
            {
                //tracing.AddSource(SocialbannerInstrumentationSource.ActivitySourceName);
                tracing.AddAspNetCoreInstrumentation();
                tracing.AddHttpClientInstrumentation();
                tracing.AddFusionCacheInstrumentation(o =>
                {
                    o.IncludeDistributedLevel = true;
                });

            });


            if (OtlpEndpoint != null)
            {
                _ = otel.UseOtlpExporter(
                        OtlpExportProtocol.Grpc,
                        new Uri(OtlpEndpoint)
                    );                            
            }

            //get IConfiguration
            StartupLogger.Write("Configuration 2 initialized.");
            
            
            
            DBUpgrades.DBUpgrader.UpgradeDatabases(Connectors.DirectDB.Instance.DefaultCnnStr);
            StartupLogger.Write("Db upgrader upgraded.");
            
            builder.Services.ConfigureServices(configuration);
            StartupLogger.Write("Services configured.");
            
            WebApplication app = builder.Build();
            StartupLogger.Write("App builded");
            
            app.ConfigurePipeline();
            StartupLogger.Write("Pipeline configured");
            
            app.Run();
        }
    }
}