using System;

namespace HlidacStatu.Entities
{
    public partial class UptimeItem
    {
        

        [Nest.Keyword]
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        [Nest.Date]
        public DateTime CheckStart { get; set; }

        [Nest.Date]
        public DateTime CheckEnd { get; set; }

        [Nest.Number]
        public decimal ResponseCode { get; set; }

        [Nest.Number]
        public decimal ResponseTimeInSec { get; set; }

        [Nest.Number]
        public long ResponseSize { get; set; }


        [Nest.Boolean]
        public bool ScreenshotTriggered { get; set; }





    }
}
