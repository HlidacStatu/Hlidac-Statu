using HlidacStatu.DS.Api.MCP;
using System;
using static HlidacStatu.DS.Api.Firmy.SubjektDetailInfo;

namespace HlidacStatu.DS.Api.Firmy
{
    public class SubjektFinancialInfo : MCPBaseResponse
    {
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
        public string Omezeni_Cinnosti { get; set; }

        /// <summary>
        /// Charakter subjektu
        /// </summary>
        public CharakterEnum Charakter_Firmy { get; set; }

        public string[] Kategorie_Organu_Verejne_Moci = null;

        public string Pocet_Zamestnancu { get; set; }
        public string Obor_Podnikani { get; set; }
        public string Obrat { get; set; } = null;
        public string Platce_DPH { get; set; } = null;
        //public string CompanyIndexKod { get; set; } = null;
        public string Je_nespolehlivym_platcem_DPHKod { get; set; } = null;
        public string Ma_dluh_vzp { get; set; } = null;


        public string Kod_Okresu { get; set; }
        public string ICZUJ { get; set; }
        public string KODADM { get; set; }
        public string Adresa { get; set; }
        public string PSC { get; set; }
        public string Obec { get; set; }

        public Osoba[] Osoby_s_vazbou_na_firmu = null;

        public class Osoba
        {
            public string Full_Name { get; set; }
            public string Person_Id { get; set; }
            public string Year_Of_Birth { get; set; }
        }
    }
}
