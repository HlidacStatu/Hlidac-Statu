using System;
using Devmasters.Enums;
using Nest;

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
        }
        
        #endregion
    }
}