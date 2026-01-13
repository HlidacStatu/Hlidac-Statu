using System;
using HlidacStatu.AutocompleteApi.Services;
using HlidacStatu.LibCore.Extensions;
using HlidacStatu.LibCore.MiddleWares;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

string CORSPolicy = "from_hlidacstatu.cz";

// inicializace
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Host.ConfigureHostForDocker();
var logger = Log.ForContext<Program>();

//Register hlidac
Devmasters.Config.Init(builder.Configuration);
System.Globalization.CultureInfo.DefaultThreadCurrentCulture = HlidacStatu.Util.Consts.czCulture;
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = HlidacStatu.Util.Consts.csCulture;

// wati until generator (dependency) is running
var baseUrl = Devmasters.Config.GetWebConfigValue("GeneratorUrl");
var url = $"{baseUrl}/test"; //test endpoint
await HlidacStatu.LibCore.Docker.WaitUntilServiceIsRunning(url);


// run application
logger.Information("Starting application");
try
{
    logger.Information("Configuring services");
    builder.Services.AddHostedService<TimedHostedService>();
    builder.Services.AddSingleton<IndexCache>();
    builder.Services.AddControllers();
    builder.Services.AddHttpClient();
    
    builder.Services.AddCors(options =>
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

    logger.Information("Services configured");

    var app = builder.Build();
    app.UseRequestTrackMiddleware(new RequestTrackMiddleware.Options()
    {
        ApplicationName = "AutocompleteApi",
        MinimumRequestTimeToTrackMs = 900
    });
    
    app.UseTimeMeasureMiddleware();
    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }

    app.UseRouting();
    
    app.UseCors(CORSPolicy);

    app.MapControllers();
    
    await app.RunAsync();
    return 0;
}
catch (Exception exception)
{
    Log.Fatal(exception, "Host terminated unexpectedly");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}
