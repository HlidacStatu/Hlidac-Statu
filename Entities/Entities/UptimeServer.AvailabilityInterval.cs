using System;

namespace HlidacStatu.Entities
{
    public partial class UptimeServer
    {
        public class AvailabilityInterval
        {
            public AvailabilityInterval()
            {
            }
            public AvailabilityInterval(DateTime from, DateTime to, decimal responseInSec)
            {
                From = from;
                To = to;
                Status = Availability.GetStatus(responseInSec);
            }
            public AvailabilityInterval(DateTime from, DateTime to, UptimeSSL.Statuses status)
            {
                From = from;
                To = to;
                Status = status;
            }


            public DateTime From { get; set; }
            public DateTime To { get; set; }
            public UptimeSSL.Statuses Status { get; set; }
        }

    }
}
