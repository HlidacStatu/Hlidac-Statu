﻿namespace HlidacStatu.Lib.Analytics
{

    public class SimpleStat : CoreStat, IAddable<SimpleStat>
    {
        public readonly static SimpleStat Zero = new SimpleStat();

        public long Pocet { get; set; } = 0;
        public decimal CelkemCena { get; set; } = 0;

        public void Add(long pocet, decimal cena)
        {
            Pocet = Pocet + pocet;
            CelkemCena = CelkemCena + cena;
        }
        public void Subtract(long pocet, decimal cena)
        {
            Pocet = Pocet - pocet;
            CelkemCena = CelkemCena - cena;
        }

        public SimpleStat Add(SimpleStat other)
        {
            return new SimpleStat()
            {
                CelkemCena = CelkemCena + other?.CelkemCena ?? 0,
                Pocet = Pocet + other?.Pocet ?? 0
            };
        }
        public SimpleStat Subtract(SimpleStat other)
        {
            return new SimpleStat()
            {
                CelkemCena = CelkemCena - other?.CelkemCena ?? 0,
                Pocet = Pocet - other?.Pocet ?? 0
            };
        }

        public override int NewSeasonStartMonth()
        {
            return 1;
        }

        public override int UsualFirstYear()
        {
            return 1990;
        }
    }
}
