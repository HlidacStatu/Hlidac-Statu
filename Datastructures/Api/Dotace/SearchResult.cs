using HlidacStatu.DS.Api.Dotace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HlidacStatu.DS.Api.Subsidy
{
    public class SearchResult : MCP.MCPSimpleSearchResult
    {

        public ListItem[] Found_subsidies { get; set; } = null;
    }
}
