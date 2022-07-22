using BlurredPageMinion;


AppDomain.CurrentDomain.ProcessExit += new EventHandler(ExceptionHandling.CurrentDomain_ProcessExit);
AppDomain.CurrentDomain.DomainUnload += new EventHandler(ExceptionHandling.CurrentDomain_ProcessExit);
AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(ExceptionHandling.CurrentDomain_UnhandledException);


foreach (System.Collections.DictionaryEntry kv in Environment.GetEnvironmentVariables())
{
    string key = kv.Key.ToString();
    if (string.Equals(key, "ApiKey", StringComparison.OrdinalIgnoreCase))
    {
        Settings.ApiKey = kv.Value.ToString();
    }
    if (string.Equals(key, "proxy", StringComparison.OrdinalIgnoreCase))
    {
        Settings.Proxy = kv.Value.ToString();
        
    }
    if (string.Equals(key, "debug", StringComparison.OrdinalIgnoreCase))
    {
        Settings.Debug = (kv.Value?.ToString()?.ToLower() == "true");
    }
}

var confBuilder = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appsettings.json", optional: true)
               .AddEnvironmentVariables();
var appConfiguration = confBuilder.Build();


IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(configure =>
    {
        configure.AddConfiguration(appConfiguration);
    })
    .ConfigureLogging((hostContext, logging) =>
    {
        logging.AddConfiguration(hostContext.Configuration.GetSection("Logging"));
        //logging.AddConsole();
        // time doesn't need timestamp to it, because it is appended by docker
    })
    .ConfigureServices(services =>
    {
        services.AddHostedService<Worker>();
        services.AddHttpClient<HttpClient>("api",config =>
        {
            config.DefaultRequestHeaders.Add("Authorization", Settings.ApiKey);
            config.DefaultRequestHeaders.Add("Accept", "*/*");
            config.Timeout = TimeSpan.FromMinutes(5);
        })
        .ConfigureHttpMessageHandlerBuilder(builder =>
        {
            if (string.IsNullOrEmpty(Settings.Proxy) == false)
            {
                builder.PrimaryHandler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (m, c, ch, e) => true,
                    Proxy = (string.IsNullOrEmpty(Settings.Proxy) ? (System.Net.IWebProxy)null : new System.Net.WebProxy(Settings.Proxy))
                };
            }
        });
        services.AddHttpClient<HttpClient>("common")
        .ConfigureHttpMessageHandlerBuilder(builder =>
        {
            if (string.IsNullOrEmpty(Settings.Proxy) == false)
            {
                builder.PrimaryHandler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (m, c, ch, e) => true,
                    Proxy = (string.IsNullOrEmpty(Settings.Proxy) ? (System.Net.IWebProxy)null : new System.Net.WebProxy(Settings.Proxy))
                };
            }
        });


    })

    .Build();


IHttpClientFactory fa = (IHttpClientFactory)host.Services.GetService(typeof(IHttpClientFactory));
var apiClient = fa?.CreateClient("api");
ExceptionHandling._httpClient = apiClient;

if (Settings.Debug)
{
        string msg = "START INFO\n"
        + Devmasters.Diags.Render(
                            Devmasters.Diags.GetOSInfo()
                                .Concat(Devmasters.Diags.GetProcessInfo())
                                .Concat(Devmasters.Diags.GetGarbageCollectorInfo())
                                .Concat(Devmasters.Diags.GetDrivesInfo())
                        );
        Console.WriteLine(msg);
        ExceptionHandling.SendLogToServer(msg, apiClient);
}
await host.RunAsync();






