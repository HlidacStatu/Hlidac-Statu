using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace HlidacStatu.AutocompleteApi.Services
{
    public class HlidacApiService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly HlidacApiOptions _options; 
        
        public HlidacApiService(IHttpClientFactory httpClientFactory, IOptions<HlidacApiOptions> options)
        {
            _httpClientFactory = httpClientFactory;
            _options = options.Value;
        }


        public async Task<byte[]> LoadFullAutocomplete()
        {
            var client = _httpClientFactory.CreateClient(AppConstants.HttpClientName);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", _options.Token );

            try
            {
                var response = await client.GetByteArrayAsync(_options.AutocompleteUri);
                return response;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            

            return null;
        }
    }

    public class HlidacApiOptions
    {
        public string Token { get; set; }
        public string AutocompleteUri { get; set; }
    }
}