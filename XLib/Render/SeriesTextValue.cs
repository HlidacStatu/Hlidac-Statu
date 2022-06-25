
using Newtonsoft.Json;

namespace HlidacStatu.XLib.Render
{
    public class SeriesTextValue : Series<SeriesDataTextValue>
    {
        [JsonProperty("colorByPoint")]
        public bool ColorByPoint { get; set; }


    }
}