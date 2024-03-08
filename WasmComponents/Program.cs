using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using WasmComponents.Components.Autocomplete;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddScoped(_ => new HttpClient());

var autocompleteWrapHack = typeof(AutocompleteWrap);

await builder.Build().RunAsync();
