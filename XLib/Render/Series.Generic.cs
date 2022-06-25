
using Newtonsoft.Json;

namespace HlidacStatu.XLib.Render
{
    public class Series<T>
    {



        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("data")]
        public T[] Data { get; set; }
    }
}