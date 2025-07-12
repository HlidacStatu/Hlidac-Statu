using System;
using System.ComponentModel;

namespace HlidacStatu.Entities
{

    public partial class HintSmlouva
    {

        [Devmasters.Enums.ShowNiceDisplayName]
        [Description("Smlouvy s cenou těsně pod zákonným limitem")]
        public enum ULimituTyp
        {
            [Devmasters.Enums.NiceDisplayName("")]
            OK = 0,
            [Devmasters.Enums.NiceDisplayName("U limitu veřejné zakázky malého rozsahu na dodávky a služby ")]            
            LimitMalehoRozsahuDodavkySluzby = 1, //do 3.4.2025 2.000.000, pote 3M bez DPH

            [Devmasters.Enums.NiceDisplayName("U limitu veřejné zakázky malého rozsahu na stavební práce ")]
            LimitMalehoRozsahuStavebniPrace = 2,//do 3.4.2025 2.000.000, pote 3M bez DPH

            [Devmasters.Enums.NiceDisplayName("U limitu podlimitní veřejné zakázky na dodávky a služby na dodávky a služby zadávané ústředními orgány státní správy")]
            LimitPodlimitniDodavkySluzbyUstredniOrgany = 3,// 2 000 000 do 3 494 000

            [Devmasters.Enums.NiceDisplayName("U limitu podlimitní veřejné zakázky na dodávky a služby na dodávky a služby zadávané ostatními zadavateli")]
            LimitPodlimitniDodavkySluzby = 4,// 2 000 000 do 5 401 000

            [Devmasters.Enums.NiceDisplayName("U limitu podlimitní veřejné zakázky na dodávky a služby na stavební práce ")]
            LimitPodlimitniStavebniPrace = 5,// 6 000 000 do 135 348 000
        }

        [Devmasters.Enums.ShowNiceDisplayName]
        public enum PolitickaAngazovanostTyp
        {
            [Devmasters.Enums.NiceDisplayName("Nesponzoruje politické strany")]
            Neni = 0,
            [Devmasters.Enums.NiceDisplayName("Subjekt přímo sponzoroval politické strany")]
            PrimoSubjekt = 1,
            [Devmasters.Enums.NiceDisplayName("Osoba propojená se subjektem sponzorovala politické strany")]
            AngazovanyMajitel = 2
        }
        public enum VztahSeSoukromymSubjektemTyp
        {
            PouzeSoukrSoukr = -1,
            Neznamy = -2,
            PouzeStatStat = 0,
            PouzeStatSoukr = 1,
            Kombinovane = 2
        }

        [Nest.Date]
        public DateTime? Updated { get; set; }

        [Nest.Number]
        public int SmlouvaULimitu { get; set; } = 0;

        [Nest.Number]
        public int DenUzavreni { get; set; } = 0;

        [Nest.Number]
        public int SmlouvaSPolitickyAngazovanymSubjektem { get; set; } = 0;

        [Nest.Number]
        public int PocetDniOdZalozeniFirmy { get; set; } = 999999;

        [Nest.Number]
        public int VztahSeSoukromymSubjektem { get; set; } = -2;

        [Nest.Number]
        public int SkrytaCena { get; set; } = 0;

    }
}
