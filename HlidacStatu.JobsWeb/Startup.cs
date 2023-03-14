using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using HlidacStatu.Ceny.Services;
using HlidacStatu.Entities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

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

            //add third party authentication
            services.AddAuthentication()
                .AddGoogle(options => //https://learn.microsoft.com/en-us/aspnet/core/security/authentication/social/google-logins?view=aspnetcore-7.0
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
                });
            
            services.ConfigureApplicationCookie(o =>
            {
                o.ExpireTimeSpan = TimeSpan.FromDays(30);
                o.Cookie.Domain = ".watchdoganalytics.cz";
                o.Cookie.SecurePolicy = CookieSecurePolicy.Always;

                o.Cookie.SameSite = SameSiteMode.Strict;
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
    }
}