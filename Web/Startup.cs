using HlidacStatu.Entities;
using HlidacStatu.LibCore.Filters;
using HlidacStatu.LibCore.MiddleWares;
using HlidacStatu.LibCore.Services;
using HlidacStatu.Web.Filters;
using HlidacStatu.Web.Framework;
using HlidacStatu.Web.Views.Shared.Components;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Polly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace HlidacStatu.Web
{
    public class Startup
    {

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

#if DEBUG
            //if (System.Diagnostics.Debugger.IsAttached)
            //    System.Net.Http.HttpClient.DefaultProxy = new System.Net.WebProxy("127.0.0.1", 8888);
#endif

            System.Globalization.CultureInfo.DefaultThreadCurrentCulture = Util.Consts.czCulture;
            System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = Util.Consts.csCulture;


            DBUpgrades.DBUpgrader.UpgradeDatabases(Connectors.DirectDB.DefaultCnnStr);


            string connectionString = Configuration.GetConnectionString("DefaultConnection");
            // for scoped services (mainly for identity)
            _= services.AddDbContext<DbEntities>(options =>
                options.UseSqlServer(connectionString));
            _ = services.AddDatabaseDeveloperPageExceptionFilter();

            // Add a DbContext to store your Database Keys
            _ = services.AddDbContext<HlidacKeysContext>(options =>
                options.UseSqlServer(connectionString));

            // using Microsoft.AspNetCore.DataProtection;
            _ = services.AddDataProtection()
                .PersistKeysToDbContext<HlidacKeysContext>()
                .SetApplicationName("HlidacStatu");

            AddIdentity(services);
            AddBundling(services);

            if (Constants.IsDevelopment(WebHostEnvironment))
            {
                _ = services.AddControllersWithViews()
                    .AddNewtonsoftJson()
                    .AddRazorRuntimeCompilation();
            }
            else
            {
                _ = services.AddControllersWithViews()
                    .AddNewtonsoftJson();
            }

            _ = services.AddRazorPages()
                .AddMvcOptions(options =>
                    options.Filters.Add<SpamProtectionRazor>());

            _ = services.AddSingleton<AttackerDictionaryService>();

            _ = services.AddResponseCaching();

            _ = services.AddHttpClient(Constants.DefaultHttpClient)
                .AddTransientHttpErrorPolicy(policyBuilder =>
                    policyBuilder.WaitAndRetryAsync(
                        3, retryNumber => TimeSpan.FromMilliseconds(10)));

            _ = services.AddServerSideBlazor().AddInteractiveServerComponents();

            _ = services.AddScoped<IErrorBoundaryLogger, AutocompleteErrorLogger>();

            _ = services
                .AddHealthChecks()
                .AddProcessAllocatedMemoryHealthCheck(maximumMegabytesAllocated: 50000,
                    name: "Web server využitá pamět",
                    tags: new[] { "Web server", "process", "memory" })
                .AddHealthCheckWithOptions<HlidacStatu.Web.HealthChecks.NetworkDiskStorage, HlidacStatu.Web.HealthChecks.NetworkDiskStorage.Options>(
                    new HlidacStatu.Web.HealthChecks.NetworkDiskStorage.Options()
                    {
                        UNCPath = "c:\\",
                        DegradedMinimumFreeMegabytes = 20 * 1024, //20G 
                        UnHealthtMinimumFreeMegabytes = 5 * 1024 //5GB
                    },
                    "System disk", HealthStatus.Unhealthy, tags: new[] { "Web server" }
                )
                .AddHealthCheckWithOptions<HlidacStatu.Web.HealthChecks.IISConnections, HlidacStatu.Web.HealthChecks.IISConnections.Options>(
                    new HealthChecks.IISConnections.Options() {  AppPoolNameFilter= "net6-wwwHS", CountWarningThreshold = 20, CountErrorThreshold = 50  },
                        "IIS open requests",
                        tags: new[] { "Web server" }
                        )
                .AddHealthCheckWithOptions<HlidacStatu.Web.HealthChecks.IISConnections, HlidacStatu.Web.HealthChecks.IISConnections.Options>(
                    new HealthChecks.IISConnections.Options() { AppPoolNameFilter = "net6-wwwHS", StartsWithFilter="_blazor" },
                        "Blazor open requests",
                        tags: new[] { "Web server" }
                        )
                .AddHealthCheckWithOptions<HlidacStatu.Web.HealthChecks.NetworkDiskStorage, HlidacStatu.Web.HealthChecks.NetworkDiskStorage.Options>(
                    new HlidacStatu.Web.HealthChecks.NetworkDiskStorage.Options()
                    {
                        UNCPath = Devmasters.Config.GetWebConfigValue("FileCachePath"),
                        DegradedMinimumFreeMegabytes = 20 * 1024, //20G 
                        UnHealthtMinimumFreeMegabytes = 5 * 1024 //5GB
                    },
                    "Cache disk", HealthStatus.Unhealthy, tags: new[] { "Web server" }
                );

        }

        //Nastavení, jak budou zpracovány požadavky (Middleware).
        //!Záleží na pořadí
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRequestTrackMiddleware(new RequestTrackMiddleware.Options()
            {
                LimitToPaths = new List<string> { "/api" },
                ApplicationName = "WEB"
            });


            //request time measurement with exception for /_blazor pages
            app.UseTimeMeasureMiddleware(new List<string>() { "/_blazor" });


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
                var whitelistIps = Devmasters.Config.GetWebConfigValue("BanWhitelist")?.Split(',',
                    StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                BannedIpsMiddleware.Whitelist whitelist = new BannedIpsMiddleware.Whitelist();
                if (whitelistIps != null && whitelistIps.Length > 0)
                {
                    foreach (var ip in whitelistIps)
                    {
                        whitelist.IpAddresses.Add(ip);
                    }
                }

                app.UseBannedIpsMiddleware(whitelist); // tohle nechci při developmentu :) 
                app.UseExceptionHandler("/Error/500");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseWebOptimizer();
            app.UseStatusCodePagesWithReExecute("/error/{0}");
            app.UseHttpsRedirection();

            app.UseOnHTTPErrorMiddleware();

            app.UseStaticFiles();



            //redirect rules
            _ = app.Use(async (context, next) =>
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
                    context.Response.Redirect("https://www.WatchdogAnalytics.cz/?" + context.Request.QueryString);
                    return;   // short circuit
                }

                if (url?.ToLower()?.StartsWith("/jobtableeditor") == true)
                {
                    context.Response.Redirect("https://jobtableeditor.hlidacstatu.cz/");
                    return;   // short circuit
                }

/*                if (url?.ToLower()?.StartsWith("/health") == true)
                {
                    await context.Response.WriteAsJsonAsync<Models.HealthCheckStatusModel>(Models.HealthCheckStatusModel.CurrentData.Get());
                    return;
                }
*/
                await next(context);
            });
            _ = app.UseRouting();
            _ = app.UseResponseCaching();



            /*app.UseHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions()
            {
                Predicate = _ => true,
                ResponseWriter = global::HealthChecks.UI.Client.UIResponseWriter.WriteHealthCheckUIResponse
                
            });*/


            _ = app.UseAuthentication();
            _ = app.UseApiAuthenticationMiddleware();
            _ = app.UseAuthorization();

            _ = app.UseEndpoints(endpoints => {
                endpoints.MapHealthChecks("/health"
                        , new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions()
                        {
                            Predicate = _ => true,
                            ResponseWriter = global::HealthChecks.UI.Client.UIResponseWriter.WriteHealthCheckUIResponse

                        }
                    ).WithMetadata(new AuthorizeAttribute() { Roles = "Admin" });
            });

            _ = app.UseEndpoints(endpoints =>
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
                endpoints.MapRazorComponents<AutocompleteWrap>()
                    .AddInteractiveServerRenderMode();
                //endpoints.MapBlazorHub();
            });


        }

        private void AddBundling(IServiceCollection services)
        {
            _ = services.AddWebOptimizer(pipeline =>
            {
                string[] cssPaths = new[]
                {
                    "wwwroot/Content/GlobalSite.v1.15.css",
                    "wwwroot/Content/social-share-kit.css",
                    "wwwroot/Content/new.v1.15.css"
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
                    "Scripts/hlidac.v1.13.js",
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

                // pipeline.MinifyJsFiles(new NUglify.JavaScript.CodeSettings() { MinifyCode = Constants.IsDevelopment(WebHostEnvironment)==false });
            });
        }


        private void AddIdentity(IServiceCollection services)
        {
            _ = services.AddDefaultIdentity<ApplicationUser>(options =>
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
                    string keyId = appleAuthSetting["KeyId"];

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
                        context.TokenEndpointRequest.ClientSecret = TokenGenerator.CreateNewToken(clientId, secret, teamId, keyId);
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
            public static string CreateNewToken(string clientId, string secret, string teamId, string keyId)
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
                    Claims = new Dictionary<string, object> { { "sub", clientId } },
                    Expires = now.AddMinutes(5), // expiry can be a maximum of 6 months - generate one per request or re-use until expiration
                    IssuedAt = now,
                    NotBefore = now,
                    SigningCredentials = new SigningCredentials(new ECDsaSecurityKey(ecdsa) { KeyId = keyId }, SecurityAlgorithms.EcdsaSha256)
                });
            }
        }


    }
}