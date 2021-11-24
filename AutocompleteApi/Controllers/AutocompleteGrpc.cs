using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using GrpcProtobufs;
using HlidacStatu.AutocompleteApi.Services;
using ProtoBuf.Grpc;

namespace HlidacStatu.AutocompleteApi.Controllers
{
    public class AutocompleteGrpc : IAutocompleteGrpc
    {
        private readonly IMemoryStoreService _memoryStore;
        
        private JsonSerializerOptions _jsonOptions = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public AutocompleteGrpc(IMemoryStoreService memoryStore)
        {
            _memoryStore = memoryStore;
        }

        public Task<AutocompleteResponse> AutocompleteAsync(AutocompleteRequest request, CallContext context = default)
        {
            var searchResult = _memoryStore.HlidacFulltextIndex.Search(request.Query, 5, ac => ac.Priority);

            var autocomplete = searchResult.Select(r => r.Original).FirstOrDefault();
            
            var jsonMessage = JsonSerializer.Serialize(autocomplete, _jsonOptions);
            
            return Task.FromResult(new AutocompleteResponse()
            {
                JsonMessage = jsonMessage
            });
        }

        public Task<AutocompleteResponse> TestAutocompleteAsync(AutocompleteRequest request,
            CallContext context = default)
        {
            var searchResult = _memoryStore.SmallSampleIndex.Search(request.Query, 5, ac => ac.Priority);

            var autocomplete = searchResult.Select(r => r.Original).FirstOrDefault();
            
            var jsonMessage = JsonSerializer.Serialize(autocomplete, _jsonOptions);
            
            return Task.FromResult(new AutocompleteResponse()
            {
                JsonMessage = jsonMessage
            });
        }
    }
}