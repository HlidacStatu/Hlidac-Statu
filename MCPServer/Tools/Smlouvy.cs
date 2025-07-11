using ModelContextProtocol.Server;
using System.ComponentModel;

namespace HlidacStatu.MCPServer.Tools
{
    [McpServerToolType]
    public class Smlouvy
    {
        
        [McpServerTool(Name = "ContractDetailInfo", Title ="Return detail of specific contract between czech government and company"), Description ("Return detail of specific contract between government and company")]
        public async static Task<HlidacStatu.Entities.Smlouva> ContractDetailInfo(string IdOfContract, bool IncludeFullTextOfContract)
        {
            
            if (string.IsNullOrWhiteSpace(IdOfContract))
                return null;
            if (Util.DataValidators.CheckCZICO(IdOfContract) == false)
                return null;

            var res = await HlidacStatu.Repositories.SmlouvaRepo.LoadAsync(IdOfContract,includePrilohy: IncludeFullTextOfContract);

            return res;

        }

        [McpServerTool(Name = "ContractSearch", Title = "Find contracts based on query"), Description("Search contract of Czech government for specified parameters. You can combine any of parameters.")]
        public async static Task<HlidacStatu.Entities.Smlouva> Search(string IdOfContract, bool IncludeFullTextOfContract)
        {

            if (string.IsNullOrWhiteSpace(IdOfContract))
                return null;
            if (Util.DataValidators.CheckCZICO(IdOfContract) == false)
                return null;

            var res = await HlidacStatu.Repositories.SmlouvaRepo.LoadAsync(IdOfContract, includePrilohy: IncludeFullTextOfContract);

            return res;

        }
    }
}
