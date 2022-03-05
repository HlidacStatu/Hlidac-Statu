using Newtonsoft.Json;

using System;

namespace HlidacStatu.Entities
{
    public partial class UptimeServer
    {
        [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay,nq}")]

        public class Availability
        {
            public static decimal OKLimit = 1.000m;
            public static decimal SlowLimit = 2.000m;
            public static decimal TimeOuted = 99m;
            public static decimal TimeOuted2 = 99.2m;
            public static decimal BadHttpCode = 99.1m;

            public static UptimeSSL.Statuses GetStatus(decimal responseInMs)
            {
                if (responseInMs < OKLimit)
                    return UptimeSSL.Statuses.OK;
                else if (responseInMs < SlowLimit)
                    return UptimeSSL.Statuses.Pomalé;
                else
                    return UptimeSSL.Statuses.Nedostupné;

            }

            public static string GetStatusChartColor(UptimeSSL.Statuses status)
            {
                switch (status)
                {
                    case UptimeSSL.Statuses.OK:
                        return "#3c763d";
                    case UptimeSSL.Statuses.Pomalé:
                        return "#ff9600";
                    case UptimeSSL.Statuses.Nedostupné:
                        return "#db3330";
                    case UptimeSSL.Statuses.TimeOuted:
                        return "#961b19";
                    case UptimeSSL.Statuses.BadHttpCode:
                        return "#4c0908";
                    default:
                        return "#ddd";
                }
            }

            public DateTime Time { get; set; }
            public decimal? Response { get; set; } = null;

            public int? HttpStatusCode { get; set; } = null;

            [JsonIgnore()]
            public decimal? DownloadSpeed { get; set; } = null;


            public enum SimpleStatuses
            {
                OK = 0,
                //Slow = 1,
                Bad = 10,
                Unknown = -1,

            }
            public SimpleStatuses SimpleStatus()
            {
                switch (this.Status())
                {
                    case UptimeSSL.Statuses.OK:
                    case UptimeSSL.Statuses.Pomalé:
                        return SimpleStatuses.OK;
                        //return SimpleStatuses.Slow;
                    case UptimeSSL.Statuses.Nedostupné:
                    case UptimeSSL.Statuses.TimeOuted:
                    case UptimeSSL.Statuses.BadHttpCode:
                        return SimpleStatuses.Bad;
                    case UptimeSSL.Statuses.Unknown:
                    default:
                        return SimpleStatuses.Unknown;
                }
            }
            public UptimeSSL.Statuses Status()
            {
                if (Response.HasValue == false)
                {
                    if (HttpStatusCode.HasValue)
                    {
                        if (HttpStatusCode.Value >= 300)
                            return UptimeSSL.Statuses.Nedostupné;
                        if (HttpStatusCode.Value >= 200)
                            return UptimeSSL.Statuses.OK;
                    }
                    return UptimeSSL.Statuses.Unknown;
                }
                else
                    return GetStatus(Response.Value);
            }

            private string DebuggerDisplay
            {
                get
                {
                    return Time.ToString("M.d HH:mm:ss") + " " + (Response?.ToString() ?? "null") + " " + Status().ToString();
                }
            }
        }

    }
}
