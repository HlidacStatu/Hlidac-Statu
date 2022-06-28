using HlidacStatu.Entities;
using HlidacStatu.Entities.Entities;
using HlidacStatu.LibCore.Filters;
using HlidacStatu.LibCore.MiddleWares;
using HlidacStatu.LibCore.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

//init statics and others
Devmasters.Config.Init(builder.Configuration);

System.Globalization.CultureInfo.DefaultThreadCurrentCulture = HlidacStatu.Util.Consts.czCulture;
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = HlidacStatu.Util.Consts.csCulture;



// Add services to the container.
string connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
// for scoped services (mainly for identity)
builder.Services.AddDbContext<DbEntities>(options =>
    options.UseSqlServer(connectionString));

// Add a DbContext to store your Database Keys
builder.Services.AddDbContext<HlidacKeysContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDataProtection()
    .PersistKeysToDbContext<HlidacKeysContext>()
    .SetApplicationName("HlidacStatu");

AddIdentity(builder.Services);

builder.Services.AddSingleton<AttackerDictionaryService>();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v2", new OpenApiInfo
    {
        Version = "v2",
        Title = "HlidacStatu Api V2.1.1",
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






builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseRequestTrackMiddleware(new RequestTrackMiddleware.Options()
{
    LimitToPaths = new List<string> {"/api"},
    ApplicationName = "HlidacstatuApi"
});
            
//request time measurement
app.UseTimeMeasureMiddleware();

app.UseBannedIpsMiddleware(); // tohle nechci při developmentu :) 
app.UseExceptionHandler("/Error/500");


app.UseSwagger();
            
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v2/swagger.json", "API V2");
    //c.RoutePrefix = "api/v2/swagger";
});

app.UseHttpsRedirection();

app.UseApiAuthenticationMiddleware();

app.UseAuthorization();

app.MapControllers();

app.Run();



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