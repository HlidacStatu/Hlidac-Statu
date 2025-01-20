using System;
using Devmasters.Enums;
using Nest;
using static HlidacStatu.Entities.Firma.Statistics;

namespace HlidacStatu.Entities;

public partial class Subsidy
{
    public partial class Hint
    {
        public class Category
        {
            [Number]
            public int TypeValue { get; set; }

            [Number]
            public decimal Probability { get; set; }

            [Date]
            public DateTime Created { get; set; }


            public CalculatedCategories CalculatedCategory()
            {
                return (CalculatedCategories)TypeValue;
            }

            public string CalculatedCategoryDescription()
            {
                return CalculatedCategory().ToNiceDisplayName();
            }
            public string GetSearchUrl(bool local) 
            {
                string url = $"/Dotace/hledat?q=oblast:{this.TypeValue}";

                if (local == false)
                    return "https://www.hlidacstatu.cz" + url;
                else
                    return url;
            }
        }
        
    }
}