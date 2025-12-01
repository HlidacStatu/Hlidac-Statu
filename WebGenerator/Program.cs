using HlidacStatu.LibCore.Extensions;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Reflection.PortableExecutable;
using System.Diagnostics.Metrics;
using HlidacStatu.WebGenerator.Models;

namespace Gen
{
    public class Program
    {
        public static void Main(string[] args)
        {
            System.Globalization.CultureInfo.DefaultThreadCurrentCulture = HlidacStatu.Util.Consts.czCulture;
            System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = HlidacStatu.Util.Consts.csCulture;

            var builder = WebApplication.CreateBuilder(args);
            builder.ConfigureHostForWeb(args);
            //inicializace statických proměnných
            Devmasters.Config.Init(builder.Configuration);

            builder.Services.AddSingleton<SocialbannerInstrumentationSource>();


            // Setup logging to be exported via OpenTelemetry
            _ = builder.Logging.AddOpenTelemetry(logging =>
            {
                logging.IncludeFormattedMessage = true;
                logging.IncludeScopes = true;
            });

            var OtlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];

            //builder.Services.Configure<OtlpExporterOptions>(
            //    o => o.Headers = $"x-otlp-api-key=Groggily-Chuck-Target7-Provable");

            var otel = builder.Services.AddOpenTelemetry()
                .ConfigureResource(b => b
                    .AddService(
                        serviceName: "WebGenerator",
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
                        metrics.AddMeter(SocialbannerInstrumentationSource.MeterName);
                        metrics.AddFusionCacheInstrumentation(o => o.IncludeDistributedLevel = true);
                    }
                );
            _ = otel.WithTracing(tracing =>
                {
                    tracing.AddSource(SocialbannerInstrumentationSource.ActivitySourceName);
                    tracing.AddAspNetCoreInstrumentation();
                    tracing.AddHttpClientInstrumentation();
                    tracing.AddFusionCacheInstrumentation(o =>
                    {
                        o.IncludeDistributedLevel = true;
                    });

                });


            if (OtlpEndpoint != null)
            {
                _ = otel.UseOtlpExporter();
            }



            // Add services to the container.
            builder.Services.AddControllersWithViews();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();



            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{action=Index}/{id?}",
                defaults: new { controller = "Home" });

            app.Run();
        }
    }
}
