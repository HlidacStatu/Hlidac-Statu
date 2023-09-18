using System;

namespace HlidacStatu.Entities.Entities.KIndex
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
