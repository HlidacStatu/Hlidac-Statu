using HlidacStatu.Entities;
using Microsoft.AspNetCore.Identity;

namespace JobTableEditor;

public class IdentityStartup
{
    public static void AddIdentity(IServiceCollection services)
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