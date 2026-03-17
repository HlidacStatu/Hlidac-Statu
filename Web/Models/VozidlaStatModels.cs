using System;
using System.Collections.Generic;

namespace HlidacStatu.Web.Models
{
    /// <summary>
    /// Single vehicle item used across multiple reports.
    /// </summary>
    public class VehicleStatItem
    {
        public string PCV { get; set; }
        public string Brand { get; set; }
        public string Model { get; set; }
        public string Type { get; set; }
        public decimal? PowerKw { get; set; }
        public decimal? TopSpeedKmh { get; set; }
        public string Fuel { get; set; }
        public DateTime? RegistrationDate { get; set; }
        public string Color { get; set; }
        public string LogoSlug { get; set; }
        public string VIN { get; set; }
        public int? YearOfManufacture { get; set; }
        public string EngineDisplacement { get; set; }
        public DateTime? StkExpiryDate { get; set; }
    }

    /// <summary>
    /// Report with year selection and a list of vehicles.
    /// Used by: NejsilnejsiAuta, NejrychlejsiAuta, LuxusniAuta.
    /// </summary>
    public class VehicleYearReport
    {
        public int SelectedYear { get; set; }
        public List<int> AvailableYears { get; set; } = new();
        public List<VehicleStatItem> Items { get; set; } = new();
    }

    /// <summary>
    /// Brand with registration count (for popularity report).
    /// </summary>
    public class BrandStatItem
    {
        public string Brand { get; set; }
        public string LogoSlug { get; set; }
        public int Count { get; set; }
    }

    public class BrandYearReport
    {
        public int SelectedYear { get; set; }
        public List<int> AvailableYears { get; set; } = new();
        public List<BrandStatItem> Items { get; set; } = new();
    }

    /// <summary>
    /// Electric/hybrid vehicle trend per year.
    /// </summary>
    public class EvTrendItem
    {
        public int Year { get; set; }
        public int ElectricCount { get; set; }
        public int HybridCount { get; set; }
        public int TotalCount { get; set; }
    }

    /// <summary>
    /// Import statistics per country.
    /// </summary>
    public class ImportStatItem
    {
        public string Country { get; set; }
        public int Count { get; set; }
    }

    public class ImportYearReport
    {
        public int SelectedYear { get; set; }
        public List<int> AvailableYears { get; set; } = new();
        public List<ImportStatItem> Items { get; set; } = new();
    }

    /// <summary>
    /// Average vehicle age per brand.
    /// </summary>
    public class AgeStatItem
    {
        public string Brand { get; set; }
        public string LogoSlug { get; set; }
        public double AverageAge { get; set; }
        public int Count { get; set; }
    }

    /// <summary>
    /// Fuel type distribution.
    /// </summary>
    public class FuelStatItem
    {
        public string FuelType { get; set; }
        public int Count { get; set; }
    }

    public class FuelYearReport
    {
        public int SelectedYear { get; set; }
        public List<int> AvailableYears { get; set; } = new();
        public List<FuelStatItem> Items { get; set; } = new();
        public int TotalCount { get; set; }
    }
}
