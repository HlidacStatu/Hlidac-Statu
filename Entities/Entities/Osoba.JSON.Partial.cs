using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace HlidacStatu.Entities
{
    public partial class Osoba
    {

        

        public class JSON
        {

            [JsonProperty("id", Required = Required.Default)]
            public int Id
            {
                get; set;
            }
            [JsonProperty("nameId", Required = Required.Default)]
            public string NameId
            {
                get; set;
            }

            [JsonProperty("jmeno", Required = Required.Default)]
            public string Jmeno
            {
                get; set;
            }

            [JsonProperty("prijmeni", Required = Required.Default)]
            public string Prijmeni
            {
                get; set;
            }

            /// <summary>ve formátu 1982-03-23 (rok-mesic-den)</summary>
            [JsonProperty("narozeni", Required = Required.Default)]
            public string Narozeni
            {
                get; set;
            }

            [JsonProperty("umrti", Required = Required.Default)]
            public string Umrti
            {
                get; set;
            }

            [JsonProperty("gender", Required = Required.Default)]
            [JsonConverter(typeof(StringEnumConverter))]
            public gender Gender
            {
                get; set;
            }

            [JsonProperty("status", Required = Required.Default)]
            [JsonConverter(typeof(StringEnumConverter))]
            public StatusOsobyEnum Status
            {
                get; set;
            }

            /// <summary>ve formátu 1982-03-23 (rok-mesic-den)</summary>
            [JsonProperty("autor", Required = Required.Default)]
            public string Autor
            {
                get; set;
            }

            [JsonProperty("event", Required = Required.Default)]
            public ev[] Event
            {
                get; set;
            }

            [JsonProperty("sponzoring", Required = Required.Default)]
            public sponzoring[] Sponzoring
            {
                get; set;
            }

            [JsonProperty("vazbyfirmy", Required = Required.Default)]
            public vazba[] Vazbyfirmy
            {
                get; set;
            }

            public enum gender
            {
                Muž = 0,
                Žena = 1,
            }

            public enum typVazby
            {
                OsobniVztah = -3,
                Vliv = -2,
                Kontrola = -1,
            }

            public partial class ev
            {

                [JsonProperty("pk", Required = Required.Default)]
                public int pk { get; set; } = 0;

                [JsonProperty("title", Required = Required.Default)]
                public string Title
                {
                    get; set;
                }
                [JsonProperty("note", Required = Required.Default)]
                public string Note
                {
                    get; set;
                }


                /// <summary>Pokud politik, tak za jakou stranu</summary>
                [JsonProperty("organizace", Required = Required.Default)]
                public string Organizace
                {
                    get; set;
                }
                /// <summary>Pokud politik, tak za jakou stranu</summary>
                [JsonProperty("addInfoNum", Required = Required.Default)]
                public decimal? AddInfoNum
                {
                    get; set;
                }


                [JsonProperty("typ", Required = Required.Default)]
                [JsonConverter(typeof(StringEnumConverter))]
                public OsobaEvent.Types Typ
                {
                    get; set;
                }

                [JsonProperty("addinfo", Required = Required.Default)]
                public string AddInfo
                {
                    get; set;
                }

                /// <summary>Ve formatu yyyy-MM-dd (1983-03-26)</summary>
                [JsonProperty("datumOd", Required = Required.Default)]
                public string DatumOd
                {
                    get; set;
                }

                /// <summary>Ve formatu yyyy-MM-dd (1983-03-26)</summary>
                [JsonProperty("datumDo", Required = Required.Default)]
                public string DatumDo
                {
                    get; set;
                }
                [JsonProperty("zdroj", Required = Required.Default)]
                public string Zdroj
                {
                    get; set;
                }

            }

            public class sponzoring
            {
                [JsonProperty("id", Required = Required.Default)]
                public int Id { get; set; } = 0;
                
                [JsonProperty("hodnota", Required = Required.Default)]
                public decimal? Hodnota { get; set; }

                [JsonProperty("typ", Required = Required.Default)]
                [JsonConverter(typeof(StringEnumConverter))]
                public Sponzoring.TypDaru Typ { get; set; }

                [JsonProperty("icoPrijemce", Required = Required.Default)]
                public string IcoPrijemce { get; set; }

                [JsonProperty("popis", Required = Required.Default)]
                public string Popis { get; set; }

                /// <summary>Ve formatu yyyy-MM-dd (1983-03-26)</summary>
                [JsonProperty("darovanoDne", Required = Required.Default)]
                public string DarovanoDne { get; set; }

                [JsonProperty("zdroj", Required = Required.Default)]
                public string Zdroj { get; set; }

            }

            public class vazba
            {


                [JsonProperty("vazbaKIco", Required = Required.Default)]
                public string VazbaKIco
                {
                    get; set;
                }
                [JsonProperty("vazbaKOsoba", Required = Required.Default)]
                public string VazbaKOsoba
                {
                    get; set;
                }

                /// <summary>Formalni vztahy automaticky nacteme z Obchodniho rejstriku</summary>
                [JsonProperty("typVazby", Required = Required.Default)]
                [JsonConverter(typeof(StringEnumConverter))]
                public typVazby TypVazby
                {
                    get; set;
                }

                [JsonProperty("popis", Required = Required.Default)]
                public string Popis
                {
                    get; set;
                }

                /// <summary>Ve formatu yyyy-MM-dd (1983-03-26)</summary>
                [JsonProperty("datumOd", Required = Required.Default)]
                public string DatumOd
                {
                    get; set;
                }

                /// <summary>Ve formatu yyyy-MM-dd (1983-03-26)</summary>
                [JsonProperty("datumDo", Required = Required.Default)]
                public string DatumDo
                {
                    get; set;
                }

                /// <summary>Zdroj (URL], ktery tuto vazbu popisuje/dokazuje</summary>
                [JsonProperty("zdroj", Required = Required.Default)]
                public string Zdroj
                {
                    get; set;
                }

            }
        }




    }

}

