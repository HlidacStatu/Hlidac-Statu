using System;
using HlidacStatu.Entities;
using HlidacStatu.Entities.Entities;
using HlidacStatu.JobTableEditor.Areas.Identity;
using HlidacStatu.JobTableEditor.Data;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HlidacStatu.JobTableEditor
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            Devmasters.Config.Init(Configuration);

            System.Globalization.CultureInfo.DefaultThreadCurrentCulture = Util.Consts.czCulture;
            System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = Util.Consts.csCulture;

            
            string connectionString = Configuration.GetConnectionString("DefaultConnection");
            // for scoped services (mainly for identity)
            services.AddDbContext<DbEntities>(options =>
                options.UseSqlServer(connectionString));
            
            // Add a DbContext to store your Database Keys
            services.AddDbContext<HlidacKeysContext>(options =>
                options.UseSqlServer(connectionString));
            
            // using Microsoft.AspNetCore.DataProtection;
            services.AddDataProtection()
                .PersistKeysToDbContext<HlidacKeysContext>()
                .SetApplicationName("HlidacStatu");
            
            AddIdentity(services);
            
            services.AddRazorPages();
            services.AddServerSideBlazor();
            services.AddScoped<AuthenticationStateProvider, RevalidatingIdentityAuthenticationStateProvider<ApplicationUser>>();
            services.AddDatabaseDeveloperPageExceptionFilter();

            services.AddSingleton<JobService>();
            services.AddScoped<ToastService>();
            services.AddScoped<StatisticsService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
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
                );
            
            // setup single sign on cookie
            services.ConfigureApplicationCookie(o =>
            {
                o.Cookie.Domain = ".hlidacstatu.cz"; 
                o.Cookie.Name = "HlidacLoginCookie"; // Name of cookie     
                
                o.Cookie.SameSite = SameSiteMode.Lax;
            });
        }
        
        
    }
}