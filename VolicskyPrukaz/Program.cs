using VolicskyPrukaz.Models;
using VolicskyPrukaz.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

var cacheSingleton = new AutocompleteCache();
builder.Services.AddSingleton(cacheSingleton);

builder.Services.AddSingleton<PdfGenerator>();

//Register hlidac
Devmasters.Config.Init(builder.Configuration);
System.Globalization.CultureInfo.DefaultThreadCurrentCulture = HlidacStatu.Util.Consts.czCulture;
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = HlidacStatu.Util.Consts.csCulture;

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

app.MapGet("/findAddress/{query}", (string query, AutocompleteCache autocompleteCache) =>
{
    var index = autocompleteCache.GetIndex();
    var result = index.Search(query, 10, adr => adr.TypOvm );
    return Results.Json(result.Select(x => x.Original).ToList());
});

// app.MapGet("/generateAc", (AutocompleteCache autocompleteCache) =>
// {
//     autocompleteCache.SerializeAc();
//     return Results.Ok("hotovo");
// });

app.Run();