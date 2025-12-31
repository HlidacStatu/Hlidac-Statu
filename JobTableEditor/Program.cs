using HlidacStatu.Entities;
using HlidacStatu.LibCore.Extensions;
using JobTableEditor;
using JobTableEditor.Areas.Identity;
using JobTableEditor.Components;
using JobTableEditor.Data;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureHostForWeb(args);
builder.WebHost.UseStaticWebAssets();


// Service registration
//inicializace statických proměnných
var configuration = builder.Configuration;
Devmasters.Config.Init(configuration);
System.Globalization.CultureInfo.DefaultThreadCurrentCulture = HlidacStatu.Util.Consts.czCulture;
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = HlidacStatu.Util.Consts.csCulture;

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();

string? connectionString = configuration.GetConnectionString("DefaultConnection");
// for scoped services (mainly for identity)
builder.Services.AddDbContext<DbEntities>(options =>
    options.UseSqlServer(connectionString));
            
// Add a DbContext to store your Database Keys
builder.Services.AddDbContext<HSKeysContext>(options =>
    options.UseSqlServer(connectionString));
            
// using Microsoft.AspNetCore.DataProtection;
builder.Services.AddDataProtection()
    .PersistKeysToDbContext<HSKeysContext>()
    .SetApplicationName("HlidacStatu");
            
IdentityStartup.AddIdentity(builder.Services);
            
builder.Services.AddScoped<AuthenticationStateProvider, RevalidatingIdentityAuthenticationStateProvider<ApplicationUser>>();


builder.Services.AddSingleton<JobService>();
builder.Services.AddScoped<ToastService>();
builder.Services.AddScoped<StatisticsService>();


var app = builder.Build();

// HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
