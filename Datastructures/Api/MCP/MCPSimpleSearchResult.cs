using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HlidacStatu.DS.Api.MCP
{
    public abstract class MCPSimpleSearchResult : MCPBaseResponse
    {
        public int Current_Page { get; set; } = 0;
        public int Page_Size { get; set; } = 0;
        public int Total_Pages { get; set; } = 0;
        public long Total_Found_Results { get; set; } = 0;


    }
}
