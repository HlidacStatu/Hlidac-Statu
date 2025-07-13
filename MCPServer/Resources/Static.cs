using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using static HlidacStatu.MCPServer.Resources.Static;

namespace HlidacStatu.MCPServer.Resources
{

    [McpServerResourceType()]

    public class Static
    {
        [
            McpServerResource(Name = "historic_view_valid_values", UriTemplate = "resource://hlidac_statu/historic_view_values"), 
            Description("Valid values for historic_view parameter with description.")]
        public static string Historic_View_Values()
        {

            return @"The 'historic_view' parameter defines the period in history for which links to subsidiaries are retrieved.
Parameter 'historic_view' could have only these values:
1. Aktualni: Current subsidiaries
2. Nedavny: Currently according to the commercial register or the last 5 years
3. Libovolny: At any time in the past until now.
";
            
        }


    }
}
