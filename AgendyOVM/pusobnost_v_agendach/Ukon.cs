using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgendyOVM.Ukony
{
    public class Ukon
    {
        [JsonProperty("digitální", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Digitální { get; set; }

        [JsonProperty("fáze", NullValueHandling = NullValueHandling.Ignore)]
        public string[] Fáze { get; set; }

        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public string Id { get; set; }

        [JsonProperty("identifikátor-úkonu", NullValueHandling = NullValueHandling.Ignore)]
        public string IdentifikátorÚkonu { get; set; }

        [JsonProperty("kanály", NullValueHandling = NullValueHandling.Ignore)]
        public Kanály[] Kanály { get; set; }

        [JsonProperty("název-úkonu", NullValueHandling = NullValueHandling.Ignore)]
        public NázevÚkonu NázevÚkonu { get; set; }

        [JsonProperty("popis-úkonu", NullValueHandling = NullValueHandling.Ignore)]
        public PopisÚkonu PopisÚkonu { get; set; }

        [JsonProperty("typ-vykonavatele-úkonu", NullValueHandling = NullValueHandling.Ignore)]
        public string TypVykonavateleÚkonu { get; set; }

        [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)]
        public string Type { get; set; }

        [JsonProperty("ustanovení-úkonu", NullValueHandling = NullValueHandling.Ignore)]
        public UstanoveníÚkonu[] UstanoveníÚkonu { get; set; }

        [JsonProperty("vhodný-k-digitalizaci", NullValueHandling = NullValueHandling.Ignore)]
        public bool? VhodnýKDigitalizaci { get; set; }
    }

    public partial class Kanály
    {
        [JsonProperty("plán-do", NullValueHandling = NullValueHandling.Ignore)]
        public DateTimeOffset? PlánDo { get; set; }

        [JsonProperty("plán-od", NullValueHandling = NullValueHandling.Ignore)]
        public DateTimeOffset? PlánOd { get; set; }

        [JsonProperty("poskytovatelé", NullValueHandling = NullValueHandling.Ignore)]
        public string[] Poskytovatelé { get; set; }

        [JsonProperty("realizován", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Realizován { get; set; }

        [JsonProperty("typ-kanálu", NullValueHandling = NullValueHandling.Ignore)]
        public string TypKanálu { get; set; }

        [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)]
        public string Type { get; set; }

        [JsonProperty("úroveň-důvěry", NullValueHandling = NullValueHandling.Ignore)]
        public string ÚroveňDůvěry { get; set; }
    }

    public partial class NázevÚkonu
    {
        [JsonProperty("cs", NullValueHandling = NullValueHandling.Ignore)]
        public string Cs { get; set; }
    }

    public partial class PopisÚkonu
    {
        [JsonProperty("cs", NullValueHandling = NullValueHandling.Ignore)]
        public string Cs { get; set; }
    }

    public partial class UstanoveníÚkonu
    {
        [JsonProperty("označení", NullValueHandling = NullValueHandling.Ignore)]
        public string Označení { get; set; }

        [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)]
        public string Type { get; set; }
    }
}


