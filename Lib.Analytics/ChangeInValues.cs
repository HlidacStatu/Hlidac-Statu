namespace HlidacStatu.Lib.Analytics
{
    public class ChangeInValues
    {
        public decimal PrevValue {get;set;}
        public decimal CurrValue { get; set; }
        public decimal ValueChange { get; set; }
        public decimal? PercentChange { get; set; }
    }

}