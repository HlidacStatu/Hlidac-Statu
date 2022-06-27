using HlidacStatu.Entities;

namespace HlidacStatuApi.Models
{
    public class NedostupnostModel
    {
        public UptimeServer Server { get; set; }
        public UptimeServer.AvailabilityStatistics Statistics { get; set; }
    }
}
