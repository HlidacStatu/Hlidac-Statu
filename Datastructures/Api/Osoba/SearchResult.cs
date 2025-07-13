using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HlidacStatu.DS.Api.Osoba
{
    public class SearchResult : MCP.MCPSimpleSearchResult
    {
        public ListItem[] Found_Persons { get; set; } = null;
    }
}
