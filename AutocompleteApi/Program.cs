using System;
using Devmasters.Log;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace HlidacStatu.AutocompleteApi
{
    public class Program
    {
        public static int Main(string[] args)
        {
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
            logger.Information("Starting application");

            try
            {
                CreateHostBuilder(args).Build().Run();
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
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });
    }
}