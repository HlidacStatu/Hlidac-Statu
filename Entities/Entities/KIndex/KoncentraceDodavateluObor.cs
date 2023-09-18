namespace HlidacStatu.Entities.Entities.KIndex
{
    public class KoncentraceDodavateluObor
    {
        public int OborId { get; set; }
        [Nest.Keyword]
        public string OborName { get; set; }


        public KoncentraceDodavateluIndexy Koncentrace { get; set; }

        public decimal SmluvBezCenyMalusKoeficient { get; set; } = 1;

        public decimal Combined_Herfindahl_Hirschman_Modified()
        {
            return Koncentrace.Herfindahl_Hirschman_Modified * SmluvBezCenyMalusKoeficient;
        }

        public decimal PodilSmluvBezCeny
        {
            get
            {
                if (Koncentrace.PocetSmluvProVypocet == 0)
                    return 0m;

                return (decimal)Koncentrace.PocetSmluvBezCenyProVypocet / (decimal)Koncentrace.PocetSmluvProVypocet;
            }
        }
    }

}
