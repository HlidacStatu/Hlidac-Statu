using System.Text.Json;

namespace PoliticiEditor.Components.Autocomplete;

public class AutocompleteService
{
    private static readonly string Endpoint = Devmasters.Config.GetWebConfigValue("AutocompleteEndpoint");

    private readonly IHttpClientFactory _httpClientFactory;

    private JsonSerializerOptions? _jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AutocompleteService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<List<HlidacStatu.DS.Api.Autocomplete>> FindPerson(string q, CancellationToken ctx)
    {
        var autocompletePath = $"/autocomplete/autocomplete?q={q}&category=Person";

        return await RunAutocompleteQuery(ctx, autocompletePath);
    }
    
    public async Task<List<HlidacStatu.DS.Api.Autocomplete>> FindCompany(string q, CancellationToken ctx)
    {
        var autocompletePath = $"/autocomplete/autocomplete?q={q}&category=Company StateCompany Authority City";

        return await RunAutocompleteQuery(ctx, autocompletePath);
    }
    

    private async Task<List<HlidacStatu.DS.Api.Autocomplete>> RunAutocompleteQuery(CancellationToken ctx, string autocompletePath)
    {
        var uri = new Uri($"{Endpoint}{autocompletePath}");
        using var client = _httpClientFactory.CreateClient();

        try
        {
            using var response = await client.GetAsync(uri, ctx);

            _ = response.EnsureSuccessStatusCode();


            var datastream = await response.Content.ReadAsStreamAsync(ctx);

            var autocomplete = await JsonSerializer.DeserializeAsync<List<HlidacStatu.DS.Api.Autocomplete>>(datastream,
                cancellationToken: ctx,
                options: _jsonSerializerOptions);

            return autocomplete ?? Enumerable.Empty<HlidacStatu.DS.Api.Autocomplete>().ToList();
        }
        catch (Exception ex) when (ex is OperationCanceledException || ex is TaskCanceledException)
        {
            // canceled by user
        }
        catch (Exception e)
        {
            //log exception?
        }

        return Enumerable.Empty<HlidacStatu.DS.Api.Autocomplete>().ToList();
    }
}