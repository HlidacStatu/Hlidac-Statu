using Devmasters.Log;
using Hangfire;
using HlidacStatu.Entities;
using HlidacStatu.LibCore.Extensions;
using HlidacStatu.LibCore.Filters;
using HlidacStatu.LibCore.MiddleWares;
using HlidacStatu.LibCore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;

string CORSPolicy = "from_hlidacstatu.cz";

var builder = WebApplication.CreateBuilder(args);
builder.Host.ConfigureHostForWeb(args);

//init statics and others
Devmasters.Config.Init(builder.Configuration);
//System.Net.Http.HttpClient.DefaultProxy = new System.Net.WebProxy("127.0.0.1", 8888);

System.Globalization.CultureInfo.DefaultThreadCurrentCulture = HlidacStatu.Util.Consts.czCulture;
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = HlidacStatu.Util.Consts.csCulture;

new Thread(
    () =>
    {
        HlidacStatuApi.Code.Log.Logger.Info(
            "{action} {code} for {part} init during start.",
            "starting",
            "thread",
            "availability cache");
        Devmasters.DT.StopWatchEx sw = new Devmasters.DT.StopWatchEx();
        sw.Start();
        _ = HlidacStatuApi.Code.Availability.AllActiveServers24hoursStat();
        _ = HlidacStatuApi.Code.Availability.AllActiveServersWeekStat();
        sw.Stop();
        HlidacStatuApi.Code.Log.Logger.Info(
            "{action} thread for {part} init during start in {duration} sec.",
            "ends",
            "availability cache",
            sw.Elapsed.TotalSeconds);
    }
).Start();

// service registration --------------------------------------------------------------------------------------------

// for scoped services (mainly for identity)
string connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<DbEntities>(options =>
    options.UseSqlServer(connectionString));

// Add a DbContext to store your Database Keys (cookie single sign on)
builder.Services.AddDbContext<HlidacKeysContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDataProtection()
    .PersistKeysToDbContext<HlidacKeysContext>()
    .SetApplicationName("HlidacStatu");

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: CORSPolicy,
        policy =>
        {
            policy.SetIsOriginAllowedToAllowWildcardSubdomains()
                  .WithOrigins("https://*.hlidacstatu.cz")
                  .AllowAnyMethod()
                  .AllowCredentials()
                  .AllowAnyHeader()
                  .Build();
        });
});

AddIdentity(builder.Services);

builder.Services.AddSingleton<AttackerDictionaryService>();

builder.Services.AddControllers()
    .AddNewtonsoftJson(); // this needs to be added, so datasety's Registration string[,] property can be serialized 

//swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v2", new OpenApiInfo
    {
        Version = "v2",

        Title = "HlidacStatu Api " + System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString(),
        Description = "REST API Hlídače státu",
        TermsOfService = new Uri("https://www.hlidacstatu.cz/texty/provoznipodminky/"),
        Contact = new OpenApiContact
        {
            Name = "Hlídač státu",
            Email = "podpora@hlidacstatu.cz",
            Url = new Uri("https://www.hlidacstatu.cz/texty/kontakt/"),
        },
        License = new OpenApiLicense
        {
            Name = "CC BY 3.0 CZ",
            Url = new Uri("https://www.hlidacstatu.cz/texty/licence/"),
        }
    });

    c.AddSecurityDefinition("apiKey", new OpenApiSecurityScheme()
    {
        Type = SecuritySchemeType.ApiKey,
        Description = "API Key Authentication",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Scheme = "apiKey"
    });
    c.OperationFilter<ApiAuthHeaderParameter>();

    // Set the comments path for the Swagger JSON and UI.
    var xmlFile = "HlidacStatuApi.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
    c.CustomSchemaIds(x => x.FullName);
});

builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, HlidacStatuApi.Code.SpecificApiAuthorizationMiddlewareResultHandler>();


bool _shouldRunHealthcheckFeature = false;
string healthcheckFeatureOption = Devmasters.Config.GetWebConfigValue("RunHealthcheckFeature");
_shouldRunHealthcheckFeature = !string.IsNullOrWhiteSpace(healthcheckFeatureOption) &&
                               healthcheckFeatureOption == "true";

if (_shouldRunHealthcheckFeature)
{
    AddAllHealtChecks(builder.Services, builder.Configuration);

}
_= builder.Services.AddHangfire(configuration => configuration
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()

        .UseSqlServerStorage(Devmasters.Config.GetWebConfigValue("HangFireConnection"))
            .WithJobExpirationTimeout(TimeSpan.FromMinutes(55))
        
        );
_= builder.Services.AddHangfireServer();

// Pipeline below -------------------------------------------------------------------------------------------------
var app = builder.Build();


app.UseRequestTrackMiddleware(new RequestTrackMiddleware.Options()
{
    LimitToPaths = new List<string> { "/api" },
    ApplicationName = "HlidacstatuApi"
});

app.UseTimeMeasureMiddleware();

if (IsDevelopment(app.Environment))
{
    app.UseDeveloperExceptionPage();
}
else
{
    var whitelistIps = Devmasters.Config.GetWebConfigValue("BanWhitelist")?.Split(',',
        StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    BannedIpsMiddleware.Whitelist whitelist = new BannedIpsMiddleware.Whitelist();
    if (whitelistIps != null && whitelistIps.Length > 0)
    {
        foreach (var ip in whitelistIps)
        {
            whitelist.IpAddresses.Add(ip);
        }
    }
    app.UseBannedIpsMiddleware(whitelist); // tohle nechci při developmentu :) 
    app.UseExceptionHandler("/Error/500");
}

// redirect to apikey landing page
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value;
    // Redirect to an external URL
    if (path == "" || path == "/")
    {
        context.Response.Redirect("https://www.hlidacstatu.cz/api");
        return; // short circuit
    }

    await next(context);
});

#if !DEBUG
    app.UseHttpsRedirection();
#endif

app.UseOnHTTPErrorMiddleware();


app.UseCors(CORSPolicy);


app.UseSwagger();

app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v2/swagger.json", "API V2");
    c.EnableTryItOutByDefault();
});

app.UseAuthentication();
app.UseApiAuthenticationMiddleware();

app.UseAuthorization();

app.MapControllers();


if (_shouldRunHealthcheckFeature)
{
    app.UseHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions()
    {
        Predicate = _ => true,
        ResponseWriter = global::HealthChecks.UI.Client.UIResponseWriter.WriteHealthCheckUIResponse
    }).UseHealthChecks("/_health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions()
    {
        Predicate = _ => true,
    });
}

//todo: odstranit tohle, protože by to mělo fungovat jak to máme nakonfigurované v services.addhangfireserver
app.UseHangfireServer();

app.Run();






// Methods below -------------------------------------------------------------------------------------------------

void AddAllHealtChecks(IServiceCollection services, IConfiguration Configuration)
{
    var conf = Configuration.GetSection("HealthChecks");

    _ = services
        .AddHealthChecks()
        .AddProcessAllocatedMemoryHealthCheck(maximumMegabytesAllocated: 20000,
            name: "Web server využitá pamět",
            tags: new[] { "Web server", "process", "memory" })
        .AddHealthCheckWithResponseTime(
            new global::HealthChecks.SqlServer.SqlServerHealthCheck(
                new HealthChecks.SqlServer.SqlServerHealthCheckOptions() { 
                    ConnectionString = Devmasters.Config.GetWebConfigValue("OldEFSqlConnection"),
                     CommandText= "select top 1 username from AspNetUsers"
                }),
            "SQL server", HealthStatus.Unhealthy, tags: new[] { "DB", "db" }
        )
        .AddHealthCheckWithOptions<HlidacStatu.Web.HealthChecks.ProcessOpenPorts>(
            "Open TCP ports",
            tags: new[] { "Web server" }
            )
        .AddHealthCheckWithOptions<HlidacStatu.Web.HealthChecks.ElasticSearchClusterStatus, HlidacStatu.Web.HealthChecks.ElasticSearchClusterStatus.Options>(
            new HlidacStatu.Web.HealthChecks.ElasticSearchClusterStatus.Options()
            {
                ExpectedNumberOfNodes = 16,
                ElasticServerUris = Devmasters.Config.GetWebConfigValue("ESConnection").Split(';')
            },
            "Elastic cluster", tags: new[] { "DB", "elastic" })
        .AddHealthCheckWithOptions<HlidacStatu.Web.HealthChecks.ElasticSearchNodesFreeDisk, HlidacStatu.Web.HealthChecks.ElasticSearchNodesFreeDisk.Options>(
            new HlidacStatu.Web.HealthChecks.ElasticSearchNodesFreeDisk.Options()
            {
                ExpectedNumberOfNodes = 16,
                ElasticServerUris = Devmasters.Config.GetWebConfigValue("ESConnection").Split(';'),
                MinimumFreeSpaceInMegabytes = 5000
            },
            "Elastic nodes disk space", tags: new[] { "DB", "elastic" })
        .AddHealthCheckWithOptions<HlidacStatu.Web.HealthChecks.NetworkDiskStorage, HlidacStatu.Web.HealthChecks.NetworkDiskStorage.Options>(
            new HlidacStatu.Web.HealthChecks.NetworkDiskStorage.Options()
            {
                UNCPath = "c:\\",
                DegradedMinimumFreeMegabytes = 10 * 1024, //10G 
                UnHealthtMinimumFreeMegabytes = 1 * 1024 //1GB
            },
            "System disk", HealthStatus.Unhealthy, tags: new[] { "Web server" }
        )

        .AddHealthCheckWithOptions<HlidacStatu.Web.HealthChecks.NetworkDiskStorage, HlidacStatu.Web.HealthChecks.NetworkDiskStorage.Options>(
            new HlidacStatu.Web.HealthChecks.NetworkDiskStorage.Options()
            {
                UNCPath = Devmasters.Config.GetWebConfigValue("FileCachePath"),
                DegradedMinimumFreeMegabytes = 10 * 1024, //10G 
                UnHealthtMinimumFreeMegabytes = 1 * 1024 //1GB
            },
            "Cache disk", HealthStatus.Unhealthy, tags: new[] { "Web server" }
        )
        .AddHealthCheckWithResponseTime(
            new global::HealthChecks.Network.SmtpHealthCheck(new global::HealthChecks.Network.SmtpHealthCheckOptions()
            {
                Host = "10.10.100.147",
                Port = 25,
                ConnectionType = global::HealthChecks.Network.Core.SmtpConnectionType.PLAIN
            }),
            "SMTP", HealthStatus.Degraded, tags: new[] { "Web server" }
        )
        .AddCheck<HlidacStatu.Web.HealthChecks.OCRServer>("OCR servers", tags: new[] { "OCR cloud" })
        .AddCheck<HlidacStatu.Web.HealthChecks.OCRQueue>("OCR queues", tags: new[] { "OCR cloud" })
        .AddCheck<HlidacStatu.Web.HealthChecks.SmlouvyZpracovane>("Zpracované smlouvy", tags: new[] { "Data" })
        .AddCheck<HlidacStatu.Web.HealthChecks.VerejneZakazkyZpracovane>("Zpracované VZ", tags: new[] { "Data" })
        .AddHealthCheckWithOptions<HlidacStatu.Web.HealthChecks.DatasetZpracovane, HlidacStatu.Web.HealthChecks.DatasetZpracovane.Options>(
            new HlidacStatu.Web.HealthChecks.DatasetZpracovane.Options()
            {
                DatasetId = "vyjadreni-politiku",
                MinRecordsInInterval = 100,
                Interval = HlidacStatu.Web.HealthChecks.DatasetZpracovane.IntervalEnum.Day
            }, "Dataset Vyjadření politiků", HealthStatus.Unhealthy, tags: new[] { "Data" })
        .AddHealthCheckWithOptions<HlidacStatu.Web.HealthChecks.DatasetZpracovane, HlidacStatu.Web.HealthChecks.DatasetZpracovane.Options>(
            new HlidacStatu.Web.HealthChecks.DatasetZpracovane.Options()
            {
                DatasetId = "veklep",
                MinRecordsInInterval = 30,
                Interval = HlidacStatu.Web.HealthChecks.DatasetZpracovane.IntervalEnum.Week
            }, "Dataset VEKLEP", HealthStatus.Unhealthy, tags: new[] { "Data" })
        .AddHealthCheckWithOptions<HlidacStatu.Web.HealthChecks.DatasetZpracovane, HlidacStatu.Web.HealthChecks.DatasetZpracovane.Options>(
            new HlidacStatu.Web.HealthChecks.DatasetZpracovane.Options()
            {
                DatasetId = "rozhodnuti-uohs",
                MinRecordsInInterval = 10,
                Interval = HlidacStatu.Web.HealthChecks.DatasetZpracovane.IntervalEnum.Week
            }, "Dataset Rozhodnuti UOHS", HealthStatus.Unhealthy, tags: new[] { "Data" })
        .AddHealthCheckWithOptions<HlidacStatu.Web.HealthChecks.DatasetyStatistika, HlidacStatu.Web.HealthChecks.DatasetyStatistika.Options>(
            new HlidacStatu.Web.HealthChecks.DatasetyStatistika.Options()
            {
                Exclude = new string[] { "rozhodnuti-uohs", "veklep", "vyjadreni-politiku" },
                Interval = HlidacStatu.Web.HealthChecks.DatasetyStatistika.IntervalEnum.Month
            }, "Statistiky malých databází", HealthStatus.Unhealthy, tags: new[] { "Data" })

        .AddHealthCheckWithOptions<HlidacStatu.Web.HealthChecks.CamelotApis, HlidacStatu.Web.HealthChecks.CamelotApis.Options>(
            new HlidacStatu.Web.HealthChecks.HCConfig<HlidacStatu.Web.HealthChecks.CamelotApis.Options>(conf).ConfigData,
            "Camelot APIs", HealthStatus.Unhealthy, tags: new[] { "Docker" })

        .AddHealthCheckWithOptions<HlidacStatu.Web.HealthChecks.ProxmoxVMs, HlidacStatu.Web.HealthChecks.ProxmoxVMs.Options>(
            new HlidacStatu.Web.HealthChecks.HCConfig<HlidacStatu.Web.HealthChecks.ProxmoxVMs.Options>(conf, "Proxmox.VM.100.99").ConfigData,
            "Proxmox 100.99", HealthStatus.Unhealthy, tags: new[] { "VMs" })
        .AddHealthCheckWithOptions<HlidacStatu.Web.HealthChecks.ProxmoxVMs, HlidacStatu.Web.HealthChecks.ProxmoxVMs.Options>(
            new HlidacStatu.Web.HealthChecks.HCConfig<HlidacStatu.Web.HealthChecks.ProxmoxVMs.Options>(conf, "Proxmox.VM.100.97").ConfigData,
            "Proxmox 100.97", HealthStatus.Unhealthy, tags: new[] { "VMs" })
        .AddHealthCheckWithOptions<HlidacStatu.Web.HealthChecks.ProxmoxVMs, HlidacStatu.Web.HealthChecks.ProxmoxVMs.Options>(
            new HlidacStatu.Web.HealthChecks.HCConfig<HlidacStatu.Web.HealthChecks.ProxmoxVMs.Options>(conf, "Proxmox.VM.100.95").ConfigData,
            "Proxmox 100.95", HealthStatus.Unhealthy, tags: new[] { "VMs" })
        .AddHealthCheckWithOptions<HlidacStatu.Web.HealthChecks.ProxmoxVMs, HlidacStatu.Web.HealthChecks.ProxmoxVMs.Options>(
            new HlidacStatu.Web.HealthChecks.HCConfig<HlidacStatu.Web.HealthChecks.ProxmoxVMs.Options>(conf, "Proxmox.VM.pve-hs-01-r540").ConfigData,
            "Proxmox pve-hs-01-r540 (02.161)", HealthStatus.Unhealthy, tags: new[] { "VMs" })
        .AddHealthCheckWithOptions<HlidacStatu.Web.HealthChecks.ProxmoxVMs, HlidacStatu.Web.HealthChecks.ProxmoxVMs.Options>(
            new HlidacStatu.Web.HealthChecks.HCConfig<HlidacStatu.Web.HealthChecks.ProxmoxVMs.Options>(conf, "Proxmox.VM.pve-hs-02-r720xd").ConfigData,
            "Proxmox pve-hs-02-r720xd (02.167)", HealthStatus.Unhealthy, tags: new[] { "VMs" })
        ;
}

static bool IsDevelopment(IHostEnvironment hostEnvironment)
{
    if (hostEnvironment == null)
    {
        throw new ArgumentNullException(nameof(hostEnvironment));
    }

    return hostEnvironment.IsEnvironment("Petr") ||
           hostEnvironment.IsEnvironment("Michal") ||
           hostEnvironment.IsEnvironment("Development");
}

void AddIdentity(IServiceCollection services)
{
    services.AddDefaultIdentity<ApplicationUser>(options =>
        {
            options.SignIn.RequireConfirmedAccount = true;

            options.User.RequireUniqueEmail = true;

            options.Password.RequiredLength = 6;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireDigit = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireUppercase = false;

            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.MaxFailedAccessAttempts = 5;
        })
        .AddRoles<IdentityRole>()
        .AddEntityFrameworkStores<DbEntities>();

    // this is needed because passwords are stored with old hashes
    services.Configure<PasswordHasherOptions>(options =>
        options.CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV2
    );

    services.ConfigureApplicationCookie(o =>
    {
        o.Cookie.Domain = ".hlidacstatu.cz";
        o.Cookie.Name = "HlidacLoginCookie"; // Name of cookie     

        o.Cookie.SameSite = SameSiteMode.Lax;
    });
}