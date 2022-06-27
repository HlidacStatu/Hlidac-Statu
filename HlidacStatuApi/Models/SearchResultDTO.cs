using Newtonsoft.Json;

namespace HlidacStatuApi.Models
{
    [JsonObject(MemberSerialization.OptOut)]
    public class SearchResultDTO<T>
    {
        public long Total { get; set; }
        public int Page { get; set; }
        public IEnumerable<T> Results { get; set; }

        public SearchResultDTO(long total, int page, IEnumerable<T> obj)
        {
            Total = total;
            Page = page;
            Results = obj;
        }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}