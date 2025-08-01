using System;

namespace HlidacStatu.Entities;

public partial class PuEvent
{
    public class EventDescription
    {
        public int Pk { get; set; }
        public DateTime Date { get; set; }
        public string? Title { get; set; }
        public string? Note { get; set; }
        public SmerKomunikace Smer { get; set; }
        public NegativityLevel Negativity { get; set; } = NegativityLevel.Neutral;
        public enum NegativityLevel
        {
            HighIssue = -2,
            LowIssue = -1,
            Neutral = 0,
            Ok = 1,
        }
        public string BootStrapColor() => Negativity switch
        {
            NegativityLevel.HighIssue => "danger",
            NegativityLevel.LowIssue => "warning",
            NegativityLevel.Neutral => "secondary",
            NegativityLevel.Ok => "success",
            _ => "secondary"
        };
    }

}