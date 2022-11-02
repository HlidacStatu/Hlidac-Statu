using HlidacStatu.LibCore;
using Microsoft.AspNetCore.Mvc;
using VolicskyPrukaz.Services;
using Polly;
using VolicskyPrukaz;

var builder = WebApplication.CreateBuilder(args);
//builder.Host.ConfigureHostForWeb(args); --cant use here since it doesnt need keys from hs

//Register hlidac
Devmasters.Config.Init(builder.Configuration);
System.Globalization.CultureInfo.DefaultThreadCurrentCulture = HlidacStatu.Util.Consts.czCulture;
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = HlidacStatu.Util.Consts.csCulture;

// Add services to the container.
builder.Services.AddRazorPages();

builder.Services.AddHttpClient("default")
    .AddTransientHttpErrorPolicy(policyBuilder =>
        policyBuilder.WaitAndRetryAsync(
            3, retryNumber => TimeSpan.FromMilliseconds(10)));

builder.Services.AddSingleton<PdfGenerator>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.MapGet("/findAddress/{query}", async (string query,
        [FromServices] IHttpClientFactory httpClientFactory,
        CancellationToken ctx) =>
{
        var autocompleteHost = Devmasters.Config.GetWebConfigValue("AutocompleteEndpoint");
        var autocompleteQuery = $"?q={query}";
        var uri = new Uri($"{autocompleteHost}{autocompleteQuery}");
        using var client = httpClientFactory.CreateClient(Constants.DefaultHttpClient);

        try
        {
            var response = await client.GetAsync(uri, ctx);
          
            return new HttpResponseMessageResult(response);
        }
        catch (Exception ex) when ( ex is OperationCanceledException || ex is TaskCanceledException)
        {
            // canceled by user
            //Util.Consts.Logger.Info("Autocomplete canceled by user");
        }
        catch (Exception e)
        {
            //Util.Consts.Logger.Warning("Autocomplete API problem.", e, new { query });
        }

        return Results.NoContent();
    
});


app.Run();