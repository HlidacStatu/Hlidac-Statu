using HlidacStatu.Entities;
using HlidacStatu.LibCore.Extensions;
using HlidacStatu.LibCore.MiddleWares;
using HlidacStatu.LibCore.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PlatyUredniku.Cache;
using PlatyUredniku.Services;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using ZiggyCreatures.Caching.Fusion;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Exporter;


namespace PlatyUredniku;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.ConfigureHostForWeb(args);


        //init statics and others
        Devmasters.Config.Init(builder.Configuration);
        // init logger
        var logger = Log.ForContext<Program>();
        
        try
        {
            System.Globalization.CultureInfo.DefaultThreadCurrentCulture = HlidacStatu.Util.Consts.czCulture;
            System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = HlidacStatu.Util.Consts.csCulture;
            
            
            builder.Services.AddOpenTelemetry()
              .ConfigureResource(r => r.AddService(System.Reflection.Assembly.GetEntryAssembly().GetName().Name))
              .WithTracing(tracing =>
              {
                  tracing.AddSource(System.Reflection.Assembly.GetEntryAssembly().GetName().Name);
                  tracing.AddAspNetCoreInstrumentation();
                  tracing.AddHttpClientInstrumentation();
                  tracing.AddSqlClientInstrumentation();
                  tracing.AddOtlpExporter(opt =>
                  {
                      opt.Endpoint = new Uri("http://10.10.100.141:5341/ingest/otlp/v1/traces");
                      opt.Protocol = OtlpExportProtocol.HttpProtobuf;
                      opt.Headers = "X-Seq-ApiKey=kYIIhIQAPdJEZxrXjvl3";
                  });
              });


            // Identity stuff for shared cookies with Hlidac
            string connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            builder.Services.AddDbContext<DbEntities>(options =>
                options.UseSqlServer(connectionString));
            builder.Services.AddDbContext<HlidacKeysContext>(options =>
                options.UseSqlServer(connectionString));
            builder.Services.AddDataProtection()
                .PersistKeysToDbContext<HlidacKeysContext>()
                .SetApplicationName("HlidacStatu");

            AddIdentity(builder.Services);
            AddBundling(builder.Services);

            builder.Services.AddDatabaseDeveloperPageExceptionFilter();
            builder.Services.AddControllersWithViews();
            builder.Services.AddResponseCaching();

            builder.Services.AddSingleton<AttackerDictionaryService>();
            builder.Services.AddHttpClient();

            builder.Services.AddSingleton<AutocompleteCacheService>();
            builder.Services.AddSingleton<AutocompleteCategoryCacheService>();
            builder.Services.AddHostedService<AutocompleteTimer>();

            builder.Services.AddFusionCache()
                .WithDefaultEntryOptions(CachingOptions.Default);

            // builder.Services.AddScoped<IErrorBoundaryLogger, AutocompleteErrorLogger>();

            var app = builder.Build();


            UredniciStaticCache.Init(app.Services.GetService<IFusionCache>());

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

            // app.UseLogRequestMiddleware(new LogRequestMiddleware.Options()
            // {
            //     LogEventLevel = Serilog.Events.LogEventLevel.Information,
            //     RequestToLogFilter = (c) => c.Response.ContentType.StartsWith("text/html")
            // });

            app.UseTimeMeasureMiddleware(new List<string>() { "/_blazor" });

            app.UseBannedIpsMiddleware(whitelist);

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/StatusCode/500"); // Generic error handling
                app.UseHsts();
            }
            app.UseStatusCodePagesWithReExecute("/Home/StatusCode", "?code={0}"); // Handle specific status codes

            app.UseWebOptimizer();

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            
            
            app.UseRouting();

            // Redirect original platy uredniku from Home into /urednici
            app.Use(async (context, next) =>
            {
                var path = context.Request.Path.Value?.Trim('/');
    
                if (!string.IsNullOrEmpty(path) && context.GetEndpoint() == null)
                {
                    var actionDescriptors = context.RequestServices
                        .GetRequiredService<IActionDescriptorCollectionProvider>()
                        .ActionDescriptors.Items
                        .OfType<ControllerActionDescriptor>();

                    var actionName = path.Split('/').FirstOrDefault();
                    if (actionName is not null)
                    {
                        var existsInUrednici = actionDescriptors.Any(a =>
                            a.ControllerName.Equals("Urednici", StringComparison.OrdinalIgnoreCase) &&
                            a.ActionName.Equals(actionName, StringComparison.OrdinalIgnoreCase));

                        if (existsInUrednici)
                        {
                            // Preserve query parameters
                            var queryString = context.Request.QueryString.Value;
                            context.Response.Redirect($"/urednici/{path}{queryString}", permanent: true);
                            return;
                        }
                    }
                }

                await next();
            });
            
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.MapControllerRoute(
                name: "home",
                pattern: "{action=Index}/{id?}",
                defaults: new { controller = "Home" });

            app.MapControllerRoute(
                name: "directIndex",
                pattern: "{controller}/{id?}",
                defaults: new { action = "Index" });


            app.MapRazorPages();
            app.Logger.LogInformation("PlatyUredniku Web starting");
            app.Run();
        }
        catch (Exception e)
        {
            logger.Fatal(e, "PlatyUredniku Web se nepodařilo spustit");
            throw;
        }
    }

    private static void AddBundling(IServiceCollection services)
    {
        _ = services.AddWebOptimizer(pipeline =>
        {
            string[] cssPaths = new[]
            {
                    "wwwroot/Content/GlobalSite.v1.15.css",
                    "wwwroot/Content/social-share-kit.css",
                    "wwwroot/Content/new.v1.15.css",
                    "wwwroot/css/site.css",
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
                "Scripts/Highcharts-8/highcharts.js",
                "Scripts/Highcharts-8/highcharts-more.js",
                "Scripts/highcharts.global.options.js");

            pipeline.AddJavaScriptBundle("/bundles/highcharts11",
                "Scripts/Highcharts-11/highcharts.js",
                "Scripts/Highcharts-11/highcharts-more.js",
                "Scripts/highcharts.global.options.js");

            pipeline.AddJavaScriptBundle("/bundles/typeahead",
                "Scripts/typeahead.bundle.min.js",
                "Scripts/bloodhound.min.js");

            // pipeline.MinifyJsFiles(new NUglify.JavaScript.CodeSettings() { MinifyCode = Constants.IsDevelopment(WebHostEnvironment)==false });
        });
    }

    private static void AddIdentity(IServiceCollection services)
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
        );

        services.ConfigureApplicationCookie(o =>
        {
            o.Cookie.Domain = ".platyuredniku.cz";
            o.Cookie.Name = "HlidacLoginCookie"; // Name of cookie     

            o.Cookie.SameSite = SameSiteMode.Lax;
        });
    }
}