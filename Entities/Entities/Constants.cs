using System.Linq;

namespace HlidacStatu.Entities.Entities;

public static class Constants
{
    public static class Ica
    {
        public const string Senat = "63839407";
        public const string KancelarPoslaneckeSnemovny = "00006572";
        public const string UradVlady = "00006599";
        
        public static readonly string[] Kraje =
        [
            "70890650", "70888337", "70891168", "70890749", "70890366", "70889546", "70891508", "70890692", "60609460", 
            "70892822", "70891095", "70892156", "70891320", "00064581"
        ];
        
        public static readonly string[] Ministerstva =
        [
            "00006947", "00007064", "00020478", "00022985", "00023671", "00024341", "00025429", "00164801", "00551023", 
            "00639303", "45769851", "47609109", "60162694", "60433558", "66002222", "66003008", "86594346"
        ];

        public static readonly string[] Vlada = Ministerstva.Concat([UradVlady]).ToArray();
        
        
        
    }
    
    
    
    
}