using System.IO;
using HlidacStatu.AutocompleteApi.Services;
using HlidacStatu.Connectors;
using HlidacStatu.LibCore.MiddleWares;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
            
            //todo: hodil by se refactoring
            // Aktuálně je FirmaRepo.Cached závislá na určitých souborech
            // podle mě by se tato závislost měla odstranit
            FixMissingData();
            
            //start initializing during startup, not after first request
            // after first request call looks like this: services.AddSingleton<MemoryStoreService>();
            services.AddSingleton<Caches>(new Caches());
           
            services.AddControllers();

        }

        private void FixMissingData()
        {
            //tenhle soubor by šel ve firmarepo.cached nahradit načtením dat z tabulky o ovm: OrganVerejneMoci
            File.Copy("./DS_OVM.xml", Path.Combine(Init.WebAppDataPath, "DS_OVM.xml"), true);
            
            //
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRequestTrackMiddleware(new RequestTrackMiddleware.Options()
            {
                ApplicationName = "AutocompleteApi",
                MinimumRequestTimeToTrackMs = 900
            });

            var timeMeasureLogger = Devmasters.Log.Logger.CreateLogger("HlidacStatu.AutocompleteApi.ResponseTimes",
                Devmasters.Log.Logger.DefaultConfiguration()
                    .Enrich.WithProperty("codeversion",
                        System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString()));
            
            app.UseTimeMeasureMiddleware(timeMeasureLogger);
            
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            //app.UseHttpsRedirection();

            app.UseRouting();


            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}