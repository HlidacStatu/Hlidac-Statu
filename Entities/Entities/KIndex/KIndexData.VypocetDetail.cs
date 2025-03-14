﻿using System.Linq;
using System.Text.Json.Serialization;
using Devmasters.Enums;

namespace HlidacStatu.Entities.KIndex
{
    public partial class KIndexData
    {
        public class VypocetDetail
        {
            public class Radek
            {
                [JsonConstructor]
                public Radek()
                {
                    
                }
                
                public Radek(KIndexParts velicina, decimal hodnota, decimal koef)
                {
                    Velicina = (int)velicina;
                    VelicinaName = velicina.ToString();
                    Hodnota = hodnota;
                    Koeficient = koef;
                }

                [JsonIgnore]
                [Nest.Object(Ignore = true)]
                public string VelicinaLongName { get => ((KIndexParts)Velicina).ToNiceDisplayName(); }

                [JsonIgnore]
                [Nest.Object(Ignore = true)]
                public KIndexParts VelicinaPart { get => (KIndexParts)Velicina; }

                public int Velicina { get; set; }
                [Nest.Keyword]
                public string VelicinaName { get; set; }
                public decimal Hodnota { get; set; }
                public decimal Koeficient { get; set; }

            }
            public VypocetOboroveKoncentrace OboroveKoncentrace { get; set; }
            public Radek[] Radky { get; set; }
            public System.DateTime LastUpdated { get; set; }

            public decimal Vypocet()
            {
                decimal vysledek = 0;
                if (Radky != null)
                    vysledek = Radky
                       .Select(m => m.Hodnota * m.Koeficient)
                       .Sum();

                if (vysledek < 0)
                    vysledek = 0;

                return vysledek;
            }



            public string ToDebugString()
            {
                string l = "";
                foreach (var r in Radky.Where(m => m.VelicinaPart != KIndexParts.PercSmlouvyPod50kBonus))
                {
                    l += r.VelicinaLongName;
                    l += "\t" + r.Hodnota.ToString("N4");
                    l += "\n";
                }
                l += "bonus\t" + (Radky.FirstOrDefault(m => m.VelicinaPart == KIndexParts.PercSmlouvyPod50kBonus)?.Hodnota ?? 0m).ToString("N2");
                return l;
            }
        }
        public class VypocetOboroveKoncentrace
        {
            public class RadekObor
            {
                [Nest.Keyword]
                public string Obor { get; set; }
                public decimal Hodnota { get; set; }
                public decimal Vaha { get; set; }
                public decimal PodilSmluvBezCeny { get; set; }
                public decimal CelkovaHodnotaSmluv { get; set; }
                public decimal PocetSmluvCelkem { get; set; }

            }
            public RadekObor[] Radky { get; set; }
            public decimal PrumernaCenaSmluv { get; set; }
        }



    }
}
