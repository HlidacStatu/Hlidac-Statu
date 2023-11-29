using Nest;

using System;
using System.Collections.Generic;

namespace HlidacStatu.Entities.Insolvence
{
    public partial class Rizeni
        : IBookmarkable
    {

        public static DateTime MinSqlDate = new DateTime(1753, 1, 1); // 01/01/1753 
        public Rizeni()
        {
            Dokumenty = new List<Dokument>();
            Dluznici = new List<Osoba>();
            Veritele = new List<Osoba>();
            Spravci = new List<Osoba>();
        }

        [Object(Ignore = true)]
        public bool IsFullRecord { get; set; } = false;

        [Keyword]
        public string SpisovaZnacka { get; set; }
        [Keyword]
        public string Stav { get; set; }
        [Date]
        public DateTime? Vyskrtnuto { get; set; }
        [Keyword]
        public string Url { get; set; }
        [Date]
        public DateTime? DatumZalozeni { get; set; }
        [Date]
        public DateTime PosledniZmena { get; set; }
        [Keyword]
        public string Soud { get; set; }

        [Object]
        public List<Dokument> Dokumenty { get; set; }

        [Object]
        public List<Osoba> Dluznici { get; set; }
        [Object]
        public List<Osoba> Veritele { get; set; }
        [Object]
        public List<Osoba> Spravci { get; set; }

        [Boolean]
        public bool OnRadar { get; set; } = false;

        bool _odstraneny = false;
        [Boolean]
        public bool Odstraneny
        {
            get
            {
                return _odstraneny;
            }
            set
            {
                _odstraneny = value;
                if (_odstraneny == true)
                    OnRadar = false;
            }
        }


    }
}
