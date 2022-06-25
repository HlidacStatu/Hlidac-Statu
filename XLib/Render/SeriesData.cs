
using Newtonsoft.Json;

namespace HlidacStatu.XLib.Render
{
    public class SeriesData
    {
        public SeriesData() { }
        public SeriesData(int x, decimal y)
        {
            X = x;
            Y = y;
        }
        [JsonProperty("x")]
        public int X { get; set; }
        [JsonProperty("y")]
        public decimal Y { get; set; }
    }
}