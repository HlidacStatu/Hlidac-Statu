using System;
using System.IO;
using HlidacStatu.Entities;
using HlidacStatu.LibCore.MiddleWares;
using HlidacStatu.LibCore.Services;
using HlidacStatu.Web.Filters;
using HlidacStatu.Web.Framework;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HlidacStatu.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
        {
            Configuration = configuration;
            WebHostEnvironment = webHostEnvironment;
        }

        public IWebHostEnvironment WebHostEnvironment { get; }
        public IConfiguration Configuration { get; }

        //Globální konfiguraci a nastavení sem
        public void ConfigureServices(IServiceCollection services)
        {
            //inicializace statických proměnných
            Devmasters.Config.Init(Configuration);

            System.Globalization.CultureInfo.DefaultThreadCurrentCulture = Util.Consts.czCulture;
            System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = Util.Consts.csCulture;


            DBUpgrades.DBUpgrader.UpgradeDatabases(Connectors.DirectDB.DefaultCnnStr);

            string connectionString = Configuration.GetConnectionString("DefaultConnection");
            // for scoped services (mainly for identity)
            services.AddDbContext<DbEntities>(options =>
                options.UseSqlServer(connectionString));
            services.AddDatabaseDeveloperPageExceptionFilter();

            AddIdentity(services);
            AddBundling(services);

            if (Constants.IsDevelopment(WebHostEnvironment))
            {
                services.AddControllersWithViews()
                    .AddNewtonsoftJson()
                    .AddRazorRuntimeCompilation();
            }
            else
            {
                services.AddControllersWithViews()
                    .AddNewtonsoftJson();
            }

            services.AddRazorPages()
                .AddMvcOptions(options =>
                    options.Filters.Add<SpamProtectionRazor>());

            services.AddSingleton<AttackerDictionaryService>(); //migrace: Přejmenovat attackerDictionaryService

            services.AddApplicationInsightsTelemetry();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v2", new OpenApiInfo
                {
                    Version = "v2",
                    Title = "HlidacStatu Api V2.1.1",
                    Description = "REST API Hlídače státu",
                    TermsOfService = new Uri("https://www.hlidacstatu.cz/texty/provoznipodminky/"),
                    Contact = new OpenApiContact
                    {
                        Name = "Hlídač státu",
                        Email = "podpora@hlidacstatu.cz",
                        Url = new Uri("https://www.hlidacstatu.cz/texty/kontakt/"),
                    },
                    License = new OpenApiLicense
                    {
                        Name = "CC BY 3.0 CZ",
                        Url = new Uri("https://www.hlidacstatu.cz/texty/licence/"),
                    }
                });

                c.AddSecurityDefinition("apiKey", new OpenApiSecurityScheme()
                {
                    Type = SecuritySchemeType.ApiKey,
                    Description = "API Key Authentication",
                    Name = "Authorization",
                    In = ParameterLocation.Header
                });

                //migrace: otestovat swagger
                // Set the comments path for the Swagger JSON and UI.
                var xmlFile = "HlidacStatu.Web.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
            });

            AddAllHealtChecks(services);

            services.AddHealthChecksUI(set =>
                   {
                       set.AddHealthCheckEndpoint("Hlidac státu", "/health");
                       set.SetHeaderText("Hlídač státu status page");
                       set.MaximumHistoryEntriesPerEndpoint(50);
                       
                   }
                )
                .AddSqlServerStorage(Configuration["ConnectionStrings:HealthChecksConnection"]);


        }

        //Nastavení, jak budou zpracovány požadavky (Middleware).
        //!Záleží na pořadí
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

            if (Constants.IsDevelopment(env))
            {
                app.UseDeveloperExceptionPage();
                app.UseMigrationsEndPoint();
            }
            else if (Devmasters.Config.GetWebConfigValue("DeveloperExceptionPage") == "true")
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseBannedIpsMiddleware(); // tohle nechci při developmentu :) 
                app.UseExceptionHandler("/Error/500");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseWebOptimizer();
            app.UseStatusCodePagesWithReExecute("/error/{0}");
            app.UseHttpsRedirection();

            app.UseOnHTTPErrorMiddleware();

            app.UseStaticFiles();

            app.UseSwagger();
            //app.UseSwaggerUI();
            // app.UseSwagger(s => s.RouteTemplate = "api/{documentName}/swagger/swagger.json");
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v2/swagger.json", "API V2");
                //c.RoutePrefix = "api/v2/swagger";
            });


            //redirect rules
            app.Use(async (context, next) =>
            {
                var url = context.Request.Path.Value;

                // Redirect to an external URL
                if (url?.ToLower()?.StartsWith("/account/") == true)
                {
                    context.Response.Redirect("https://www.hlidacstatu.cz/Identity" + url + context.Request.QueryString.Value);
                    return;   // short circuit
                }

                await next();
            });

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions()
            {
                Predicate = _ => true,
                ResponseWriter = global::HealthChecks.UI.Client.UIResponseWriter.WriteHealthCheckUIResponse
            });
            app.UseHealthChecksUI(set =>
                {
                    set.UIPath = "/status";
                    set.AsideMenuOpened = false;
                    set.AddCustomStylesheet("wwwroot\\content\\CustomHealthCheckUI.css");
                }
            );

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");

                endpoints.MapControllerRoute(
                    name: "home",
                    pattern: "{action}/{id?}",
                    defaults: new { controller = "Home" });

                endpoints.MapControllerRoute(
                    name: "directIndex",
                    pattern: "{controller}/{id?}",
                    defaults: new { action = "Index" });

                endpoints.MapControllerRoute(
                    name: "DataController",
                    pattern: "Data/{action}/{id?}/{dataid?}",
                    defaults: new { controller = "Data", action = "Index" });

                endpoints.MapRazorPages();
            });


            //force init autocomplete cache
            new System.Threading.Thread(() =>
            {
                try
                {
                    using (Devmasters.Net.HttpClient.URLContent net = new Devmasters.Net.HttpClient.URLContent("https://www.hlidacstatu.cz/api/autocomplete/?q=flakan&term=flakan&_type=query&q=flakan"))
                    {
                        net.Timeout = 3 * 60000;
                        var s = net.GetContent();
                    }

                }
                catch (Exception)
                {
                }
            }).Start();


        }

        private void AddBundling(IServiceCollection services)
        {
            services.AddWebOptimizer(pipeline =>
            {
                string[] cssPaths = new[]
                {
                    "wwwroot/Content/bootstrap.css",
                    "wwwroot/Content/GlobalSite.v1.10.css",
                    "wwwroot/Content/social-share-kit.css",
                    "wwwroot/Content/new.v1.10.css"
                };
                pipeline.AddCssBundle("/Content/bundled.css", cssPaths)
                    .UseContentRoot() // tohle je tady potřeba, protože při standardním použití se špatně generují relativní cesty ve stylech (bootstrap.css)
                    .AdjustRelativePaths(); //tohle je tady potřeba, aby výsledné cesty neobsahovaly /wwwroot/


                pipeline.AddJavaScriptBundle("/bundles/jquery", "Scripts/jquery-1.11.3.min.js");
                pipeline.AddJavaScriptBundle("/bundles/jqueryval", "Scripts/jquery.validate*");

                pipeline.AddJavaScriptBundle("/bundles/modernizr", "Scripts/modernizr-2.8.3.js");

                string[] bootstrapPaths = new[]
                {
                    "Scripts/bootstrap.js",
                    "Scripts/respond.js",
                    "Scripts/hlidac.v1.11.js",
                    //"Scripts/autocomplete.v1.1.js",
                    "Scripts/fuckadblock.min.js",
                    "Scripts/social-share-kit.min.js"
                };
                pipeline.AddJavaScriptBundle("/bundles/bootstrap", bootstrapPaths);

                pipeline.AddJavaScriptBundle("/bundles/highcharts",
                    "Scripts/Highcharts-6/js/highcharts.js",
                    "Scripts/highcharts.global.options.js");

                pipeline.AddJavaScriptBundle("/bundles/highcharts8",
                    "Scripts/Highcharts-8/js/highcharts.js",
                    "Scripts/highcharts.global.options.js");

                pipeline.AddJavaScriptBundle("/bundles/typeahead",
                    "Scripts/typeahead.bundle.min.js",
                    "Scripts/bloodhound.min.js");

            });
        }


        private void AddIdentity(IServiceCollection services)
        {
            services.AddDefaultIdentity<ApplicationUser>(options =>
            {
                options.SignIn.RequireConfirmedAccount = true;

                options.User.RequireUniqueEmail = true;

                options.Password.RequiredLength = 6;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;

                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.MaxFailedAccessAttempts = 5;

            })
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<DbEntities>();

            // this is needed because passwords are stored with old hashes
            services.Configure<PasswordHasherOptions>(options =>
                options.CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV2
                )
            ;
        }

        private void AddAllHealtChecks(IServiceCollection services)
        {
            services
                .AddHealthChecks()

                .AddProcessAllocatedMemoryHealthCheck(maximumMegabytesAllocated: 20000, name: "Web server allocated memory", tags: new[] { "webserver", "process", "memory" })

                .AddHealthCheckWithResponseTime(
                    new global::HealthChecks.SqlServer.SqlServerHealthCheck(Configuration["ConnectionStrings:DefaultConnection"], "select top 1 username from AspNetUsers"), 
                    "SQL server", HealthStatus.Unhealthy, tags: new[] { "network", "db" }
                )

                .AddHealthCheckWithOptions<Web.HealthChecks.ElasticSearchClusterStatus, Web.HealthChecks.ElasticSearchClusterStatus.Options>(
                    new Web.HealthChecks.ElasticSearchClusterStatus.Options()
                        {
                             ExpectedNumberOfNodes = 16,
                             ElasticServerUris = Devmasters.Config.GetWebConfigValue("ESConnection").Split(';')
                        }
                    ,"Elastic cluster", tags: new[] { "network", "elastic" })

                .AddHealthCheckWithOptions<Web.HealthChecks.ElasticSearchNodesFreeDisk, Web.HealthChecks.ElasticSearchNodesFreeDisk.Options>(
                    new Web.HealthChecks.ElasticSearchNodesFreeDisk.Options()
                    {
                        ExpectedNumberOfNodes = 16,
                        ElasticServerUris = Devmasters.Config.GetWebConfigValue("ESConnection").Split(';'),
                        MinimumFreeSpaceInPercent = 15m
                    }
                    , "Elastic nodes disk space", tags: new[] { "network", "elastic" })

                .AddHealthCheckWithOptions<Web.HealthChecks.NetworkDiskStorage, Web.HealthChecks.NetworkDiskStorage.Options>(
                    new Web.HealthChecks.NetworkDiskStorage.Options()
                        {
                            UNCPath = "c:\\",
                            DegradedMinimumFreeMegabytes = 10 * 1024, //10G 
                            UnHealthtMinimumFreeMegabytes = 1 * 1024 //1GB
                        }, "System disk", HealthStatus.Unhealthy, tags: new[] { "webserver" }
                )

                .AddHealthCheckWithOptions<Web.HealthChecks.NetworkDiskStorage, Web.HealthChecks.NetworkDiskStorage.Options>(
                    new Web.HealthChecks.NetworkDiskStorage.Options()
                        {
                            UNCPath = Devmasters.Config.GetWebConfigValue("FileCachePath"),
                            DegradedMinimumFreeMegabytes = 10 * 1024, //10G 
                            UnHealthtMinimumFreeMegabytes= 1 * 1024 //1GB
                        }, "Cache disk", HealthStatus.Unhealthy, tags: new[] { "webserver" }
                )

                .AddHealthCheckWithOptions<Web.HealthChecks.Couchbase, Web.HealthChecks.Couchbase.Options>(
                    new Web.HealthChecks.Couchbase.Options() 
                        {
                            ServerUris = Devmasters.Config.GetWebConfigValue("CouchbaseServers").Split(','),
                            Bucket= Devmasters.Config.GetWebConfigValue("CouchbaseBucket"),
                            Username= Devmasters.Config.GetWebConfigValue("CouchbaseUsername"),
                            Password= Devmasters.Config.GetWebConfigValue("CouchbasePassword"),
                            Service = Web.HealthChecks.Couchbase.Service.KeyValue
                        }
                    , "Couchbase", tags: new[] { "cache" })

                .AddHealthCheckWithResponseTime(
                    new global::HealthChecks.Network.SmtpHealthCheck(new global::HealthChecks.Network.SmtpHealthCheckOptions()
                    {
                        Host = "10.10.100.60",
                        Port= 25,
                        ConnectionType = global::HealthChecks.Network.Core.SmtpConnectionType.PLAIN
                    }),
                    "SMTP", HealthStatus.Degraded, tags: new[] { "network" }
                )

                .AddCheck<Web.HealthChecks.OCRServer>("OCR servers", tags: new[] { "OCR cloud" })
                .AddCheck<Web.HealthChecks.OCRQueue>("OCR queues", tags: new[] { "OCR cloud" })
                .AddHealthCheckWithOptions<Web.HealthChecks.DockerContainer, Web.HealthChecks.DockerContainer.Options>(
                    new Web.HealthChecks.DockerContainer.Options()
                        {
                            DockerAPIUri = Devmasters.Config.GetWebConfigValue("Docker.HealthCheck.100.145").Split(';')[0],
                            ContainerNames = Devmasters.Config.GetWebConfigValue("Docker.HealthCheck.100.145").Split(';')[1].Split(','),                            
                        }, "Docker .145", HealthStatus.Unhealthy, tags: new[] { "docker" })
                .AddHealthCheckWithOptions<Web.HealthChecks.DockerContainer, Web.HealthChecks.DockerContainer.Options>(
                    new Web.HealthChecks.DockerContainer.Options()
                    {
                        DockerAPIUri = Devmasters.Config.GetWebConfigValue("Docker.HealthCheck.100.146").Split(';')[0],
                        ContainerNames = Devmasters.Config.GetWebConfigValue("Docker.HealthCheck.100.146").Split(';')[1].Split(','),
                    }, "Docker .146", HealthStatus.Unhealthy, tags: new[] { "docker" })
                .AddHealthCheckWithOptions<Web.HealthChecks.DockerContainer, Web.HealthChecks.DockerContainer.Options>(
                    new Web.HealthChecks.DockerContainer.Options()
                    {
                        DockerAPIUri = Devmasters.Config.GetWebConfigValue("Docker.HealthCheck.150.200").Split(';')[0],
                        ContainerNames = Devmasters.Config.GetWebConfigValue("Docker.HealthCheck.150.200").Split(';')[1].Split(','),
                    }, "Docker .200", HealthStatus.Unhealthy, tags: new[] { "docker" })
                .AddHealthCheckWithOptions<Web.HealthChecks.DockerContainer, Web.HealthChecks.DockerContainer.Options>(
                    new Web.HealthChecks.DockerContainer.Options()
                    {
                        DockerAPIUri = Devmasters.Config.GetWebConfigValue("Docker.HealthCheck.150.201").Split(';')[0],
                        ContainerNames = Devmasters.Config.GetWebConfigValue("Docker.HealthCheck.150.201").Split(';')[1].Split(','),
                    }, "Docker .201", HealthStatus.Unhealthy, tags: new[] { "docker" })
                ;
        }

    }
}