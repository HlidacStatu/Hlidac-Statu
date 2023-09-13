using System;

namespace HlidacStatu.Lib.Analysis.KorupcniRiziko
{
    public class Backup
    {
        [Nest.Keyword]
        public string Id { get; set; }

        public DateTime Created { get; set; }

        public string Comment { get; set; }
        public KIndexData KIndex { get; set; }
    }
}
