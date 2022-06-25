
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace HlidacStatu.XLib.Render
{
    public class Series : Series<SeriesData>
    {
        [JsonProperty("color")]
        public string Color { get; set; }

        public enum SeriesType
        {
            column, line
        }
        [JsonProperty("type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public SeriesType? Type { get; set; } = null;


        private int? _yAxis = null;
        [JsonProperty("yAxis")]
        public int YAxis
        {
            get
            {
                if (_yAxis is null)
                {
                    switch (Type)
                    {
                        case SeriesType.column:
                            return 0;
                        case SeriesType.line:
                            return 1;
                        default:
                            return 0;
                    }
                }
                return _yAxis.Value;
            }

            set
            {
                _yAxis = value;
            }
        }
        [JsonProperty("tooltip")]
        public SeriesTooltip SeriesTooltip { get; set; }

    }
}