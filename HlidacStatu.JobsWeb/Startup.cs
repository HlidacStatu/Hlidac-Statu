using System;
using System.Linq;
using System.Threading.Tasks;
using HlidacStatu.Ceny.Services;
using HlidacStatu.Entities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HlidacStatu.Ceny
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            Devmasters.Config.Init(Configuration);

            System.Globalization.CultureInfo.DefaultThreadCurrentCulture = Util.Consts.czCulture;
            System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = Util.Consts.csCulture;

            Task.Run(async () => await JobService.RecalculateAsync()).GetAwaiter().GetResult();

            string usersConnection = Devmasters.Config.GetWebConfigValue("WdAnalyt");
            // for scoped services (mainly for identity)
            services.AddDbContext<DbEntities>(options =>
                options.UseSqlServer(usersConnection));
            
            // Pro subdoménový login pomocí cookie
            // Add a DbContext to store your Database Keys
            // services.AddDbContext<HlidacKeysContext>(options =>
            //     options.UseSqlServer(connectionString));
            // services.AddDataProtection()
            //     .PersistKeysToDbContext<HlidacKeysContext>()
            //     .SetApplicationName("HlidacStatu");

            AddIdentity(services);
            services.AddDatabaseDeveloperPageExceptionFilter();

            services.AddRazorPages()
                .AddRazorRuntimeCompilation();
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

            app.Use(async (context, next) =>
            {
                var req = context.Request.Query;

                if (req.TryGetValue("obor", out var oborValues))
                {
                    if (req.TryGetValue("rok", out var rokValues) 
                        && int.TryParse(rokValues.FirstOrDefault(), out int rok))
                    {
                        string obor = oborValues.FirstOrDefault();
                        
                        context.Items.Add("obor", obor);
                        context.Items.Add("rok", rok);
                        
                    }
                }

                await next();
            });

            app.UseEndpoints(endpoints => { endpoints.MapRazorPages(); });
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

            // Pro napojení na hs
            // services.Configure<PasswordHasherOptions>(options =>
            //     options.CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV2
            // );
            
            // services.ConfigureApplicationCookie(o =>
            // {
            //     o.Cookie.Domain = ".hlidacstatu.cz"; 
            //     o.Cookie.Name = "HlidacLoginCookie"; // Name of cookie     
            //     
            //     o.Cookie.SameSite = SameSiteMode.Lax;
            // });
        }
    }
}