using System;
using System.Collections.Generic;
using System.Linq;

namespace HlidacStatu.Lib.Data.External.Zabbix
{


    public class ZabHostSslStatus
    {
        public class EndpointStatus
        {

            public string ipAddress { get; set; }
            public string serverName { get; set; }
            public SSLLabsGrades grade { get; set; }
        }

        static DateTime fixPolicieFrom = new DateTime(2017, 12, 18, 19, 05, 00);
        static DateTime fixPolicieTo = new DateTime(2017, 12, 19, 11, 10, 00);

        public ZabHostSslStatus(ZabHost host, IEnumerable<EndpointStatus> endpoints, DateTime time)
        {
            Host = host;
            Endpoints = endpoints.ToArray();
            Time = time;

        }

        public ZabHost Host { get; set; }
        public DateTime Time { get; set; }

        public SSLLabsGrades? Status()
        {
            if (Endpoints != null && Endpoints.Count() > 0)
            {
                return Endpoints
                    .Select(m => m.grade).Max();
            }
            else
                return null;
        }


        public EndpointStatus[] Endpoints { get; set; }

    }
}

