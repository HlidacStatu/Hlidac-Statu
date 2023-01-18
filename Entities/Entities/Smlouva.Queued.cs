using System;

namespace HlidacStatu.Entities
{

    public partial class Smlouva
    {
        public class Queued
        {
            public string SmlouvaId { get; set; }
            public DateTime AddedToQueue { get; set; } = DateTime.Now;
            public bool ForceOCR { get; set; }
            public bool ForceClassification { get; set; }
            public bool ForceTablesMining { get; set; }
            public bool ForceBlurredPages { get; set; }
            public ulong? ItemIdInQueue { get; set; }
        }

    }
}
