using System;
using Newtonsoft.Json.Linq;

namespace HlidacStatu.Entities
{
    public partial class InDocTableCells
    {
        [Nest.Keyword]
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        [Nest.Keyword]
        public int Page { get; set; }
        [Nest.Keyword]
        public int TableOnPage { get; set; }
        [Nest.Keyword]
        public string Algorithm { get; set; }
        [Nest.Keyword]
        public string SmlouvaID { get; set; }
        [Nest.Keyword]
        public string PrilohaHash { get; set; }
        [Nest.Date]
        public DateTime Date { get; set; }
        [Nest.Object(Enabled = false)]
        public string Cells { get; set; }

    }
}
