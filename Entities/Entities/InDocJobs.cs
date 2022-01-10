using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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
            None = 0,
            [NiceDisplayName("h")]
            Hour = 1,
            [NiceDisplayName("den")]
            Day = 2,
            [NiceDisplayName("ks")]
            Kus = 3,
            [NiceDisplayName("m")]
            Metr = 4,
            [NiceDisplayName("km")]
            Kilometr = 5,
            [NiceDisplayName("mm")]
            Milimetr = 6,
            [NiceDisplayName("m2")]
            MetrCtverecny = 7,
            [NiceDisplayName("km2")]
            KilometrCtverecny = 8,
            [NiceDisplayName("g")]
            Gram = 9,
            [NiceDisplayName("kg")]
            Kilogram = 10,
            [NiceDisplayName("t")]
            Tuna = 11,
            [NiceDisplayName("ha")]
            Hektar = 12,
            [NiceDisplayName("kompletní dodávka (kpl)")]
            KompletniDodavka = 13,
            [NiceDisplayName("měsíc")]
            Mesic = 14,
            [NiceDisplayName("m3")]
            MetrKrychlovy = 15,
            [NiceDisplayName("l")]
            Litr = 16,
            [NiceDisplayName("ml")]
            Mililitr = 17,
            
            
        }

        public void NormalizePrices()
        {
            FixMixedupVat();
            
            var unitPrice = Price / UnitCount;
            var unitPriceVAT = PriceVAT / UnitCount;
            
            switch (Unit)
            {
                case MeasureUnit.Hour:
                    SalaryMD = unitPrice * 8;
                    SalaryMdVAT = unitPriceVAT * 8;
                    break;
                        
                case MeasureUnit.Day:
                    SalaryMD = unitPrice;
                    SalaryMdVAT = unitPriceVAT;
                    break;
            }

            ComputeFinalVatPrice(unitPriceVAT);

        }

        private void FixMixedupVat()
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
        
        private void ComputeFinalVatPrice(decimal? unitPriceVAT)
        {
            decimal vatMultiplier = 1.21m;
            
            if (SalaryMdVAT > 0)
            {
                PriceVATCalculated = SalaryMdVAT;
            } 
            else if (PriceVAT > 0)
            {
                PriceVATCalculated = unitPriceVAT;
            }
            else if (SalaryMD > 0)
            {
                PriceVATCalculated = SalaryMD * vatMultiplier;
            }
        }

    }
}