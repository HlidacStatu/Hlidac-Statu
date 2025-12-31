using Hangfire;
using HlidacStatu.Entities;
using HlidacStatu.LibCore.Extensions;
using HlidacStatu.LibCore.Filters;
using HlidacStatu.LibCore.MiddleWares;
using HlidacStatu.LibCore.Services;
using HlidacStatuApi;
using HlidacStatuApi.Code;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using System.Diagnostics;
using ILogger = Serilog.ILogger;

string CORSPolicy = "from_hlidacstatu.cz";

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureHostForWeb(args);

//init statics and others
Devmasters.Config.Init(builder.Configuration);
//System.Net.Http.HttpClient.DefaultProxy = new System.Net.WebProxy("127.0.0.1", 8888);

System.Globalization.CultureInfo.DefaultThreadCurrentCulture = HlidacStatu.Util.Consts.czCulture;
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = HlidacStatu.Util.Consts.csCulture;

ILogger logger = Log.ForContext<Program>();

new Thread(
    () =>
    {
        logger.Information(
            "{action} {code} for {part} init during start.",
            "starting",
            "thread",
            "availability cache");
        Devmasters.DT.StopWatchEx sw = new Devmasters.DT.StopWatchEx();
        sw.Start();
        _ = HlidacStatuApi.Code.Availability.AllActiveServers24hoursStatAsync();
        _ = HlidacStatuApi.Code.Availability.AllActiveServersWeekStatAsync();
        sw.Stop();
        logger.Information(
            "{action} thread for {part} init during start in {duration} sec.",
            "ends",
            "availability cache",
            sw.Elapsed.TotalSeconds);
    }
).Start();

// Setup logging to be exported via OpenTelemetry
_ = builder.Logging.AddOpenTelemetry(logging =>
    {
        logging.IncludeFormattedMessage = true;
        logging.IncludeScopes = true;
    });


var OtlpEndpoint = Devmasters.Config.GetWebConfigValue("OTEL_EXPORTER_OTLP_ENDPOINT");


var otel = builder.Services.AddOpenTelemetry()
    .ConfigureResource(b => b
        .AddService(
            serviceName: "Api",
            serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown"
        )
    );


_ = otel.WithMetrics(metrics =>
{
    // Metrics provider from OpenTelemetry
    metrics.AddAspNetCoreInstrumentation();
    metrics.AddRuntimeInstrumentation();
    //Our custom metrics
    //metrics.AddMeter(greeterMeter.Name);
    // Metrics provides by ASP.NET Core in .NET 8
    metrics.AddMeter("Microsoft.AspNetCore.Hosting");
    metrics.AddMeter("Microsoft.AspNetCore.Server.Kestrel");
    // Metrics provided by System.Net libraries
    metrics.AddMeter("System.Net.Http");
    metrics.AddMeter("System.Net.NameResolution");
    metrics.AddMeter("Microsoft.EntityFrameworkCore");
    metrics.AddMeter("System.Data"); //just blind test shot
    metrics.AddMeter("Microsoft.Data"); //just blind test shot
    //metrics.AddMeter(SocialbannerInstrumentationSource.MeterName);
    metrics.AddFusionCacheInstrumentation(o => o.IncludeDistributedLevel = true);
});
_ = otel.WithTracing(tracing =>
{
    //tracing.AddSource(SocialbannerInstrumentationSource.ActivitySourceName);
    tracing.AddAspNetCoreInstrumentation();
    tracing.AddHttpClientInstrumentation();
    tracing.AddSqlClientInstrumentation();
    tracing.AddEntityFrameworkCoreInstrumentation();
    tracing.AddElasticsearchClientInstrumentation();
    tracing.AddFusionCacheInstrumentation(o =>
    {
        o.IncludeDistributedLevel = true;
    });

});



if (OtlpEndpoint != null)
{
    _ = otel.UseOtlpExporter(
         OtlpExportProtocol.Grpc,
         new Uri(OtlpEndpoint)
        );
}

var source = new ActivitySource("ApiProgramStart");

using (var activity = source.StartActivity("Starting prg"))
{
    activity?.SetTag("test.key", "test-value");
    activity?.SetTag("env", "production");
    // simulace práce
    Thread.Sleep(100);
}


// service registration --------------------------------------------------------------------------------------------

// for scoped services (mainly for identity)
string connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<DbEntities>(options =>
    options.UseSqlServer(connectionString));

// Add a DbContext to store your Database Keys (cookie single sign on)
builder.Services.AddDbContext<HSKeysContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDataProtection()
    .PersistKeysToDbContext<HSKeysContext>()
    .SetApplicationName("HlidacStatu");

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

AddIdentity(builder.Services);

builder.Services.AddSingleton<AttackerDictionaryService>();

builder.Services.AddControllers(
    options =>
    {
        // Insert the custom formatter at the beginning of the list
        options.InputFormatters.Insert(0, new TextPlainInputFormatter());
    }
    )
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

//Backroud blurredPage processing
builder.Services.AddSingleton<BlurredPageBackgroundQueue>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<BlurredPageBackgroundQueue>>();
    return new BlurredPageBackgroundQueue(logger, capacity: 300); 
});
builder.Services.AddHostedService<BlurredPageProcessor>();

_ = builder.Services
    .AddHealthChecks();

_ = builder.Services.AddHangfire(configuration => configuration
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

    app.UseDeveloperExceptionPage();
    //app.UseExceptionHandler("/Error/500");
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
    if (path.ToLower().StartsWith("/identity"))
    {
        context.Response.Redirect("https://www.hlidacstatu.cz"+path);
        return; // short circuit
    }

    await next(context);
});

#if !DEBUG
    app.UseHttpsRedirection();
#endif

app.UseOnHTTPErrorMiddleware();

app.UseSwagger();

app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v2/swagger.json", "API V2");
    c.EnableTryItOutByDefault();
});

app.UseRouting();

app.UseAuthentication();
app.UseApiAuthenticationMiddleware();
app.UseAuthorization();

app.UseCors(CORSPolicy);

app.UseEndpoints(endpoints => {
    endpoints.MapHealthChecks("/health"
            , new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions()
            {
                Predicate = _ => true,
                ResponseWriter = global::HealthChecks.UI.Client.UIResponseWriter.WriteHealthCheckUIResponse

            }
        ).WithMetadata(new AuthorizeAttribute() { Roles = "PrivateApi" });
});


app.MapControllers();


//todo: odstranit tohle, protože by to mělo fungovat jak to máme nakonfigurované v services.addhangfireserver
app.UseHangfireServer();

app.Run();






// Methods below -------------------------------------------------------------------------------------------------


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