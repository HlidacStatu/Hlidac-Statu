using System;

namespace HlidacStatu.Entities.Entities;

public class RejstrikTrestu
{
    [Nest.PropertyName("Id")]
    public string Id { get; set; }

    [Nest.PropertyName("ICO")]
    public string ICO { get; set; }
    [Nest.PropertyName("NazevFirmy")]
    public string NazevFirmy { get; set; }

    [Nest.PropertyName("Sidlo")]
    public string Sidlo { get; set; }

    [Nest.PropertyName("RUIANCode")]
    public string RUIANCode { get; set; }

    [Nest.PropertyName("DatumPravniMoci")]
    public DateTime DatumPravniMoci { get; set; }

    [Nest.PropertyName("Odsouzeni")]
    public odsouzeni Odsouzeni { get; set; }

    public class odsouzeni
    {

        [Nest.PropertyName("PrvniInstance")]
        public Soud PrvniInstance { get; set; }

        [Nest.PropertyName("OdvolaciSoud")]
        public Soud OdvolaciSoud { get; set; }
    }


    [Nest.PropertyName("Paragrafy")]
    public Paragraf[] Paragrafy { get; set; }


    [Nest.PropertyName("Tresty")]
    public Trest[] Tresty { get; set; }

    public class Trest
    {

        [Nest.PropertyName("Druh")]
        public string Druh { get; set; }

        [Nest.PropertyName("DruhText")]
        public string DruhText { get; set; }

        [Nest.Ignore]
        public Severity Riziko => Druh.ToLower() switch
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
            Fatal = 3,

            [Devmasters.Enums.NiceDisplayName("Vysoké riziko")]
            Critical = 2,

            [Devmasters.Enums.NiceDisplayName("Nízké riziko")]
            Normal = 1,

            [Devmasters.Enums.NiceDisplayName("Ostatní")]
            Others = 0,
        }
    }

    public class Soud
    {

        [Nest.PropertyName("DruhRozhodnuti")]
        public string DruhRozhodnuti { get; set; }

        [Nest.PropertyName("DatumRozhodnuti")]
        public DateTime DatumRozhodnuti { get; set; }

        [Nest.PropertyName("Jmeno")]
        public string Jmeno { get; set; }

        [Nest.PropertyName("SpisovaZnacka")]
        public string SpisovaZnacka { get; set; }
    }

    public class Paragraf
    {

        [Nest.PropertyName("Zakon")]
        public zakon Zakon { get; set; }

        public class zakon
        {

            [Nest.PropertyName("Rok")]
            public int Rok { get; set; }

            [Nest.PropertyName("ZakonCislo")]
            public string ZakonCislo { get; set; }   

            [Nest.PropertyName("ParagrafCislo")]
            public string ParagrafCislo { get; set; } 

            [Nest.PropertyName("OdstavecPismeno")]
            public string OdstavecPismeno { get; set; }
        }


        [Nest.PropertyName("ZakonPopis")]
        public string ZakonPopis { get; set; }

        [Nest.PropertyName("Zavineni")]
        public string Zavineni { get; set; }

        [Nest.PropertyName("Doplneni")]
        public string Doplneni { get; set; }
    }



    [Nest.PropertyName("TextOdsouzeni")]
    public string TextOdsouzeni { get; set; }
}

