using System;
using System.IO;
using Devmasters.Log;
using HlidacStatu.AutocompleteApi;
using HlidacStatu.AutocompleteApi.Services;
using HlidacStatu.Connectors;
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

// Generate index files only
if (args.Length > 0 && args[0] == "gen")
{
    try
    {
        logger.Information("Starting index generator");
        await IndexGenerator.GenerateIndexesAsync(logger);
        logger.Information("Index generator finished");
    }
    finally
    {
        Log.CloseAndFlush();
    }
    return 0;
}


// run application
logger.Information("Starting application");
try
{
    //todo: hodil by se refactoring
    // Aktuálně je FirmaRepo.Cached závislá na určitých souborech
    // podle mě by se tato závislost měla odstranit
    // tenhle soubor by šel ve firmarepo.cached nahradit načtením dat z tabulky o ovm: OrganVerejneMoci
    File.Copy("./DS_OVM.xml", Path.Combine(Init.WebAppDataPath, "DS_OVM.xml"), true);

    logger.Information("Configuring services");
    builder.Services.AddSingleton<CacheService>(new CacheService(logger));
    builder.Services.AddControllers();
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
