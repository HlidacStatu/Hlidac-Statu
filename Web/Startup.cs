using HlidacStatu.Entities;
using HlidacStatu.LibCore.MiddleWares;
using HlidacStatu.LibCore.Services;
using HlidacStatu.Web.Filters;
using HlidacStatu.Web.Framework;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace HlidacStatu.Web
{
    public class Startup
    {
        private bool _shouldRunHealthcheckFeature = false;

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

            string healthcheckFeatureOption = Devmasters.Config.GetWebConfigValue("RunHealthcheckFeature");
            _shouldRunHealthcheckFeature = !string.IsNullOrWhiteSpace(healthcheckFeatureOption) &&
                                           healthcheckFeatureOption == "true";

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
                    In = ParameterLocation.Header,
                    Scheme = "apiKey"
                });
                c.OperationFilter<AddApiAuthHeaderParameter>();

                //migrace: otestovat swagger
                // Set the comments path for the Swagger JSON and UI.
                var xmlFile = "HlidacStatu.Web.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
            });



            if (_shouldRunHealthcheckFeature)
                AddAllHealtChecks(services);

            //services.AddHealthChecksUI(set =>
            //       {
            //           set.AddHealthCheckEndpoint("Hlidac státu", "/health");
            //           set.SetHeaderText("Hlídač státu status page");
            //           set.MaximumHistoryEntriesPerEndpoint(50);

            //       }
            //    )
            //    .AddSqlServerStorage(Configuration["ConnectionStrings:HealthChecksConnection"]);


        }

        //Nastavení, jak budou zpracovány požadavky (Middleware).
        //!Záleží na pořadí
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

            //request time measurement
            app.UseTimeMeasureMiddleware();
                

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
                
                if (url?.ToLower()?.StartsWith("/cenypracehlidac") == true)
                {
                    context.Response.Redirect("https://cenyprace.hlidacstatu.cz/");
                    return;   // short circuit
                }
                

                await next();
            });

            app.UseRouting();

            app.UseAuthentication();
            app.Use(async (context, next) =>
            {
                var userName = context.User.Identity.Name;
                if (string.IsNullOrEmpty(userName) && context.Request.Path.StartsWithSegments("/api"))
                {
                    var authToken = context.GetAuthToken(); 
                    authToken = authToken.Replace("Token ", "").Trim();

                    ApplicationUser user = null;
                
                    if (!string.IsNullOrEmpty(authToken) && Guid.TryParse(authToken, out var guid))
                    {
                        using (DbEntities db = new())
                        {
                            
                            user = await db.Users.FromSqlInterpolated(
                                    $"select u.* from AspNetUsers u join AspNetUserApiTokens a on u.Id = a.Id where a.Token = {guid}")
                                .AsQueryable()
                                .FirstOrDefaultAsync();
                            
                        }

                        if (user != null)
                        {
                            var roles = user.GetRoles();
                            
                            var claims = new List<Claim> {
                                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                                new(ClaimTypes.Name, user.UserName),
                                new(ClaimTypes.Email, user.Email),
                            };
                            
                            foreach (var role in roles)
                            {
                                claims.Add(new Claim(ClaimTypes.Role, role));
                            }
                            
                            var identity = new ClaimsIdentity(claims, "Api");
                            var principal = new ClaimsPrincipal(identity);
                            context.User = principal;
                            
                            // var ticket = new AuthenticationTicket(principal, Scheme.Name);
                            // context.User.AddIdentity(identity);
                            // context.User
                        }
                    }
                }
                
                await next();
            });
            app.UseAuthorization();
            
            if (_shouldRunHealthcheckFeature)
            {
                app.UseHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions()
                {
                    Predicate = _ => true,
                    ResponseWriter = global::HealthChecks.UI.Client.UIResponseWriter.WriteHealthCheckUIResponse
                }).UseHealthChecks("/_health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions()
                {
                    Predicate = _ => true,
                });
                //app.UseHealthChecksUI(set =>
                //    {
                //        set.UIPath = "/status";
                //        set.AsideMenuOpened = false;
                //        set.AddCustomStylesheet("wwwroot\\content\\CustomHealthCheckUI.css");
                //    }
                //);
            }


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

                string[] scriptBundle = new[]
                {
                    "Scripts/respond.js",
                    "Scripts/hlidac.v1.11.js",
                    "Scripts/fuckadblock.min.js",
                    "Scripts/social-share-kit.min.js"
                };
                pipeline.AddJavaScriptBundle("/bundles/scriptbundle", scriptBundle);

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

            }).AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<DbEntities>();

            // this is needed because passwords are stored with old hashes
            services.Configure<PasswordHasherOptions>(options =>
                options.CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV2);
            
            // 401 and 403 responses instead of redirects for api - for [Authorize] attribute
            services.ConfigureApplicationCookie(o =>
            {
                o.Events = new CookieAuthenticationEvents()
                {
                    OnRedirectToLogin = (ctx) =>
                    {
                        if (ctx.Request.Path.StartsWithSegments("/api") && ctx.Response.StatusCode == 200)
                        {
                            ctx.Response.StatusCode = 401;
                        }

                        return Task.CompletedTask;
                    },
                    OnRedirectToAccessDenied = (ctx) =>
                    {
                        if (ctx.Request.Path.StartsWithSegments("/api") && ctx.Response.StatusCode == 200)
                        {
                            ctx.Response.StatusCode = 403;
                        }

                        return Task.CompletedTask;
                    }
                };
            });

            
        }
        
        private void AddAllHealtChecks(IServiceCollection services)
        {
            var conf = Configuration.GetSection("HealthChecks");

            services
                .AddHealthChecks()
                .AddProcessAllocatedMemoryHealthCheck(maximumMegabytesAllocated: 20000,
                    name: "Web server využitá pamět",
                    tags: new[] { "Web server", "process", "memory" })
                .AddHealthCheckWithResponseTime(
                    new global::HealthChecks.SqlServer.SqlServerHealthCheck(Configuration["ConnectionStrings:DefaultConnection"], "select top 1 username from AspNetUsers"),
                    "SQL server", HealthStatus.Unhealthy, tags: new[] { "DB", "db" }
                )
                .AddHealthCheckWithOptions<Web.HealthChecks.ElasticSearchClusterStatus, Web.HealthChecks.ElasticSearchClusterStatus.Options>(
                    new Web.HealthChecks.ElasticSearchClusterStatus.Options()
                    {
                        ExpectedNumberOfNodes = 16,
                        ElasticServerUris = Devmasters.Config.GetWebConfigValue("ESConnection").Split(';')
                    },
                    "Elastic cluster", tags: new[] { "DB", "elastic" })
                .AddHealthCheckWithOptions<Web.HealthChecks.ElasticSearchNodesFreeDisk, Web.HealthChecks.ElasticSearchNodesFreeDisk.Options>(
                    new Web.HealthChecks.ElasticSearchNodesFreeDisk.Options()
                    {
                        ExpectedNumberOfNodes = 16,
                        ElasticServerUris = Devmasters.Config.GetWebConfigValue("ESConnection").Split(';'),
                        MinimumFreeSpaceInMegabytes = 5000
                    },
                    "Elastic nodes disk space", tags: new[] { "DB", "elastic" })
                .AddHealthCheckWithOptions<Web.HealthChecks.NetworkDiskStorage, Web.HealthChecks.NetworkDiskStorage.Options>(
                    new Web.HealthChecks.NetworkDiskStorage.Options()
                    {
                        UNCPath = "c:\\",
                        DegradedMinimumFreeMegabytes = 10 * 1024, //10G 
                        UnHealthtMinimumFreeMegabytes = 1 * 1024 //1GB
                    },
                    "System disk", HealthStatus.Unhealthy, tags: new[] { "Web server" }
                )

                .AddHealthCheckWithOptions<Web.HealthChecks.NetworkDiskStorage, Web.HealthChecks.NetworkDiskStorage.Options>(
                    new Web.HealthChecks.NetworkDiskStorage.Options()
                    {
                        UNCPath = Devmasters.Config.GetWebConfigValue("FileCachePath"),
                        DegradedMinimumFreeMegabytes = 10 * 1024, //10G 
                        UnHealthtMinimumFreeMegabytes = 1 * 1024 //1GB
                    },
                    "Cache disk", HealthStatus.Unhealthy, tags: new[] { "Web server" }
                )
                .AddHealthCheckWithOptions<Web.HealthChecks.Couchbase, Web.HealthChecks.Couchbase.Options>(
                    new Web.HealthChecks.Couchbase.Options()
                    {
                        ServerUris = Devmasters.Config.GetWebConfigValue("CouchbaseServers").Split(','),
                        Bucket = Devmasters.Config.GetWebConfigValue("CouchbaseBucket"),
                        Username = Devmasters.Config.GetWebConfigValue("CouchbaseUsername"),
                        Password = Devmasters.Config.GetWebConfigValue("CouchbasePassword"),
                        Service = Web.HealthChecks.Couchbase.Service.KeyValue
                    },
                    "Couchbase", tags: new[] { "Cache" })
                .AddHealthCheckWithResponseTime(
                    new global::HealthChecks.Network.SmtpHealthCheck(new global::HealthChecks.Network.SmtpHealthCheckOptions()
                    {
                        Host = "10.10.100.60",
                        Port = 25,
                        ConnectionType = global::HealthChecks.Network.Core.SmtpConnectionType.PLAIN
                    }),
                    "SMTP", HealthStatus.Degraded, tags: new[] { "Web server" }
                )
                .AddCheck<Web.HealthChecks.OCRServer>("OCR servers", tags: new[] { "OCR cloud" })
                .AddCheck<Web.HealthChecks.OCRQueue>("OCR queues", tags: new[] { "OCR cloud" })
                .AddCheck<Web.HealthChecks.SmlouvyZpracovane>("Zpracované smlouvy", tags: new[] { "Data" })
                .AddCheck<Web.HealthChecks.VerejneZakazkyZpracovane>("Zpracované VZ", tags: new[] { "Data" })
                .AddHealthCheckWithOptions<Web.HealthChecks.DatasetZpracovane, Web.HealthChecks.DatasetZpracovane.Options>(
                    new Web.HealthChecks.DatasetZpracovane.Options()
                    {
                        DatasetId = "vyjadreni-politiku",
                        MinRecordsInInterval = 100,
                        Interval = HealthChecks.DatasetZpracovane.IntervalEnum.Day
                    }, "Dataset Vyjadření politiků", HealthStatus.Unhealthy, tags: new[] { "Data" })
                .AddHealthCheckWithOptions<Web.HealthChecks.DatasetZpracovane, Web.HealthChecks.DatasetZpracovane.Options>(
                    new Web.HealthChecks.DatasetZpracovane.Options()
                    {
                        DatasetId = "veklep",
                        MinRecordsInInterval = 30,
                        Interval = HealthChecks.DatasetZpracovane.IntervalEnum.Week
                    }, "Dataset VEKLEP", HealthStatus.Unhealthy, tags: new[] { "Data" })
                .AddHealthCheckWithOptions<Web.HealthChecks.DatasetZpracovane, Web.HealthChecks.DatasetZpracovane.Options>(
                    new Web.HealthChecks.DatasetZpracovane.Options()
                    {
                        DatasetId = "rozhodnuti-uohs",
                        MinRecordsInInterval = 10,
                        Interval = HealthChecks.DatasetZpracovane.IntervalEnum.Week
                    }, "Dataset Rozhodnuti UOHS", HealthStatus.Unhealthy, tags: new[] { "Data" })
                .AddHealthCheckWithOptions<Web.HealthChecks.DatasetyStatistika, Web.HealthChecks.DatasetyStatistika.Options>(
                    new Web.HealthChecks.DatasetyStatistika.Options()
                    {
                        Exclude = new string[] { "rozhodnuti-uohs", "veklep", "vyjadreni-politiku" },
                        Interval = HealthChecks.DatasetyStatistika.IntervalEnum.Month
                    }, "Statistiky malých databází", HealthStatus.Unhealthy, tags: new[] { "Data" })
                .AddHealthCheckWithOptions<Web.HealthChecks.DockerContainer, Web.HealthChecks.DockerContainer.Options>(
                    new HealthChecks.HCConfig<HealthChecks.DockerContainer.Options>(conf, "Docker.Containers.100.145").ConfigData,
                    "Docker .145", HealthStatus.Unhealthy, tags: new[] { "Docker" })
                .AddHealthCheckWithOptions<Web.HealthChecks.DockerContainer, Web.HealthChecks.DockerContainer.Options>(
                    new HealthChecks.HCConfig<HealthChecks.DockerContainer.Options>(conf, "Docker.Containers.100.146").ConfigData,
                    "Docker .146", HealthStatus.Unhealthy, tags: new[] { "Docker" })
                .AddHealthCheckWithOptions<Web.HealthChecks.DockerContainer, Web.HealthChecks.DockerContainer.Options>(                    
                    new HealthChecks.HCConfig<HealthChecks.DockerContainer.Options>(conf, "Docker.Containers.150.200").ConfigData,
                    "Docker .200", HealthStatus.Unhealthy, tags: new[] { "Docker" })
                .AddHealthCheckWithOptions<Web.HealthChecks.DockerContainer, Web.HealthChecks.DockerContainer.Options>(
                    new HealthChecks.HCConfig<HealthChecks.DockerContainer.Options>(conf, "Docker.Containers.150.201").ConfigData,
                    "Docker .201", HealthStatus.Unhealthy, tags: new[] { "Docker" })
                .AddHealthCheckWithOptions<Web.HealthChecks.DockerContainer, Web.HealthChecks.DockerContainer.Options>(
                    new HealthChecks.HCConfig<HealthChecks.DockerContainer.Options>(conf, "Docker.Containers.150.204").ConfigData,
                    "Docker .204", HealthStatus.Unhealthy, tags: new[] { "Docker" })

                .AddHealthCheckWithOptions<Web.HealthChecks.CamelotApis, Web.HealthChecks.CamelotApis.Options>(
                    new HealthChecks.HCConfig<HealthChecks.CamelotApis.Options>(conf).ConfigData,
                    "Camelot APIs", HealthStatus.Unhealthy, tags: new[] { "Docker" })

                .AddHealthCheckWithOptions<Web.HealthChecks.ProxmoxVMs, Web.HealthChecks.ProxmoxVMs.Options>(
                    new HealthChecks.HCConfig<HealthChecks.ProxmoxVMs.Options>(conf,"Proxmox.VM.100.100").ConfigData,
                    "Proxmox 100.100", HealthStatus.Unhealthy, tags: new[] { "VMs" })
                .AddHealthCheckWithOptions<Web.HealthChecks.ProxmoxVMs, Web.HealthChecks.ProxmoxVMs.Options>(
                    new HealthChecks.HCConfig<HealthChecks.ProxmoxVMs.Options>(conf, "Proxmox.VM.100.101").ConfigData,
                    "Proxmox 100.101", HealthStatus.Unhealthy, tags: new[] { "VMs" })
                .AddHealthCheckWithOptions<Web.HealthChecks.ProxmoxVMs, Web.HealthChecks.ProxmoxVMs.Options>(
                    new HealthChecks.HCConfig<HealthChecks.ProxmoxVMs.Options>(conf, "Proxmox.VM.100.102").ConfigData,
                    "Proxmox 100.102", HealthStatus.Unhealthy, tags: new[] { "VMs" })
                .AddHealthCheckWithOptions<Web.HealthChecks.ProxmoxVMs, Web.HealthChecks.ProxmoxVMs.Options>(
                    new HealthChecks.HCConfig<HealthChecks.ProxmoxVMs.Options>(conf, "Proxmox.VM.pve-hs-01-r540").ConfigData,
                    "Proxmox pve-hs-01-r540 (02.161)", HealthStatus.Unhealthy, tags: new[] { "VMs" })
                .AddHealthCheckWithOptions<Web.HealthChecks.ProxmoxVMs, Web.HealthChecks.ProxmoxVMs.Options>(
                    new HealthChecks.HCConfig<HealthChecks.ProxmoxVMs.Options>(conf, "Proxmox.VM.pve-hs-02-r720xd").ConfigData,
                    "Proxmox pve-hs-02-r720xd (02.167)", HealthStatus.Unhealthy, tags: new[] { "VMs" })
                .AddHealthCheckWithOptions<Web.HealthChecks.ProxmoxVMs, Web.HealthChecks.ProxmoxVMs.Options>(
                    new HealthChecks.HCConfig<HealthChecks.ProxmoxVMs.Options>(conf, "Proxmox.VM.pve-nic-168").ConfigData,
                    "Proxmox pve-nic-168 (02.168)", HealthStatus.Unhealthy, tags: new[] { "VMs" })
                //.AddHealthCheckWithOptions<Web.HealthChecks.ProxmoxVMs, Web.HealthChecks.ProxmoxVMs.Options>(
                //    new HealthChecks.HCConfig<HealthChecks.ProxmoxVMs.Options>(conf, "Proxmox.VM.hs-h-01").ConfigData,
                //    "Proxmox hs-h-01 (02.160)", HealthStatus.Unhealthy, tags: new[] { "VMs" })
                ;
        }

    }
}