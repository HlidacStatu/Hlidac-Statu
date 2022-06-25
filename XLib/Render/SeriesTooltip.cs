
using Newtonsoft.Json;

namespace HlidacStatu.XLib.Render
{
    public class SeriesTooltip
    {
        [JsonProperty("valuePrefix")]
        public string ValuePrefix { get; set; }
        [JsonProperty("valueSuffix")]
        public string ValueSuffix { get; set; }
        [JsonProperty("valueDecimals")]
        public int ValueDecimals { get; set; }
    }
}