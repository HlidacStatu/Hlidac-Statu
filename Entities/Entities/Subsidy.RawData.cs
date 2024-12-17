using System.Collections.Generic;

namespace HlidacStatu.Entities;

public partial class Subsidy
{
    public class RawData
    {
        [Nest.Keyword]
        public string Id { get; set; }

        [Nest.Object]
        public Dictionary<string,object?> Items { get; set; }
    }
}