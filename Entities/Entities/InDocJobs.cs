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
    public class InDocJobs
    {
        private decimal? _vat;
        private decimal? _price;
        private decimal? _priceVat;
        private decimal? _priceVatCalculated;

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public long Pk { get; set; }

        [Required] public long TablePk { get; set; }
        [Required] [StringLength(300)] public string JobRaw { get; set; }
        [StringLength(300)] public string JobGrouped { get; set; }
        [StringLength(300)] public string JobGrouped2 { get; set; }
        [StringLength(300)] public string JobGrouped3 { get; set; }

        [Column(TypeName = "datetime")] public DateTime? Created { get; set; }
        public string Tags { get; set; }

        public MeasureUnit? Unit { get; set; }

        public decimal? Price
        {
            get => RoundToWholeNumber(_price); //added to prevent user of jobTableEditor from making mistakes
            set => _price = value;
        }

        public decimal? PriceVAT
        {
            get => RoundToWholeNumber(_priceVat); //added to prevent user of jobTableEditor from making mistakes
            set => _priceVat = value;
        }

        /// <summary>
        /// Price per given unit
        /// </summary>
        public decimal? PriceVATCalculated
        {
            get => RoundToWholeNumber(
                _priceVatCalculated); //added to prevent user of jobTableEditor from making mistakes
            set => _priceVatCalculated = value;
        }


        public decimal? VAT
        {
            get => _vat;
            set
            {
                _vat = value;
                if (_vat.HasValue)
                {
                    if (!PriceVAT.HasValue || PriceVAT.Value == 0) //if price is not set, then we can calculate it
                    {
                        PriceVAT = Price * (100 + _vat.Value) / 100;
                    }
                }
            }
        }

        public decimal? UnitCount { get; set; }


        [ShowNiceDisplayName()]
        public enum MeasureUnit
        {
            [NiceDisplayName("-nic-")] [SortValue(0)] None = 0,
            
            [NiceDisplayName("minuta")] [SortValue(1)] Minuta = 18,
            [NiceDisplayName("hodina")] [SortValue(2)] Hour = 1,
            [NiceDisplayName("den")] [SortValue(3)] Day = 2,
            [NiceDisplayName("měsíc")] [SortValue(4)] Mesic = 14,
            [NiceDisplayName("rok")] [SortValue(5)] Rok = 19,

            [NiceDisplayName("g")] [SortValue(10)] Gram = 9,
            [NiceDisplayName("kg")] [SortValue(11)] Kilogram = 10,
            [NiceDisplayName("t")] [SortValue(12)] Tuna = 11,
            
            [NiceDisplayName("mm")] [SortValue(20)] Milimetr = 6,
            [NiceDisplayName("m")] [SortValue(21)] Metr = 4,
            [NiceDisplayName("km")] [SortValue(22)] Kilometr = 5,

            [NiceDisplayName("m2")] [SortValue(30)] MetrCtverecny = 7,
            [NiceDisplayName("ha")] [SortValue(31)] Hektar = 12,
            [NiceDisplayName("km2")] [SortValue(32)] KilometrCtverecny = 8,

            [NiceDisplayName("ml")] [SortValue(40)] Mililitr = 17,
            [NiceDisplayName("l")] [SortValue(41)] Litr = 16,
            [NiceDisplayName("m3")] [SortValue(42)] MetrKrychlovy = 15,

            [NiceDisplayName("ks")] [SortValue(50)] Kus = 3,
            [NiceDisplayName("kompletní dodávka (kpl)")] [SortValue(51)] KompletniDodavka = 13,
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
        }

        private void FixWhenPriceIsHigherThanPriceWithVat()
        {
            // swap if PriceVat < Price
            if (Price > 0 && PriceVAT > 0 && Price > PriceVAT)
            {
                (Price, PriceVAT) = (PriceVAT, Price);
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

        private decimal? RoundToWholeNumber(decimal? value)
        {
            if (value is null)
                return null;

            return Math.Round(value.Value, 0, MidpointRounding.ToZero);
        }

        public bool IsValid(out Errors errors)
        {
            errors = new Errors();
            bool taxAndPriceAreKnown = VAT > 0 && Price > 0;
            bool priceWithTaxIsKnown = PriceVAT > 0;
            
            if (Unit.HasValue)
            {
                if (taxAndPriceAreKnown || priceWithTaxIsKnown)
                {
                    decimal minimalAllowedHourPriceVat = 400 * 1.21m;
                    decimal maximalAllowedHourPriceVat = 2000 * 1.21m;

                    CalculateFinalVatPrice(); // je potřeba si nejprve spočíst jednotkovou cenu

                    bool isHourPriceTooLow =
                        Unit == MeasureUnit.Hour && PriceVATCalculated < minimalAllowedHourPriceVat;
                    bool isMandayPriceTooLow =
                        Unit == MeasureUnit.Day && PriceVATCalculated < minimalAllowedHourPriceVat * 8;

                    if (isHourPriceTooLow || isMandayPriceTooLow)
                    {
                        errors.Add("Cena se nám zdá nízká.", Errors.MessageSeverity.Warning);
                    }
                    
                    bool isHourPriceTooHigh = 
                        Unit == MeasureUnit.Hour && PriceVATCalculated > maximalAllowedHourPriceVat;
                    bool isMandayPriceTooHigh = 
                        Unit == MeasureUnit.Day && PriceVATCalculated > maximalAllowedHourPriceVat * 8;
                    if (isHourPriceTooHigh || isMandayPriceTooHigh)
                    {
                        errors.Add("Cena se nám zdá vyoká.", Errors.MessageSeverity.Warning);
                    }
                }
            }
            else
            {
                errors.Add("Jednotka není vybrána.", Errors.MessageSeverity.Error);
            }

            if (string.IsNullOrWhiteSpace(JobRaw))
                errors.Add("Chybí název jobu.", Errors.MessageSeverity.Error);


            if (Price > PriceVAT)
                errors.Add("Cena s DPH musí být větší než cena bez DPH.", Errors.MessageSeverity.Error);
            if (Price == PriceVAT && Price < 10)
                errors.Add("Cena s DPH je stejná jako cena bez DPH.", Errors.MessageSeverity.Warning);

            if (!taxAndPriceAreKnown && !priceWithTaxIsKnown)
                errors.Add("Není vyplněno DPH a cena, nebo chybí cena s DPH.", Errors.MessageSeverity.Error);
            
            if ((Price ?? 0) == 0 && (PriceVAT ?? 0) == 0)
            {
                errors.Add("Chybí vyplněná cena.", Errors.MessageSeverity.Error);
            }

            return errors.Count == 0;
        }
    }
}