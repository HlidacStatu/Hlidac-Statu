using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Globalization;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;


namespace AgendyOVM.Cinnosti
{
    public class Cinnost
    {
        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public string Id { get; set; }

        [JsonProperty("kód-činnosti", NullValueHandling = NullValueHandling.Ignore)]
        public string KódČinnosti { get; set; }

        [JsonProperty("název-činnosti", NullValueHandling = NullValueHandling.Ignore)]
        public Lang NázevČinnosti { get; set; }

        [JsonProperty("platnost-činnosti-do", NullValueHandling = NullValueHandling.Ignore)]
        public DateTimeOffset? PlatnostČinnostiDo { get; set; }

        [JsonProperty("platnost-činnosti-od", NullValueHandling = NullValueHandling.Ignore)]
        public DateTimeOffset? PlatnostČinnostiOd { get; set; }

        [JsonProperty("popis-činnosti", NullValueHandling = NullValueHandling.Ignore)]
        public Lang PopisČinnosti { get; set; }

        [JsonProperty("typ-činnosti", NullValueHandling = NullValueHandling.Ignore)]
        public string TypČinnosti { get; set; }

        [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)]
        public string Type { get; set; }
    }

    public partial class Lang
    {
        [JsonProperty("cs", NullValueHandling = NullValueHandling.Ignore)]
        public string Cs { get; set; }
        [JsonProperty("en", NullValueHandling = NullValueHandling.Ignore)]
        public string En { get; set; }
    }


}
