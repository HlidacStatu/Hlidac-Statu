using Devmasters.Log;
using HlidacStatu.Entities;
using HlidacStatu.Entities.Entities;
using HlidacStatu.LibCore.Filters;
using HlidacStatu.LibCore.MiddleWares;
using HlidacStatu.LibCore.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

string CORSPolicy = "from_hlidacstatu.cz";

var builder = WebApplication.CreateBuilder(args);

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
            "ending",
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
});


// Pipeline below -------------------------------------------------------------------------------------------------
var app = builder.Build();

app.UseRequestTrackMiddleware(new RequestTrackMiddleware.Options()
{
    LimitToPaths = new List<string> { "/api" },
    ApplicationName = "HlidacstatuApi"
});

var timeMeasureLogger = Devmasters.Log.Logger.CreateLogger("Api.PageTimes",
    Devmasters.Log.Logger.DefaultConfiguration()
        .Enrich.WithProperty("codeversion", System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString())
        //.AddLogStash(new Uri("http://10.10.150.203:5000"))
        .AddFileLoggerFilePerLevel("c:/Data/Logs/HlidacStatu/Api/Web.PageTimes", "slog.txt",
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {SourceContext} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
            rollingInterval: Serilog.RollingInterval.Day,
            fileSizeLimitBytes: null,
            retainedFileCountLimit: 9,
            shared: true
        ));

app.UseTimeMeasureMiddleware(timeMeasureLogger);

if (IsDevelopment(app.Environment))
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseBannedIpsMiddleware(); // tohle nechci při developmentu :) 
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


Devmasters.Log.Logger apiExceptionLogger = Devmasters.Log.Logger.CreateLogger("Api.Exceptions",
    Devmasters.Log.Logger.DefaultConfiguration()
        .Enrich.WithProperty("codeversion", System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString())
        //.AddLogStash(new Uri("http://10.10.150.203:5000"))
        .AddFileLoggerFilePerLevel("c:/Data/Logs/HlidacStatu/Api", "slog.txt",
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {SourceContext} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
            rollingInterval: Serilog.RollingInterval.Day,
            fileSizeLimitBytes: null,
            retainedFileCountLimit: 9,
            shared: true
        ));
app.UseOnHTTPErrorMiddleware(apiExceptionLogger);


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


HlidacStatuApi.Code.Log.Logger.Info("{action} {code}.", "starting", "web API");
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