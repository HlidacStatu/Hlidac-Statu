using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HlidacStatu.DS.Api.Smlouva
{
    public class SearchResult : MCP.MCPSimpleSearchResult
    {

        public decimal? Total_Value_Of_Found_Contracts { get; set; } = 0;

        public ListItem[] Found_Contracts { get; set; } = null;
    }
}
