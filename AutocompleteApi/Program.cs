using System;
using Devmasters.Log;
using HlidacStatu.AutocompleteApi.Services;
using HlidacStatu.LibCore.Extensions;
using HlidacStatu.LibCore.MiddleWares;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

// inicializace
var logLevel = Environment.GetEnvironmentVariable("MinLogLevel") switch
{
    "verbose" => LogEventLevel.Verbose,
    "debug" => LogEventLevel.Debug,
    "information" => LogEventLevel.Information,
    "error" => LogEventLevel.Error,
    "fatal" => LogEventLevel.Fatal,
    _ => LogEventLevel.Warning
};
string logStashUrl = Environment.GetEnvironmentVariable("LogStashUrl");

var loggerBuilder = new LoggerConfiguration()
    .MinimumLevel.Is(logLevel)
    .WriteTo.Console()
    .Enrich.WithProperty("hostname", Environment.GetEnvironmentVariable("HOSTNAME"))
    .Enrich.WithProperty("codeversion",
        System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version?.ToString())
    .Enrich.WithProperty("application_name", "Autocomplete api");

if (!string.IsNullOrWhiteSpace(logStashUrl))
{
    loggerBuilder = loggerBuilder.AddLogStash(new Uri(logStashUrl));
}
Log.Logger = loggerBuilder.CreateLogger();
var logger = Log.ForContext<Program>();


var builder = WebApplication.CreateBuilder(args);
builder.Host.ConfigureHostForDocker();

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
    builder.Services.AddSingleton<IndexCache>(new IndexCache());
    builder.Services.AddControllers();
    builder.Services.AddHttpClient();
    logger.Information("Services configured");

    var app = builder.Build();
    app.UseRequestTrackMiddleware(new RequestTrackMiddleware.Options()
    {
        ApplicationName = "AutocompleteApi",
        MinimumRequestTimeToTrackMs = 900
    });

    var timeMeasureLogger = Devmasters.Log.Logger.CreateLogger("HlidacStatu.AutocompleteApi.ResponseTimes",
        Devmasters.Log.Logger.DefaultConfiguration()
            .Enrich.WithProperty("codeversion",
                System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString()));
            
    app.UseTimeMeasureMiddleware(timeMeasureLogger);
    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }

    app.UseRouting();

    app.UseEndpoints(endpoints =>
    {
        endpoints.MapControllers();
    });
    
    app.Run();
    return 0;
}
catch (Exception exception)
{
    Log.Fatal(exception, "Host terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}
