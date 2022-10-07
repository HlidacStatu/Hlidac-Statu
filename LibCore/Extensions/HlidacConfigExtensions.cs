using System;
using System.IO;
using HlidacStatu.LibCore.ConfigurationProviders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace HlidacStatu.LibCore.Extensions;

public static class HlidacConfigExtensions
{
    public const string FallbackEnvironment = "Production";
    
    /// <summary>
    /// Načte sql connection string z Environment variable 'HS_CNN';
    /// Načte hodnotu environment z Environment variable 'HS_ENV';
    /// Pomocí těchto dvou proměnných načte konfigurační data z SQL (tabulka ConfigurationValues);
    /// Poté načte konfigurační data z appsettings.json (pokud existuje);
    /// Poté načte konfigurační data z appsettings.Development.json (pokud existuje);
    /// Poté načte konfigurační data z Environment Variables;
    /// -pozn.: později načtená konfigurační data mají vyšší prioritu;
    /// (Environment > appsettings.Development.json > appsettings.json > SQL ConfigurationValues)
    /// </summary>
    public static IHostBuilder ConfigureHostForDocker(this IHostBuilder hostBuilder, string? tag = null)
    {
        return hostBuilder.ConfigureAppConfiguration((context, config) =>
        {
            string connectionString = Environment.GetEnvironmentVariable("HS_CNN");
            string environment = Environment.GetEnvironmentVariable("HS_ENV");
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new Exception("HS_CNN environment variable is missing");
            if (string.IsNullOrWhiteSpace(environment))
                environment = FallbackEnvironment; 

            config.Sources.Clear();
            config.AddMsSqlConfiguration(connectionString, environment, tag);
            config.AddJsonFile("appsettings.json", true);
            config.AddJsonFile("appsettings.Development.json", true);
            config.AddEnvironmentVariables();
        });
    }

    /// <summary>
    /// Načte sql connection string z appsettings.json ("ConnectionStrings:DefaultConnection");
    /// Načte hodnotu environment z appsettings.json ("HS_ENV");
    /// Pomocí těchto dvou proměnných načte konfigurační data z SQL (tabulka ConfigurationValues);
    /// Poté načte konfigurační data z appsettings.json (pokud existuje);
    /// Poté načte konfigurační data z Environment Variables;
    /// Poté načte konfigurační data z args[];
    /// -pozn.: později načtená konfigurační data mají vyšší prioritu;
    /// (args[] > Environment > appsettings.json > SQL ConfigurationValues)
    /// </summary>
    public static IHostBuilder ConfigureHostForWeb(this IHostBuilder hostBuilder, string[] args, string? tag = null)
    {
        IConfiguration preConfig = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", false)
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
            configuration.AddJsonFile("appsettings.json", true);
            configuration.AddEnvironmentVariables();
            configuration.AddCommandLine(args);
        });
    }
    
    /// <summary>
    /// Načte sql connection string z appsettings.json ("ConnectionStrings:DefaultConnection");
    /// Načte hodnotu environment z appsettings.json ("HS_ENV");
    /// Pomocí těchto dvou proměnných načte konfigurační data z SQL (tabulka ConfigurationValues);
    /// Poté načte konfigurační data z appsettings.json (pokud existuje);
    /// Poté načte konfigurační data z appsettings.Development.json (pokud existuje);
    /// Poté načte konfigurační data z Environment Variables;
    /// Poté načte konfigurační data z args[];
    /// -pozn.: později načtená konfigurační data mají vyšší prioritu;
    /// (args[] > Environment > appsettings.json > appsettings.Development.json > SQL ConfigurationValues)
    /// </summary>
    public static IConfiguration InitializeConsoleConfiguration(string[] args, string? tag = null)
    {
        IConfiguration preConfig = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", false)
            .Build();
        var connectionString = preConfig.GetConnectionString("DefaultConnection");
        var environment = preConfig.GetValue<string>("HS_ENV");
        
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new Exception("ConnectionStrings:DefaultConnection from appsettings.json is missing");
        if (string.IsNullOrWhiteSpace(environment))
            environment = FallbackEnvironment;

        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddMsSqlConfiguration(connectionString, environment, tag)
            .AddJsonFile("appsettings.json", true)
            .AddJsonFile("appsettings.Development.json", true)
            .AddEnvironmentVariables()
            .AddCommandLine(args)
            .Build();
    
    }
}

