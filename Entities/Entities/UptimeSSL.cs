using System;
using System.Linq;

namespace HlidacStatu.Entities
{
    public partial class UptimeSSL
    {

        string _id = null;
        [Nest.Keyword]
        public string Id
        {
            get
            {
                if (_id == null)
                {
                    _id = InitId();
                }
                return _id;
            }
            set
            {
                _id = value;
                if (_id == null)
                    _id = InitId();
            }
        }

        private string InitId()
        {
            return Devmasters.Crypto.Hash.ComputeHashToHex($"{this.Domain}_{this.Created.Ticks.ToString()}");

        }

        [Nest.Keyword]
        public string Domain { get; set; }

        public Step[] Steps { get; set; }

        [Nest.Date]
        public DateTime Created { get; set; }

        [Nest.Keyword]
        public string OverallGrade { get; set; }

        [Nest.Number]
        public decimal? OverallScore { get; set; }
        public IP6Support IP6support { get; set; }


        public class Step
        {
            [Nest.Keyword]
            public string id { get; set; }

            [Nest.Keyword]
            public string ip { get; set; }
            [Nest.Keyword]
            public string port { get; set; }
            [Nest.Keyword]
            public string severity { get; set; }

            public string finding { get; set; }
            [Nest.Keyword]
            public string cwe { get; set; }
            [Nest.Keyword]
            public string cve { get; set; }

            [Nest.Object(Enabled = false)]
            public string htmlTextStyleColor
            {
                get
                {
                    string color = "text-body";
                    if (severity == "CRITICAL")
                        color = "text-danger";
                    else if (severity == "HIGH")
                        color = "text-danger";
                    else if (severity == "MEDIUM")
                        color = "text-primary";
                    else if (severity == "WARN")
                        color = "text-warning";
                    else if (severity == "LOW")
                        color = "text-muted";
                    else if (severity == "FATAL")
                        color = "text-warning bg-dark";

                    return color;
                }

            }


        }


        public class IP6Support
        {

            public class Address
            {
                public string Name { get; set; }
                public string IP6Address { get; set; }

                public bool? CanConnect { get; set; } = null;
            }

            public DateTime Date { get; set; }

            [Nest.Object(Enabled = false)]
            public string Site { get; set; }

            public Address[] Site_IPv6Addresses { get; set; }
            public Address[] Site_MX_IPv6Addresses { get; set; }
            public Address[] Site_NS_IPv6Addresses { get; set; }

            public string Log { get; set; }

            public bool IPv6Working()
            {
                return this.Site_IPv6Addresses?.Any(m => m.CanConnect == true) ?? false;
            }
        }
    }
}
