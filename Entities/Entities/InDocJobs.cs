using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using Devmasters.Enums;

#nullable disable

namespace HlidacStatu.Entities
{
    public partial class InDocJobs
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public long Pk { get; set; }
        [Required]
        public long TablePk { get; set; }
        [Required]
        [StringLength(300)]
        public string JobRaw { get; set; }
        [StringLength(300)]
        public string JobGrouped { get; set; }
        public decimal? SalaryMD { get; set; }
        public decimal? SalaryMdVAT { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime? Created { get; set; }
        public string Tags { get; set; }
        
        public MeasureUnit? Unit { get; set; }
        public decimal? Price { get; set; }
        public decimal? PriceVAT { get; set; }
        public decimal? PriceVATCalculated { get; set; }
        public decimal? VAT { get; set; }
        public decimal? UnitCount { get; set; }


        [ShowNiceDisplayName()]
        public enum MeasureUnit
        {
            [NiceDisplayName("-nic-")]
            [SortValue(0)]
            None = 0,
            [NiceDisplayName("h")]
            [SortValue(2)]
            Hour = 1,
            [NiceDisplayName("den")]
            [SortValue(3)]
            Day = 2,
            [NiceDisplayName("ks")]
            [SortValue(50)]
            Kus = 3,
            [NiceDisplayName("m")]
            [SortValue(21)]
            Metr = 4,
            [NiceDisplayName("km")]
            [SortValue(22)]
            Kilometr = 5,
            [NiceDisplayName("mm")]
            [SortValue(20)]
            Milimetr = 6,
            [NiceDisplayName("m2")]
            [SortValue(30)]
            MetrCtverecny = 7,
            [NiceDisplayName("km2")]
            [SortValue(32)]
            KilometrCtverecny = 8,
            [NiceDisplayName("g")]
            [SortValue(10)]
            Gram = 9,
            [NiceDisplayName("kg")]
            [SortValue(11)]
            Kilogram = 10,
            [NiceDisplayName("t")]
            [SortValue(12)]
            Tuna = 11,
            [NiceDisplayName("ha")]
            [SortValue(31)]
            Hektar = 12,
            [NiceDisplayName("kompletní dodávka (kpl)")]
            [SortValue(51)]
            KompletniDodavka = 13,
            [NiceDisplayName("měsíc")]
            [SortValue(3)]
            Mesic = 14,
            [NiceDisplayName("m3")]
            [SortValue(42)]
            MetrKrychlovy = 15,
            [NiceDisplayName("l")]
            [SortValue(41)]
            Litr = 16,
            [NiceDisplayName("ml")]
            [SortValue(40)]
            Mililitr = 17,
            [NiceDisplayName("minuta")]
            [SortValue(1)]
            Minuta = 18,
            [NiceDisplayName("rok")]
            [SortValue(4)]
            Rok = 19,
            
        }

        public static IEnumerable<MeasureUnit> GetSortedMeasureUnits()
        {
            Type et = typeof(MeasureUnit); 
            var units = Enum.GetValues(et).Cast<MeasureUnit>();
            //order units by sort value attribute
            return units.OrderBy(u => u.GetType()
                .GetField(u.ToString())
                ?.GetCustomAttribute<SortValueAttribute>()
                ?.SortValue ?? 0);
            
        }

        public void NormalizePrices()
        {
            FixWhenPriceIsHigherThanPriceWithVat();

            CalculateFinalVatPrice();

            CalculateSalaryBackwardCompatibility();
        }

        private void CalculateSalaryBackwardCompatibility()
        {

            if(Unit != MeasureUnit.Day && Unit != MeasureUnit.Hour)
                return;

            if (UnitCount is null || UnitCount == 0)
                return;
            
            var recalculatedMdSalary = Price / UnitCount;
            var recalculatedMdSalaryVat = PriceVATCalculated;
            if (Unit == MeasureUnit.Hour)
            {
                recalculatedMdSalary *= 8;
                recalculatedMdSalaryVat *= 8;
            }
            
            if (recalculatedMdSalaryVat is null || recalculatedMdSalaryVat == 0)
            {
                recalculatedMdSalaryVat = recalculatedMdSalary * 1.21m;
            }

            SalaryMD = recalculatedMdSalary;
            SalaryMdVAT = recalculatedMdSalaryVat;

        }

        private void FixWhenPriceIsHigherThanPriceWithVat()
        {
            // swap if PriceVat < Price
            if (Price > 0 && PriceVAT > 0 && Price > PriceVAT)
            {
                (Price, PriceVAT) = (PriceVAT, Price);
            }
            // swap if salaryMdVat < salaryMd
            if (SalaryMD > 0 && SalaryMdVAT > 0 && SalaryMD > SalaryMdVAT)
            {
                (SalaryMD, SalaryMdVAT) = (SalaryMdVAT, SalaryMD);
            }
            
        }
        
        private void CalculateFinalVatPrice()
        {
            if (UnitCount is null || UnitCount == 0)
                return;
            
            var unitPrice = Price / UnitCount;
            var unitPriceVat = PriceVAT / UnitCount;
            
            // use unitPriceVAT if it is not null or zero
            PriceVATCalculated = unitPriceVat ?? unitPrice * ((100 + VAT) / 100);
            
            
        }
        

    }
}