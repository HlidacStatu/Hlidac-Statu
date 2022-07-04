using System;

namespace HlidacStatu.Entities
{
    public partial class UptimeServer
    {
        public class WebStatusExport
        {
            public class SslData
            {
                public string Grade { get; set; }
                public DateTime? LatestCheck { get; set; }
                public DateTime? SSLExpiresAt { get; set; }
            }
            public HostAvailability Availability { get; set; }
            public SslData SSL { get; set; }
            public UptimeSSL.IP6Support IPv6Support { get; set; }
            public string DetailUrl { get; set; }
        }

    }
}
