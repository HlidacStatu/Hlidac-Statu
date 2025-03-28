﻿using HlidacStatu.Entities.KIndex;

namespace HlidacStatuApi.Models
{
    public class KIndexDTO
    {
        public string Ico { get; set; }
        public string Name { get; set; }
        public DateTime LastChange { get; set; }
        public IEnumerable<KIndexYearsDTO> AnnualCalculations { get; set; }
    }

    public class KIndexYearsDTO
    {
        public decimal KIndex { get; set; }
        public KIndexData.VypocetDetail Calculation { get; set; }

    }
}
