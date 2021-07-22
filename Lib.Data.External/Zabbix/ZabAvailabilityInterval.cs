using System;

namespace HlidacStatu.Lib.Data.External.Zabbix
{
    public class ZabAvailabilityInterval
    {
        public ZabAvailabilityInterval()
        {
        }
        public ZabAvailabilityInterval(DateTime from, DateTime to, decimal responseInSec)
        {
            From = from;
            To = to;
            Status = ZabAvailability.GetStatus(responseInSec);
        }
        public ZabAvailabilityInterval(DateTime from, DateTime to, Statuses status)
        {
            From = from;
            To = to;
            Status = status;
        }


        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public Statuses Status { get; set; }
    }
}