using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace PlatyUredniku;

/// <summary>
/// Middleware nastavuje default year pro politiky a uredniky na zaklade appsettings.json pro adminy
/// Pro lidi by mela zustat default hodnota z purepo a pprepo
/// </summary>
public class DefaultYearMiddleware
{
    private readonly RequestDelegate _next;
    private readonly int? _adminPuYear;
    private readonly int? _adminPpYear;

    public DefaultYearMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _adminPuYear = configuration.GetValue<int?>("AdminYears:Urednici");
        _adminPpYear = configuration.GetValue<int?>("AdminYears:Politici");
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.IsInRole("Admin"))
        {
            YearPicker.Set(_adminPuYear, _adminPpYear);
        }

        await _next(context);
    }
}
