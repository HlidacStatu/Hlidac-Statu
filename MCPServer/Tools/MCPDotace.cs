using ModelContextProtocol.Server;
using System.ComponentModel;

namespace HlidacStatu.MCPServer.Tools
{
    [McpServerToolType]
    public class MCPDotace
    {


        [McpServerTool(
            Name = "get_subsidy_detail",
            Title = "Get detail of specific subsidy from czech government or EU"),
        Description("Return detail of specific subsidy from czech government or EU")]
        public async static Task<HlidacStatu.Entities.Dotace> Get_subsidy_detail(
            [Description("ID of subsidy")]
            string subsidy_id)
        {
            if (string.IsNullOrWhiteSpace(subsidy_id))
                return null;

            Entities.Dotace dotace = await HlidacStatu.Repositories.DotaceRepo.GetAsync(subsidy_id);

            return dotace;
        }
    }
}
