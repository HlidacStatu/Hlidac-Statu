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
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Devmasters.Log;
using HlidacStatu.Entities.Entities;
using HlidacStatu.LibCore.Filters;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

using Polly;


namespace HlidacStatu.Web
{
    public class Startup
    {
        private bool _shouldRunHealthcheckFeature = false;

        public Startup(IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
        {
            Configuration = configuration;
            WebHostEnvironment = webHostEnvironment;

#if DEBUG
            //dont check ssl for local debugging with local api
            System.Net.ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
#endif

        }

        public IWebHostEnvironment WebHostEnvironment { get; }
        public IConfiguration Configuration { get; }

        //Globální konfiguraci a nastavení sem
        public void ConfigureServices(IServiceCollection services)
        {
            //inicializace statických proměnných
            Devmasters.Config.Init(Configuration);
            HlidacStatu.Util.Consts.Logger.Info("{action} {code}.", "starting", "web");

#if DEBUG
            //if (System.Diagnostics.Debugger.IsAttached)
            //    System.Net.Http.HttpClient.DefaultProxy = new System.Net.WebProxy("127.0.0.1", 8888);
#endif

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
            
            // Add a DbContext to store your Database Keys
            services.AddDbContext<HlidacKeysContext>(options =>
                options.UseSqlServer(connectionString));

            // using Microsoft.AspNetCore.DataProtection;
            services.AddDataProtection()
                .PersistKeysToDbContext<HlidacKeysContext>()
                .SetApplicationName("HlidacStatu");

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

            services.AddSingleton<AttackerDictionaryService>();

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
                c.OperationFilter<ApiAuthHeaderParameter>();

                // Set the comments path for the Swagger JSON and UI.
                var xmlFile = "HlidacStatu.Web.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
            });


            services.AddHttpClient(Constants.DefaultHttpClient)
                .AddTransientHttpErrorPolicy(policyBuilder =>
                    policyBuilder.WaitAndRetryAsync(
                        3, retryNumber => TimeSpan.FromMilliseconds(10)));
            

            if (_shouldRunHealthcheckFeature)
                AddAllHealtChecks(services);

        }

        //Nastavení, jak budou zpracovány požadavky (Middleware).
        //!Záleží na pořadí
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRequestTrackMiddleware(new RequestTrackMiddleware.Options()
            {
                LimitToPaths = new List<string> {"/api"},
                ApplicationName = "WEB"
            });
            
            var timeMeasureLogger = Devmasters.Log.Logger.CreateLogger("HlidacStatu.PageTimes",
                Devmasters.Log.Logger.DefaultConfiguration()
                    .Enrich.WithProperty("codeversion", System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString())
                    //.AddLogStash(new Uri("http://10.10.150.203:5000"))
                    .AddFileLoggerFilePerLevel("c:/Data/Logs/HlidacStatu/Web.PageTimes", "slog.txt",
                        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {SourceContext} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                        rollingInterval: Serilog.RollingInterval.Day,
                        fileSizeLimitBytes: null,
                        retainedFileCountLimit: 9,
                        shared: true
                    ));
            
            //request time measurement
            app.UseTimeMeasureMiddleware(timeMeasureLogger);
                

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

            Devmasters.Log.Logger webExceptionLogger = Devmasters.Log.Logger.CreateLogger("Web.Exceptions",
                Devmasters.Log.Logger.DefaultConfiguration()
                    .Enrich.WithProperty("codeversion", System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString())
                    //.AddLogStash(new Uri("http://10.10.150.203:5000"))
                    .AddFileLoggerFilePerLevel("c:/Data/Logs/HlidacStatu/Web", "slog.txt",
                        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {SourceContext} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                        rollingInterval: Serilog.RollingInterval.Day,
                        fileSizeLimitBytes: null,
                        retainedFileCountLimit: 9,
                        shared: true
                    ));
            app.UseOnHTTPErrorMiddleware(webExceptionLogger);

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
                    context.Response.Redirect("https://www.ceny.analyzy.hlidacstatu.cz/");
                    return;   // short circuit
                }
                
                if (url?.ToLower()?.StartsWith("/jobtableeditor") == true)
                {
                    context.Response.Redirect("https://jobtableeditor.hlidacstatu.cz/");
                    return;   // short circuit
                }
                

                await next(context);
            });

            app.UseRouting();

            app.UseAuthentication();
            app.UseApiAuthenticationMiddleware();
            
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

                // ReSharper disable once Mvc.ActionNotResolved
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


           

        }

        private void AddBundling(IServiceCollection services)
        {
            services.AddWebOptimizer(pipeline =>
            {
                string[] cssPaths = new[]
                {
                    "wwwroot/Content/GlobalSite.v1.14.css",
                    "wwwroot/Content/social-share-kit.css",
                    "wwwroot/Content/new.v1.14.css"
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
                    "Scripts/hlidac.v1.12.js",
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
                o.Cookie.Domain = ".hlidacstatu.cz"; 
                o.Cookie.Name = "HlidacLoginCookie"; // Name of cookie     
                o.LoginPath = "/Identity/Account/Login"; // Path for the redirect to user login page    
              
                o.Cookie.SameSite = SameSiteMode.Lax;
                o.Events = new CookieAuthenticationEvents()
                {
                    OnRedirectToLogin = (ctx) =>
                    {
                        if (ctx.Request.Path.StartsWithSegments("/api") && ctx.Response.StatusCode == 200)
                        {
                            ctx.Response.StatusCode = 401;
                        }
                        else
                        {
                            ctx.Response.Redirect($"/Identity/Account/Login?returnUrl={ctx.Request.Path}{ctx.Request.QueryString.Value}");
                        }

                        return Task.CompletedTask;
                    },
                    OnRedirectToAccessDenied = (ctx) =>
                    {
                        if (ctx.Request.Path.StartsWithSegments("/api") && ctx.Response.StatusCode == 200)
                        {
                            ctx.Response.StatusCode = 403;
                        }
                    
                        ctx.Response.StatusCode = 403;
                    
                        return Task.CompletedTask;
                    }
                };
            });
            
            //add third party authentication
            services.AddAuthentication()
                .AddGoogle(options =>
                {
                    IConfigurationSection googleAuthSetting = Configuration.GetSection("Authentication:Google");
                    options.ClientId = googleAuthSetting["Id"];
                    options.ClientSecret = googleAuthSetting["Secret"];
                })
                .AddOpenIdConnect("apple", options =>  // taken from https://github.com/scottbrady91/AspNetCore-SignInWithApple-Example/blob/main/ScottBrady91.SignInWithApple.Example/Startup.cs
                {
                    IConfigurationSection appleAuthSetting = Configuration.GetSection("Authentication:Apple");
                    string clientId = appleAuthSetting["Id"];
                    string secret = appleAuthSetting["Secret"];
                    string teamId = appleAuthSetting["TeamId"];
                    
                    options.ClientId = clientId; // Service ID
                    
                    options.Authority = "https://appleid.apple.com"; // disco doc: https://appleid.apple.com/.well-known/openid-configuration
 
                    options.CallbackPath = "/signin-apple"; // corresponding to your redirect URI

                    options.ResponseType = "code id_token"; // hybrid flow due to lack of PKCE support
                    options.ResponseMode = "form_post"; // form post due to prevent PII in the URL
                    options.DisableTelemetry = true;

                    options.Scope.Clear(); // apple does not support the profile scope
                    options.Scope.Add("openid");
                    options.Scope.Add("email");
                    options.Scope.Add("name");

                    // custom client secret generation - secret can be re-used for up to 6 months
                    options.Events.OnAuthorizationCodeReceived = context =>
                    {
                        context.TokenEndpointRequest.ClientSecret = TokenGenerator.CreateNewToken(clientId, secret, teamId);
                        return Task.CompletedTask;
                    };

                    options.UsePkce = false; // apple does not currently support PKCE (April 2021)
                })
                .AddOpenIdConnect("mojeid", options =>
                {
                    IConfigurationSection mojeidAuthSetting = Configuration.GetSection("Authentication:MojeId");
                    options.ClientId = mojeidAuthSetting["Id"]; // id, které dostaneme po registraci
                    options.ClientSecret = mojeidAuthSetting["Secret"]; // heslo, které dostaneme po registraci
                    
                    options.Authority = "https://mojeid.cz/oidc/"; // issuer
                    //options.Authority = "https://mojeid.regtest.nic.cz/oidc/"; // issuer
                    
                    options.CallbackPath = "/signin-mojeid"; //unikátní endpoint na hlídači - zatím nevím k čemu
                    
                    options.ResponseType = "code"; // typ flow (https://www.scottbrady91.com/openid-connect/openid-connect-flows)
                    options.ResponseMode = "form_post"; // form post due to prevent PII in the URL
                    
                    options.DisableTelemetry = true;
                
                    options.SaveTokens = true; // ? upřímně nevím
                    options.UsePkce = true; // ? upřímně nevím
                    
                    // claimy, které chceme získat z userinfoendpointu
                    options.Scope.Clear();
                    options.Scope.Add("openid");
                    options.Scope.Add("email");
                    //options.Scope.Add("name");
                    
                    options.GetClaimsFromUserInfoEndpoint = true; // získá data o jménu, emailu - věcech ze scope
                    
                });
            
        }
        
        private static class TokenGenerator
        {
            public static string CreateNewToken(string clientId, string secret, string teamId)
            {
                const string aud = "https://appleid.apple.com";
                
                var now = DateTime.UtcNow;
            
                var ecdsa = ECDsa.Create();
                ecdsa?.ImportPkcs8PrivateKey(Convert.FromBase64String(secret), out _);

                var handler = new JsonWebTokenHandler();
                return handler.CreateToken(new SecurityTokenDescriptor
                {
                    Issuer = teamId,
                    Audience = aud,
                    Claims = new Dictionary<string, object> {{"sub", clientId}},
                    Expires = now.AddMinutes(5), // expiry can be a maximum of 6 months - generate one per request or re-use until expiration
                    IssuedAt = now,
                    NotBefore = now,
                    SigningCredentials = new SigningCredentials(new ECDsaSecurityKey(ecdsa), SecurityAlgorithms.EcdsaSha256)
                });
            }
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
                .AddHealthCheckWithOptions<Web.HealthChecks.ProcessOpenPorts>(
                    "Open TCP ports", 
                    tags: new[] {"Web server" }
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
                        Host = "10.10.100.147",
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