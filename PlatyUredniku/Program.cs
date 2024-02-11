using HlidacStatu.Entities;
using HlidacStatu.LibCore.Extensions;
using HlidacStatu.LibCore.MiddleWares;
using HlidacStatu.LibCore.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PlatyUredniku.Services;
using PlatyUredniku.Views.Shared.Components;
using Serilog;
using System;
using System.Collections.Generic;

namespace PlatyUredniku;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Host.ConfigureHostForWeb(args);
        

        //init statics and others
        Devmasters.Config.Init(builder.Configuration);
        // init logger
        var logger = Log.ForContext<Program>();

        try
        {
            System.Globalization.CultureInfo.DefaultThreadCurrentCulture = HlidacStatu.Util.Consts.czCulture;
            System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = HlidacStatu.Util.Consts.csCulture;

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
            builder.Services.AddHostedService<AutocompleteTimer>();

            
            builder.Services.AddServerSideBlazor().AddInteractiveServerComponents();
            // builder.Services.AddScoped<IErrorBoundaryLogger, AutocompleteErrorLogger>();

            var app = builder.Build();
            
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

            app.UseTimeMeasureMiddleware(new List<string>() { "/_blazor" });
            
            app.UseBannedIpsMiddleware(whitelist);

            //todo: remove after
            app.UseDeveloperExceptionPage();
            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            
            app.UseWebOptimizer();

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "onlyAction",
                pattern: "{action=Index}/{id?}",
                defaults: new { controller = "Home" });

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            
            
            app.MapRazorPages();
            app.MapRazorComponents<AutocompleteWrap>()
                .AddInteractiveServerRenderMode();

            app.Run();
        }
        catch (Exception e)
        {
            logger.Fatal(e, "Hlidac admin se nepodařilo spustit");
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
                    "Scripts/Highcharts-8/highcharts.js",
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