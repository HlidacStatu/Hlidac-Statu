using HlidacStatu.AutocompleteApi.Controllers;
using HlidacStatu.AutocompleteApi.Services;
using HlidacStatu.LibCore.MiddleWares;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProtoBuf.Grpc.Server;

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
            services.AddSingleton<MemoryStoreService>(new MemoryStoreService());
            
            //add timer to refresh data once a day
            services.AddHostedService<TimedHostedService>();

            services.AddCodeFirstGrpc();
            services.AddControllers();
            
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRequestTrackMiddleware(new RequestTrackMiddleware.Options()
            {
                ApplicationName = "AutocompleteApi",
                MinimumRequestTimeToTrackMs = 10
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
                endpoints.MapGrpcService<AutocompleteGrpc>();
                endpoints.MapControllers();
            });
        }
    }
}