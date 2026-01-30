using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace HlidacStatu.ClassificationRepair
{
    public interface IHlidacService
    {
        Task<IEnumerable<string>> GetTextSmlouvyAsync(string id);
    }

    public class HlidacService : IHlidacService
    {
        private readonly HttpClient _httpClient;

        public HlidacService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // Result je json (array of strings), ale pro aktuální potřebu to nemá smysl přetypovat,
        // protože použijeme celý string. Stemmer si s tím poradí
        public async Task<IEnumerable<string>> GetTextSmlouvyAsync(string id)
        {
            var uri = new Uri($"smlouvy/text/{id}?addpredmet=1", UriKind.Relative);
            var json = await _httpClient.GetStringAsync(uri).ConfigureAwait(false);
            return System.Text.Json.JsonSerializer.Deserialize<IEnumerable<string>>(json);
        }

    }
}