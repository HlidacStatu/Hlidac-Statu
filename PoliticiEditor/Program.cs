using HlidacStatu.Entities;
using HlidacStatu.LibCore.Extensions;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using PoliticiEditor.Components;
using PoliticiEditor.Components.Autocomplete;
using PoliticiEditor.Components.Pages.Account;
using PoliticiEditor.Data;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
// add Hlidac app bootstrap (loading configs, ...)
builder.ConfigureHostForWeb(args); 

// init statics and others
Devmasters.Config.Init(builder.Configuration);
System.Globalization.CultureInfo.DefaultThreadCurrentCulture = HlidacStatu.Util.Consts.czCulture;
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = HlidacStatu.Util.Consts.csCulture;
var logger = Log.ForContext<Program>();

// custom services
builder.Services.AddHttpClient();
builder.Services.AddSingleton<AutocompleteService>();

string connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                          throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<DbEntities>(options =>
    options.UseSqlServer(connectionString));
    

// Default services
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Identity
builder.Services.AddCascadingAuthenticationState(); // dont know if necessarry
builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromDays(2);
        options.SlidingExpiration = true;
    });

var identityDbConnectionString = builder.Configuration.GetConnectionString("IdentityConnection") ??
                       throw new InvalidOperationException("Connection string 'IdentityConnection' not found.");
builder.Services.AddDbContext<PoliticiLoginsDbContext>(options =>
    options.UseSqlite(identityDbConnectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();