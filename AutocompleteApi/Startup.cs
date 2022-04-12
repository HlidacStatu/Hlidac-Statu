using System;
using System.Net.Http;
using System.Net.Http.Headers;
using HlidacStatu.AutocompleteApi.Controllers;
using HlidacStatu.AutocompleteApi.Services;
using HlidacStatu.LibCore.MiddleWares;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polly;

namespace HlidacStatu.AutocompleteApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }


        public void ConfigureServices(IServiceCollection services)
        {
            //db setup
            Devmasters.Config.Init(Configuration);
            System.Globalization.CultureInfo.DefaultThreadCurrentCulture = Util.Consts.czCulture;
            System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = Util.Consts.csCulture;
            
            //start initializing during startup, not after first request
            // after first request call looks like this: services.AddSingleton<MemoryStoreService>();
            services.AddSingleton<IMemoryStoreService, MemoryStoreService>();
            services.AddSingleton<HlidacApiService>();
            
            //add timer to refresh data once a day
            services.AddHostedService<TimedHostedService>();

            services.AddControllers();

            
            services.Configure<HlidacApiOptions>(Configuration.GetSection(nameof(HlidacApiOptions)));
            var httpClientFactoryBuilder = services.AddHttpClient(AppConstants.HttpClientName)
                .AddTransientHttpErrorPolicy(policyBuilder =>
                    policyBuilder.WaitAndRetryAsync(
                        3, retryNumber => TimeSpan.FromMilliseconds(5000)));

            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                httpClientFactoryBuilder.ConfigurePrimaryHttpMessageHandler(c => new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback =
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                });
            }

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRequestTrackMiddleware(new RequestTrackMiddleware.Options()
            {
                ApplicationName = "AutocompleteApi",
                MinimumRequestTimeToTrackMs = 900
            });
            
            app.UseTimeMeasureMiddleware();
            
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();


            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}