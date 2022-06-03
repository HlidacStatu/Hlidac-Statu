using HlidacStatu.Entities;

namespace HlidacStatu.Web.Models.Apiv2
{
    public class NedostupnostModel
    {
        public UptimeServer Server { get; set; }
        public UptimeServer.AvailabilityStatistics Statistics { get; set; }
    }
}
