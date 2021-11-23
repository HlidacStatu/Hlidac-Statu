using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace HlidacStatu.AutocompleteApi.Services
{
    public class HlidacApiService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public HlidacApiService(IHttpClientFactory httpClientFactory) =>
            _httpClientFactory = httpClientFactory;


        public async Task<byte[]> LoadFullAutocomplete()
        {
            var client = _httpClientFactory.CreateClient(AppConstants.HttpClientName);
            var response = await client.GetByteArrayAsync("https://www.hlidacstatu.cz/api/v2/generateAutocompleteData");

            var decompressed = await DecompressBytesAsync(response);

            return decompressed;
        }


        private async Task<byte[]> DecompressBytesAsync(byte[] bytes, CancellationToken cancel = default)
        {
            await using var inputStream = new MemoryStream(bytes);
            await using var outputStream = new MemoryStream();
            await using var decompressStream = new BrotliStream(inputStream, CompressionMode.Decompress);

            await decompressStream.CopyToAsync(outputStream, cancel);

            return outputStream.ToArray();
        }
    }
}