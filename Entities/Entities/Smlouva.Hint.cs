using System;

namespace HlidacStatu.Entities
{

    public partial class HintSmlouva
    {
        public enum ULimituTyp
        {
            OK = 0,
            Limit2M = 1,
            Limit6M = 2
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
