using HlidacStatu.LibCore.ConfigurationProviders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Devmasters.Log;

namespace HlidacStatu.LibCore.Extensions;

public static class HlidacConfigExtensions
{
    public const string FallbackEnvironment = "Production";

    /// <summary>
    /// Načte sql connection string z Environment variable 'HS_CNN';
    /// Načte hodnotu environment z Environment variable 'HS_ENV';
    /// Pomocí těchto dvou proměnných načte konfigurační data z SQL (tabulka ConfigurationValues);
    /// Poté načte konfigurační data z appsettings.json (pokud existuje);
    /// Poté načte konfigurační data z Logger.serilog.json (pokud existuje);
    /// Poté načte konfigurační data z appsettings.Development.json (pokud existuje);
    /// Poté načte konfigurační data z Environment Variables;
    /// -pozn.: později načtená konfigurační data mají vyšší prioritu;
    /// (Environment > appsettings.Development.json > appsettings.json > SQL ConfigurationValues)
    /// </summary>
    public static IHostBuilder ConfigureHostForDocker(this IHostBuilder hostBuilder,
        string? tag = null, string[] args = null,
        bool clearConfigurationSources = false,
        string logStashUrl = null)
    {
        hostBuilder.ConfigureAppConfiguration((context, config) =>
        {
            string connectionString = Environment.GetEnvironmentVariable("HS_CNN");
            string environment = Environment.GetEnvironmentVariable("HS_ENV");
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new Exception("HS_CNN environment variable is missing");
            if (string.IsNullOrWhiteSpace(environment))
                environment = FallbackEnvironment;

            if (clearConfigurationSources)
                config.Sources.Clear();
            config.AddMsSqlConfiguration(connectionString, environment, tag);
            config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
            config.AddJsonFile("logger.serilog.json", optional: true, reloadOnChange: false);
            config.AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false);
            config.AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false);
            config.AddEnvironmentVariables();
            config.AddCommandLine(args ?? Array.Empty<string>());
            var cb = config.Build();

        });

        hostBuilder.UseSerilog((context, services, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.WithProperty("hostname", Environment.GetEnvironmentVariable("HOSTNAME") ?? "unknown_hostname")
                .Enrich.WithMachineName()
                .Enrich.WithProperty("codeversion", System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString())
                .Enrich.WithProperty("application_name", System.Reflection.Assembly.GetEntryAssembly().GetName().Name)
                .Enrich.WithProperty("application_path", new FileInfo(System.Reflection.Assembly.GetEntryAssembly().Location).DirectoryName)
                .Enrich.WithClientIp()
                .Enrich.FromLogContext()
                .WriteTo.Console();
            if (Uri.TryCreate(logStashUrl, UriKind.Absolute, out Uri logStashUri))
            {
                configuration.AddLogStash(logStashUri);
            }
        });
        return hostBuilder;
    }

    /// <summary>
    /// Načte sql connection string z appsettings.json ("ConnectionStrings:DefaultConnection");
    /// Načte hodnotu environment z appsettings.json ("HS_ENV");
    /// Pomocí těchto dvou proměnných načte konfigurační data z SQL (tabulka ConfigurationValues);
    /// Poté načte konfigurační data z appsettings.json (pokud existuje);
    /// Poté načte konfigurační data z Logger.serilog.json (pokud existuje);
    /// Poté načte konfigurační data z Environment Variables;
    /// Poté načte konfigurační data z args[];
    /// -pozn.: později načtená konfigurační data mají vyšší prioritu;
    /// (args[] > Environment > appsettings.json > SQL ConfigurationValues)
    /// </summary>
    public static IHostBuilder ConfigureHostForWeb(this IHostBuilder hostBuilder, string[] args, string? tag = null)
    {
        IConfiguration preConfig = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile("appsettings.develop.json", optional: true, reloadOnChange: false)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false)
            .Build();
        var connectionString = preConfig.GetConnectionString("DefaultConnection");
        var environment = preConfig.GetValue<string>("HS_ENV");

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new Exception("ConnectionStrings:DefaultConnection from appsettings.json is missing");
        if (string.IsNullOrWhiteSpace(environment))
            environment = FallbackEnvironment;

        return hostBuilder.ConfigureAppConfiguration((context, configuration) =>
        {
            configuration.AddMsSqlConfiguration(connectionString, environment, tag);
            configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
            configuration.AddJsonFile("logger.serilog.json", optional: true, reloadOnChange: false);
            configuration.AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false);
            configuration.AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false);
            configuration.AddEnvironmentVariables();
            configuration.AddCommandLine(args);
        }).UseSerilog((context, services, configuration) => configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.WithProperty("hostname", Environment.GetEnvironmentVariable("HOSTNAME") ?? "unknown_hostname")
            .Enrich.WithMachineName()
            .Enrich.WithProperty("codeversion", System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString())
            .Enrich.WithProperty("application_name", System.Reflection.Assembly.GetEntryAssembly().GetName().Name)
            .Enrich.WithProperty("application_path", new FileInfo(System.Reflection.Assembly.GetEntryAssembly().Location).DirectoryName)
            .Enrich.WithClientIp()
            .Enrich.FromLogContext()
            .WriteTo.Console());
    }

    /// <summary>
    /// Načte sql connection string z appsettings.json ("ConnectionStrings:DefaultConnection");
    /// Načte hodnotu environment z appsettings.json ("HS_ENV");
    /// Pomocí těchto dvou proměnných načte konfigurační data z SQL (tabulka ConfigurationValues);
    /// Poté načte konfigurační data z appsettings.json (pokud existuje);
    /// Poté načte konfigurační data z Logger.serilog.json (pokud existuje);
    /// Poté načte konfigurační data z appsettings.Development.json (pokud existuje);
    /// Poté načte konfigurační data z Environment Variables;
    /// Poté načte konfigurační data z args[];
    /// -pozn.: později načtená konfigurační data mají vyšší prioritu;
    /// (args[] > Environment > appsettings.json > appsettings.Development.json > SQL ConfigurationValues)
    /// </summary>
    public static IConfiguration InitializeConsoleConfiguration(string[] args, string? tag = null)
    {
        IConfiguration preConfig = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile("logger.serilog.json", optional: true, reloadOnChange: false)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false)
            .Build();
        var connectionString = preConfig.GetConnectionString("DefaultConnection");
        var environment = preConfig.GetValue<string>("HS_ENV");

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new Exception("ConnectionStrings:DefaultConnection from appsettings.json is missing");
        if (string.IsNullOrWhiteSpace(environment))
            environment = FallbackEnvironment;

        //Change logpath for downloaders
        string placeholder = "{appFolder}";
        string pattern = @"Serilog:WriteTo:\d*:Args:path";
        var logPathsToReplace = preConfig.AsEnumerable()
            .Where(k => Regex.IsMatch(k.Key, pattern))
            .Where(k => k.Value?.Contains(placeholder, StringComparison.InvariantCultureIgnoreCase) == true);

        var directory = new FileInfo(System.Reflection.Assembly.GetEntryAssembly().Location).DirectoryName;
        var lastFolder = directory.Split(new[] { '/', '\\' },
                StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Last();

        var overridenLogPaths = logPathsToReplace.Select(pair =>
            new KeyValuePair<string, string?>(pair.Key, pair.Value.Replace(placeholder, lastFolder))
        );

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddMsSqlConfiguration(connectionString, environment, tag)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile("logger.serilog.json", optional: true, reloadOnChange: false)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .AddCommandLine(args)
            .AddInMemoryCollection(overridenLogPaths)
            .Build();

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.WithProperty("hostname", Environment.GetEnvironmentVariable("HOSTNAME") ?? "unknown_hostname")
            .Enrich.WithMachineName()
            .Enrich.WithProperty("codeversion", System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString())
            .Enrich.WithProperty("application_name", System.Reflection.Assembly.GetEntryAssembly().GetName().Name)
            .Enrich.WithProperty("application_path", new FileInfo(System.Reflection.Assembly.GetEntryAssembly().Location).DirectoryName)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateLogger();

        return configuration;
    }
}

