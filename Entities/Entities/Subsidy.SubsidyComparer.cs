using System;
using System.Collections.Generic;

namespace HlidacStatu.Entities;

public partial class Subsidy
{
    public class SubsidyComparer : IEqualityComparer<Subsidy>
    {
        public bool Equals(Subsidy x, Subsidy y)
        {
            if (x is null && y is null)
                return true;
            if (x is null || y is null)
                return false;
            return string.Equals(x.Id, y.Id, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(Subsidy obj)
        {
            return obj.Id?.GetHashCode() ?? 0;
        }
    }

}
