using System;
using static HlidacStatu.DS.Api.Firmy.SubjektDetailInfo;

namespace HlidacStatu.DS.Api.Firmy
{
    public class SubjektFinancialInfo
    {
        /// <summary>
        /// IČO subjektu
        /// </summary>
        public string Ico { get; set; }

        /// <summary>
        /// Název subjektu
        /// </summary>
        public string JmenoFirmy { get; set; }

        /// <summary>
        /// Omezení činnosti. Pokud prázdné, tak normálně fungující firma.
        /// Typicka omezeni: v likvidaci,v insolvenci,v likvidaci,v nucené správě,zaniklý subjekt,pozastavená činností,nezahájená činnost
        /// </summary>
        public string OmezeniCinnosti { get; set; }

        /// <summary>
        /// Charakter subjektu
        /// </summary>
        public CharakterEnum CharakterFirmy { get; set; }

        public string[] Kategorie_Organu_Verejne_Moci = null;

        public string PocetZam { get; set; }
        public string Industry { get; set; }
        public string Obrat { get; set; } = null;
        public string PlatceDPH { get; set; } = null;
        //public string CompanyIndexKod { get; set; } = null;
        public string Je_nespolehlivym_platcem_DPHKod { get; set; } = null;
        public string Ma_dluh_vzp { get; set; } = null;


        public string KodOkresu { get; set; }
        public string ICZUJ { get; set; }
        public string KODADM { get; set; }
        public string Adresa { get; set; }
        public string PSC { get; set; }
        public string Obec { get; set; }


        /// <summary>
        /// Odkaz na zdrojová data subjektu. Nutno uvést při použití dat.
        /// </summary>
        public string ZdrojUrl { get; set; } = "https://www.hlidacstatu.cz";
        public string Copyright { get; set; } = $"(c) {DateTime.Now.Year} Hlídač Státu z.ú.";

    }
}
