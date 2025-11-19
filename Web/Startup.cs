using HlidacStatu.Entities;
using HlidacStatu.LibCore.MiddleWares;
using HlidacStatu.LibCore.Services;
using HlidacStatu.Web.Filters;
using HlidacStatu.Web.Framework;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace HlidacStatu.Web
{
    public static class Startup
    {
        private const string CORSPolicy = "from_hlidacstatu.cz";
        //Globální konfiguraci a nastavení sem
        public static void ConfigureServices(this IServiceCollection services, IConfiguration configuration)
        {
            string connectionString = configuration.GetConnectionString("DefaultConnection");
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
            
            services.AddCors(options =>
            {
                options.AddPolicy(name: CORSPolicy,
                    policy =>
                    {
                        policy.SetIsOriginAllowedToAllowWildcardSubdomains()
                            .WithOrigins("https://*.hlidacstatu.cz", "http://*.hlidacstatu.cz")
                            .AllowAnyMethod()
                            .AllowCredentials()
                            .AllowAnyHeader()
                            .Build();
                    });
            });

            
            AddIdentity(services, configuration);

            if (Constants.IsDevelopment())
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

            services.AddResponseCaching();

            services.AddSignalR();

            services.AddHttpClient(Constants.DefaultHttpClient)
                .AddTransientHttpErrorPolicy(policyBuilder =>
                    policyBuilder.WaitAndRetryAsync(
                        3, retryNumber => TimeSpan.FromMilliseconds(10)));

            services
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
        public static void ConfigurePipeline(this WebApplication app)
        {
            app.UseRequestTrackMiddleware(new RequestTrackMiddleware.Options()
            {
                LimitToPaths = new List<string> { "/api" },
                ApplicationName = "WEB"
            });

            app.UseTimeMeasureMiddleware(ignorePaths: new List<string>() { "/_blazor" });
            
            if (Constants.IsDevelopment())
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

            app.UseStatusCodePagesWithReExecute("/error/{0}");
            app.UseHttpsRedirection();

            app.UseOnHTTPErrorMiddleware();

            app.UseStaticFiles();
            
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
                    context.Response.Redirect("https://www.WatchdogAnalytics.cz/?" + context.Request.QueryString);
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
            //app.MapHub<HlidacStatu.Web.Framework.SignalR.OllamaSignalRHub>("/ollamaHub");


            app.UseResponseCaching();

            app.UseAuthentication();
            app.UseApiAuthenticationMiddleware();
            app.UseAuthorization();
            
            app.UseCors(CORSPolicy);

            app.UseEndpoints(endpoints => {
                endpoints.MapHealthChecks("/health"
                        , new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions()
                        {
                            Predicate = _ => true,
                            ResponseWriter = global::HealthChecks.UI.Client.UIResponseWriter.WriteHealthCheckUIResponse

                        }
                    ).WithMetadata(new AuthorizeAttribute() { Roles = "PrivateApi" });
                
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

        private static void AddIdentity(IServiceCollection services, IConfiguration configuration)
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
                            // Never redirect, just send a 401 if nothing was written yet
                            if (!ctx.Response.HasStarted)
                            {
                                ctx.Response.Clear();
                                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            }
                            return Task.CompletedTask;
                        }
                        
                        if (!ctx.Response.HasStarted)
                        {
                            ctx.Response.Redirect($"/Identity/Account/Login?returnUrl={ctx.Request.Path}{ctx.Request.QueryString.Value}");
                        }

                        return Task.CompletedTask;
                    },
                    OnRedirectToAccessDenied = (ctx) =>
                    {
                        if (ctx.Request.Path.StartsWithSegments("/api") && ctx.Response.StatusCode == 200)
                        {
                            if (!ctx.Response.HasStarted)
                            {
                                ctx.Response.Clear();
                                ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                            }
                            return Task.CompletedTask;
                        }

                        if (!ctx.Response.HasStarted)
                        {
                            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                        }

                        return Task.CompletedTask;
                    }
                };
            });

            //add third party authentication
            services.AddAuthentication()
                .AddGoogle(options =>
                {
                    IConfigurationSection googleAuthSetting = configuration.GetSection("Authentication:Google");
                    options.ClientId = googleAuthSetting["Id"];
                    options.ClientSecret = googleAuthSetting["Secret"];
                })
                .AddOpenIdConnect("apple", options =>  // taken from https://github.com/scottbrady91/AspNetCore-SignInWithApple-Example/blob/main/ScottBrady91.SignInWithApple.Example/Startup.cs
                {
                    IConfigurationSection appleAuthSetting = configuration.GetSection("Authentication:Apple");
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
                    IConfigurationSection mojeidAuthSetting = configuration.GetSection("Authentication:MojeId");
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
                
                // Extract raw base64 from PEM
                var lines = secret.Split('\n')
                    .Select(l => l.Trim())
                    .Where(l => !l.StartsWith("-----"))
                    .ToArray();

                var der = Convert.FromBase64String(string.Join("", lines));

                var ecdsa = ECDsa.Create();
                ecdsa?.ImportPkcs8PrivateKey(der, out _);

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