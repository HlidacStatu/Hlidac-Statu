using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HlidacStatu.Repositories.ES;

namespace HlidacStatu.Repositories;

public static class RejstrikTrestuRepo
{
    private static string _indexName = "rejstrik-trestu-pravnickych-osob";
    private static Manager.IndexType _indexType = Manager.IndexType.DataSource;

    public static async Task<List<RejstrikTrestu>> FindTrestyAsync(string ico)
    {
        var client = await Manager.GetESClientAsync(_indexName, idxType: _indexType);

        string query = @$"{{
            ""bool"": {{
              ""must"": [
                {{
                  ""match"": {{
                    ""ICO"": ""{ico}""
                  }}
                }}
              ]
            }}
          }}";

        try
        {
            var result = await client.SearchAsync<RejstrikTrestu>(s => s
                .Query(q => q.Raw(query))
            );


            return result.Documents.ToList();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        return null;
    }
}

public class RejstrikTrestu
{
    public string Id { get; }
    public string ICO { get; set; }
    public string NazevFirmy { get; set; }
    public string Sidlo { get; set; }
    public string RUIANCode { get; set; }
    public DateTime DatumPravniMoci { get; set; }

    public odsouzeni Odsouzeni { get; set; }

    public class odsouzeni
    {
        public Soud PrvniInstance { get; set; }
        public Soud OdvolaciSoud { get; set; }
    }

    public Paragraf[] Paragrafy { get; set; }

    public Trest[] Tresty { get; set; }

    public class Trest
    {
        public string Druh { get; set; }
        public string DruhText { get; set; }

        public Severity Riziko() => Druh.ToLower() switch
        {
            "zpo" => Severity.Fatal,
            "zc" or "zpvzuvs" or "zpvz" or "zpds" => Severity.Critical,
            "pt" or "zv" or "pv" or "pvjmh" or "pm" or "pnh" => Severity.Normal,
            _ => Severity.Others
        };

        [Devmasters.Enums.ShowNiceDisplayName]
        public enum Severity
        {
            [Devmasters.Enums.NiceDisplayName("Fatální")]
            Fatal,

            [Devmasters.Enums.NiceDisplayName("Vysoké riziko")]
            Critical,

            [Devmasters.Enums.NiceDisplayName("Nízké riziko")]
            Normal,

            [Devmasters.Enums.NiceDisplayName("Ostatní")]
            Others,
        }
    }

    public class Soud
    {
        public string DruhRozhodnuti { get; set; }
        public DateTime DatumRozhodnuti { get; set; }
        public string Jmeno { get; set; }
        public string SpisovaZnacka { get; set; }
    }

    public class Paragraf
    {
        public zakon Zakon { get; set; }

        public class zakon
        {
            public int Rok { get; set; }
            public string ZakonCislo { get; set; }
            public string ParagrafCislo { get; set; }
            public string OdstavecPismeno { get; set; }
        }

        public string ZakonPopis { get; set; }
        public string Zavineni { get; set; }
        public string Doplneni { get; set; }
    }


    public string TextOdsouzeni { get; set; }
}