using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

        public enum MeasureUnit
        {
            None = 0,
            ManHour = 1,
            ManDay = 2,
            Kus = 3,
            Metr = 4,
            Kilometr = 5,
            Milimetr = 6,
            MetrCtverecny = 7,
            KilometrCtverecny = 8,
            Gram = 9,
            Kilogram = 10,
            Tuna = 11,
            Hektar = 12,
            KompletniDodavka = 13,
            Mesic = 14,
            MetrKrychlovy = 15,
            
        }

        public void NormalizePrices()
        {
            FixMixedupVat();
            
            var unitPrice = Price / UnitCount;
            var unitPriceVAT = PriceVAT / UnitCount;
            
            switch (Unit)
            {
                case MeasureUnit.ManHour:
                    SalaryMD = unitPrice * 8;
                    SalaryMdVAT = unitPriceVAT * 8;
                    break;
                        
                case MeasureUnit.ManDay:
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