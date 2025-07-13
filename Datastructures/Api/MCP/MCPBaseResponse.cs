using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HlidacStatu.DS.Api.MCP
{
    public abstract class MCPBaseResponse
    {
        public string Source_Url { get; set; } = "https://www.hlidacstatu.cz";
        public string Copyright { get; set; } = $"(c) {DateTime.Now.Year} Hlídač Státu z.ú. Podmínky použití: https://texty.hlidacstatu.cz/licence/";

    }
}
