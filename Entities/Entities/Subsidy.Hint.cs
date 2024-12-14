using System;
using Nest;

namespace HlidacStatu.Entities;

public partial class Subsidy
{
    public class Hint
    {
        /// <summary>
        /// Info zda-li se jedná o pravděpodobnou duplicitní dotace
        /// </summary>
        [Keyword]
        public bool IsDuplicate { get; set; } = false;
        
        /// <summary>
        /// Pokud jde o duplicitní dotaci, tak odkaz na originál zde
        /// </summary>
        [Keyword]
        public string OriginalSubsidyId { get; set; }
        
        /// <summary>
        /// Legislativa - info, jestli jde o státní/evropskou dotaci, krajskou, obecní a nebo investiční pobídka
        /// </summary>
        [Keyword]
        public Type SubsidyType { get; set; }
        
        public enum Type
        {
            Unknown,
            Evropska,
            Krajska,
            Obecni,
            InvesticniPobidka,
        }
        
        [Date]
        public DateTime? DuplicateCalculated { get; set; }
        
    }
}