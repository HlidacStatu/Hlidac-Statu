using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HlidacStatu.Entities
{
    public class SearchPromo
    {

        [Nest.Keyword]
        public string Id { get; set; }

        [Nest.Keyword]
        public string PromoType { get; set; }

        [Nest.Text]
        public string Title { get; set; }

        [Nest.Text]
        public string Description { get; set; }

        [Nest.Text(Index = false)]
        public string More { get; set; }

        [Nest.Text(Boost =2)]
        public string Fulltext { get; set; }

        [Nest.Keyword(Index=false)]
        public string Icon { get; set; }

        [Nest.Keyword(Index = false)]
        public string Url { get; set; }

        [Nest.Number]
        public int Priority { get; set; } = 100;

        [Nest.Keyword]
        public string OsobaId { get; set; } = null;
        [Nest.Keyword]
        public string Ico { get; set; } = null;


    }
}
