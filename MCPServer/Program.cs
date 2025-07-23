using HlidacStatu.Entities;
using HlidacStatu.LibCore.Extensions;
using HlidacStatu.LibCore.Filters;
using HlidacStatu.LibCore.MiddleWares;
using HlidacStatu.LibCore.Services;
using HlidacStatu.MCPServer.Resources;
using HlidacStatu.MCPServer.Tools;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using ModelContextProtocol.Server;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog;
using static NPOI.SS.Formula.Functions.Countif;
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

bool enableAuth = false;





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
    .WithResourcesFromAssembly()
    ;

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
app.MapMcp();



if (enableAuth)
{
    app.UseAuthentication();
    app.UseApiAuthenticationMiddleware();
    app.UseAuthorization();
}

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
