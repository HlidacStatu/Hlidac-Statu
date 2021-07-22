using System;
using System.Collections.Generic;

namespace HlidacStatu.Plugin.TransparetniUcty
{
    public interface IParser
    {
        string Name { get; }
        IEnumerable<IBankovniPolozka> GetPolozky(DateTime? fromDate, DateTime? toDate);
    }
}