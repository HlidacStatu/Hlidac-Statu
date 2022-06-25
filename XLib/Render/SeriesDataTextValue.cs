
using Newtonsoft.Json;

namespace HlidacStatu.XLib.Render
{
    public class SeriesDataTextValue
    {
        public SeriesDataTextValue() { }
        public SeriesDataTextValue(string name, decimal value)
        {
            Name = name;
            Y = value;
        }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("y")]
        public decimal Y { get; set; }
    }
}