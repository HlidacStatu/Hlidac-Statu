using HlidacStatu.LibCore.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using PoliticiEditor;
using PoliticiEditor.Components;
using PoliticiEditor.Components.Account;
using PoliticiEditor.Components.Autocomplete;
using PoliticiEditor.Components.Toasts;
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
builder.Services.AddSingleton<ToastService>();
builder.Services.AddSingleton<LocallyCachedAutocompleteService>();
builder.Services.AddHostedService<AutocompleteBackgroundTimer>();

builder.Services.Configure<SmtpSettings>(options =>
{
    options.Server = builder.Configuration.GetValue<string>("AppSettings:SmtpHost");
    options.Port = 25;
    options.FromAddress = "podpora@hlidacstatu.cz";
});
builder.Services.AddScoped<IEmailService, EmailService>();

// string connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
//                           throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
// builder.Services.AddDbContext<DbEntities>(options =>
//     options.UseSqlServer(connectionString));

// Default services
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Identity
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<UserHelper>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.ExpireTimeSpan = TimeSpan.FromDays(2);
        options.SlidingExpiration = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // 🔥 always HTTPS
        options.Cookie.HttpOnly = true; // 🔥 cookie not accessible to JavaScript
        options.Cookie.SameSite = SameSiteMode.Strict; // (optional) protects against CSRF better
        options.Cookie.Name = "PoliticiAuth";
        options.LoginPath = "/";
        options.LogoutPath = "/Account/Logout"; // (optional) if you have logout there
    });
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

var app = builder.Build();

app.UseExceptionHandler("/Error", createScopeForErrors: true);
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapPost("/account/logoutp", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Ok();
});

await app.RunAsync();