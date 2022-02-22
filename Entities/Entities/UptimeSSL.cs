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
        }


    }
}
