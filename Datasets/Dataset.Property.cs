using System;
using System.Collections.Generic;

namespace HlidacStatu.Datasets
{
    public partial class DataSet
    {
        public class Property
        {
            public string Name { get; set; }
            public Type Type { get; set; }
            public string Description { get; set; }

            public string TypePlainly()
            {

                if (Type == typeof(Nullable<decimal>)
                    || Type == typeof(Nullable<long>)
                    || Type == typeof(decimal)
                    || Type == typeof(long)
                    )
                    return "číslo";

                if (Type == typeof(Nullable<DateTime>)
                    || Type == typeof(DateTime)
                    )
                    return "datum a čas";

                if (Type == typeof(Nullable<Devmasters.DT.Date>)
                    || Type == typeof(Devmasters.DT.Date)
                    )
                    return "datum";
                if (Type == typeof(string)
                    )
                    return "text";
                if (Type == typeof(Nullable<bool>)
                    || Type == typeof(bool)
                    )
                    return "boolean, logická hodnota";
                if (Type == typeof(object)
                    )
                    return "objekt";

                if (Type == typeof(object[])
                    )
                    return "pole hodnot";

                return "neznámý";
            }

            public List<(string q, string desc)> TypeSamples()
            {
                List<(string q, string desc)> samples = new List<(string q, string desc)>();

                if (Type == typeof(Nullable<decimal>)
                    || Type == typeof(Nullable<long>)
                    || Type == typeof(decimal)
                    || Type == typeof(long)
                    )
                {
                    samples.Add(("10", "přesná hodnota 10"));
                    samples.Add((">10", "větší než 10"));
                    samples.Add((">=10", "rovno nebo větší než 10"));
                    samples.Add(("[10 TO *]", "rovno nebo větší než 10"));
                    samples.Add(("[10 TO 20]", "větší nebo rovno než 10 a menší nebo rovno než 20"));
                    samples.Add(("[10 TO 20}", "rovno nebo větší než 10 a menší než 20"));
                }
                if (Type == typeof(Nullable<DateTime>)
                    || Type == typeof(DateTime)
                    )
                {                     
                    samples.Add(("2020-04-23", "hodnota rovna 23. dubna 2020"));
                    samples.Add(("rok-mesic-den", "přesný den"));
                    samples.Add(("[2020-04-23 TO *]", "od 23. dubna 2020 včetně"));
                    samples.Add(("[* TO 2020-04-23]", "do 23. dubna 2020 23:59:59"));
                    samples.Add(("[* TO 2020-04-23}", "do 23. dubna 2020 00:00, tzn. bez tohoto dne"));
                    samples.Add(("[2020-04-23 TO 2020-04-30]", "hodnota mezi 23 až 30. dubna včetně 30.dubna"));
                }
                if (Type == typeof(Nullable<Devmasters.DT.Date>)
                    || Type == typeof(Devmasters.DT.Date)
                    )
                {                     
                    samples.Add(("2020-04-23", "hodnota rovna 23. dubna 2020"));
                    samples.Add(("rok-mesic-den", "přesný den"));
                    samples.Add(("[2020-04-23 TO *]", "od 23. dubna 2020 včetně"));
                    samples.Add(("[* TO 2020-04-23]", "do 23. dubna 2020 23:59:59"));
                    samples.Add(("[* TO 2020-04-23}", "do 23. dubna 2020 00:00, tzn. bez tohoto dne"));
                    samples.Add(("[2020-04-23 TO 2020-04-30]", "hodnota mezi 23 až 30. dubna včetně 30.dubna"));
                }
                if (Type == typeof(string)
                    )
                {                     
                    samples.Add(("podpora", "parametr obsahuje slovo podpora, všechny tvary slova"));
                    samples.Add(("\"premiér Babiš\"", "obsahuje sousloví 'premiér Babiš', všechny tvary jednotlivých slov, ale pevném pořadí"));
                }
                if (Type == typeof(Nullable<bool>)
                    || Type == typeof(bool)
                    )
                {                     
                    samples.Add(("true", "logický parametr obsahuje hodnotu 'true'"));
                    samples.Add(("false", "logický parametr obsahuje hodnotu 'false'"));
                }
                if (Type == typeof(object)
                    )
                {
                    //samples.Add(("", "jde se ptát pouze na jednotlivé "));
                }

                if (Type == typeof(object[])
                    )
                {
                    //samples.Add(("", "jde se ptát pouze na jednotlivé "));
                }

                return samples;
            }

        }

    }
}
