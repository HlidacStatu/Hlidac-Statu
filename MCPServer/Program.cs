using HlidacStatu.Entities;
using HlidacStatu.LibCore.Extensions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ModelContextProtocol.AspNetCore.Authentication;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ModelContextProtocol.AspNetCore.Authentication;
using System.Net.Http.Headers;
using System.Security.Claims;
using OpenIddict.Abstractions;
using OpenIddict.Server;

string CORSPolicy = "from_hlidacstatu.cz";

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureHostForWeb(args);

//init statics and others
Devmasters.Config.Init(builder.Configuration);
//System.Net.Http.HttpClient.DefaultProxy = new System.Net.WebProxy("127.0.0.1", 8888);

System.Globalization.CultureInfo.DefaultThreadCurrentCulture = HlidacStatu.Util.Consts.czCulture;
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = HlidacStatu.Util.Consts.csCulture;

var logger = Log.ForContext<Program>();

//tohle do produkce pak změnit na správný port
var serverUrl = "https://localhost:5552/";
builder.WebHost.UseUrls(serverUrl);



// service registration --------------------------------------------------------------------------------------------

// for scoped services (mainly for identity)
string connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<DbEntities>(options =>
{
    options.UseSqlServer(connectionString);
    options.UseOpenIddict();
});

// Add a DbContext to store your Database Keys (cookie single sign on)
builder.Services.AddDbContext<HlidacKeysContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDataProtection()
    .PersistKeysToDbContext<HlidacKeysContext>()
    .SetApplicationName("HlidacStatu");

builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
            .UseDbContext<DbEntities>();
    })
    .AddServer(options =>
    {
        // Enable the token endpoint.
        options.SetTokenEndpointUris("connect/token");
        options.SetAuthorizationEndpointUris("connect/authorize")
            .AllowAuthorizationCodeFlow();
        
        options.RegisterScopes("mcp:tools", "openid");

        
        // Enable the client credentials flow.
        options.AllowClientCredentialsFlow();

        // Register the signing and encryption credentials.
        options.AddDevelopmentEncryptionCertificate()
            .AddDevelopmentSigningCertificate();

        // Register the ASP.NET Core host and configure the ASP.NET Core options.
        options.UseAspNetCore()
            .EnableTokenEndpointPassthrough();
        
        options.AddEventHandler<OpenIddictServerEvents.HandleConfigurationRequestContext>(builder =>
        {
            builder.UseInlineHandler(context =>
            {
                context.Metadata.Add("registration_endpoint", new OpenIddictParameter(context.BaseUri + "connect/register"));
                return default;
            });
        });
    });

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultChallengeScheme = McpAuthenticationDefaults.AuthenticationScheme;
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        // Configure to validate tokens from our in-memory OAuth server
        options.Authority = serverUrl;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidAudience = serverUrl, // Validate that the audience matches the resource metadata as suggested in RFC 8707
            ValidIssuer = serverUrl,
            NameClaimType = "name",
            RoleClaimType = "roles"
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var name = context.Principal?.Identity?.Name ?? "unknown";
                var email = context.Principal?.FindFirstValue("preferred_username") ?? "unknown";
                Console.WriteLine($"Token validated for: {name} ({email})");
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"Authentication failed: {context.Exception.Message}");
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                Console.WriteLine($"Challenging client to authenticate with Entra ID");
                return Task.CompletedTask;
            }
        };
    })
    .AddMcp(options =>
    {
        options.ResourceMetadata = new()
        {
            Resource = new Uri(serverUrl),
            ResourceDocumentation = new Uri("https://docs.example.com/api/weather"),
            AuthorizationServers = { new Uri(serverUrl) },
            ScopesSupported = ["mcp:tools"],
        };
    });

    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();

//McpServerOptions mcp_server_options = new()
//{
//    ServerInfo = new ModelContextProtocol.Protocol.Implementation{ Name = "Hlidac statu MCP Server", Version = System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString() },
//};

_ = builder.Services.AddMcpServer(
    o => {
        o.ServerInfo = new ModelContextProtocol.Protocol.Implementation()
        {
            Name = "HlidacStatu.MCPServer",
            Version = System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString(),
            Title = "Hlidac statu MCP Server",
        };
        o.ServerInstructions = "This is MCP server for Hlidac statu. It provides access to data about Czech companies, contracts with Czech government, subsidies, czech politicians and other entities. Use tools to query data.";
    }
    )
    .WithHttpTransport(opt =>
    {
        //opt.PerSessionExecutionContext = true;

        opt.ConfigureSessionOptions = HlidacStatu.MCPServer.Code.Auth.ConfigureSessionCheckCookieAsync;
        //opt.RunSessionHandler = HlidacStatu.MCPServer.Code.Auth.RunSessionCheckCookieAsync;
    })
    .WithToolsFromAssembly()
    .WithResourcesFromAssembly();

builder.Services.AddHttpContextAccessor();

_ = builder.Services.AddOpenTelemetry()
    .WithTracing(b => b.AddSource("*")
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation())
    .WithMetrics(b => b.AddMeter("*")
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation())
    .UseOtlpExporter();


// Pipeline below -------------------------------------------------------------------------------------------------
var app = builder.Build();

app.Use(async (context, next) =>
{
    await next(context);
    Console.WriteLine($"===> [{context.Request.Method}] {context.Request.Path} ==> {context.Response.StatusCode}");
} );

app.MapMcp().RequireAuthorization();

app.UseHttpsRedirection();

app.UseForwardedHeaders();

app.UseRouting();
app.UseCors();

app.UseAuthentication(); 
app.UseAuthorization(); 

app.MapControllers();


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
