﻿using HlidacStatu.DS.Api.MCP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HlidacStatu.DS.Api.Firmy
{
    /// <summary>
    /// Detailní informace o firmě
    /// </summary>
    public class SubjektDetailInfo : MCPBaseResponse
    {
        /// <summary>
        /// Charakter subjektu
        /// </summary>
        public enum CharakterEnum
        {
            Nezjisteno = -1,
            SoukromaFirma = 0,
            FirmaPatriStatu = 1,
            FirmaSpoluvlastniStat = 2,
            StatniOrgan = 3,
            Obec = 4,
            NeziskovaOrganizace = 5,
            ZahranicniFirma = 6,
            PolitickaStrana = 7,
        }
        /// <summary>
        /// IČO subjektu
        /// </summary>
        public string Ico { get; set; }

        /// <summary>
        /// Název subjektu
        /// </summary>
        public string Jmeno_Firmy { get; set; }

        /// <summary>
        /// Omezení činnosti. Pokud prázdné, tak normálně fungující firma.
        /// Typicka omezeni: v likvidaci,v insolvenci,v likvidaci,v nucené správě,zaniklý subjekt,pozastavená činností,nezahájená činnost
        /// </summary>

        public SubjektFinancialInfo Business_info { get; set; } = null;
        /// <summary>
        /// Charakter subjektu
        /// </summary>
        public CharakterEnum Charakter_Firmy { get; set; }

        public string Rizika { get; set; } = string.Empty;


        public string[] Kategorie_Organu_Verejne_Moci { get; set; } = null;

        /// <summary>
        /// Index klíčových rizik
        /// </summary>
        public class KIndexData
        {

            public int Rok { get; set; }

            /// <summary>
            /// Hodnota indexu, A nejlepší, F nejhorší
            /// </summary>
            public string KIndex { get; set; }

            /// <summary>
            /// Stručná charateristika indexu
            /// </summary>
            public string Popis { get; set; }
            /// <summary>
            /// URL obrázku, který se zobrazuje hodnotu indexu
            /// </summary>
            public string Obrazek_Url { get; set; }
        }

        /// <summary>
        /// KIndex pro jednotlivé roky
        /// </summary>
        public List<KIndexData> KIndex { get; set; } = new();

        /// <summary>
        /// Statistiky smluv a kontraktů subjektu se státem
        /// </summary>
        public class SmlouvyData
        {

            public int Rok { get; set; }

            public long PocetSmluv { get; set; } = 0;

            /// <summary>       
            public decimal CelkovaHodnotaSmluv { get; set; } = 0;


            /// <summary>
            /// počet smluv bez uvedené ceny
            /// </summary>
            public long PocetSmluvBezCeny { get; set; } = 0;

            /// <summary>
            /// počet smluv s cenou těsně pod limitem, kdy je nutné vypsat řádnou veřejnou zakázku
            /// </summary>
            public long PocetSmluvULimitu { get; set; } = 0;

            /// <summary>
            /// počet smluv, které mají zásadní nedostatky až porušení zákona
            /// </summary>
            public long PocetSmluvSeZasadnimNedostatkem { get; set; } = 0;

            /// <summary>
            /// Tři hlavní oblasti, ve kterých subjekt uzavírá smlouvy
            /// </summary>
            public string[] HlavniOblasti { get; set; } = null;

            //public DS.Api.StatisticChange ZmenaPoctuSmluv { get; set; } = null;
            //public DS.Api.StatisticChange ZmenaHodnotySmluv { get; set; } = null;
        }

        /// <summary>
        /// Smlouvy a kontrakty subjektu se státem od 2016 to současnosti
        /// </summary>
        public SmlouvyData Statistika_Registr_Smluv { get; set; } = new();
        /// <summary>
        /// Pokud subjekt vlastní dceřinné společnosti, tak statistiky smluv a kontraktů firmy a těchto dceřinných společností dohromady
        /// od 2016 to současnosti
        /// </summary>
        public SmlouvyData Statistika_Registr_Smluv_pro_Holding { get; set; } = new();

        /// <summary>
        /// Smlouvy a kontrakty subjektu se státem po letech
        /// </summary>
        public List<SmlouvyData> Statistiky_Registr_Smluv_po_Letech { get; set; } = new();

        /// <summary>
        /// Pokud subjekt vlastní dceřinné společnosti, tak statistiky smluv a kontraktů firmy a těchto dceřinných společností dohromady
        /// </summary>
        public List<SmlouvyData> Statisticky_Registr_Smluv_pro_Holding_po_Letech { get; set; } = new();


        /// <summary>
        /// Statistiky dotací, které subjekt obdržel
        /// </summary>
        public class DotaceData
        {
            public int Rok { get; set; }
            public int Pocet_Dotaci { get; set; } = 0;
            public decimal Celkem_Prideleno { get; set; } = 0m;

            /// <summary>
            /// Změna počtu ročních dotací oproti aktuálnímu roku
            /// </summary>
            //public DS.Api.StatisticChange ZmenaPoctuDotaci { get; set; } = null;
            /// <summary>
            /// Změna objemu ročních dotací oproti aktuálnímu roku
            /// </summary>
            //public DS.Api.StatisticChange ZmenaHodnotyDotaci { get; set; } = null;
        }
        public DotaceData Statistika_Dotace { get; set; } = new();
        public DotaceData Statistika_Dotace_pro_Holding { get; set; } = new();

        public List<DotaceData> Statistika_Dotace_po_Letech { get; set; } = new();
        public List<DotaceData> Statistika_Dotace_pro_Holding_po_Letech { get; set; } = new();
    }
}
