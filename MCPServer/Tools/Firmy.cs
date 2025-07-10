using ModelContextProtocol.Server;
using System.ComponentModel;

namespace HlidacStatu.MCPServer.Tools
{
    [McpServerToolType]
    public class Firmy
    {
        
        [McpServerTool(Name = "CompanyDetailInfo", Title ="Return detail information about company with ICO"), Description ("Returns the company by its ICO.")]
        public static HlidacStatu.DS.Api.Firmy.FirmaDetailInfo CompanyDetailInfo(string ico, string companyName)
        {
            
            if (string.IsNullOrWhiteSpace(ico))
                return null;
            if (Util.DataValidators.CheckCZICO(ico) == false)
                return null;

            DS.Api.Firmy.FirmaDetailInfo res = HlidacStatu.Repositories.FirmaRepo.GetDetailInfo(ico, companyName);

            return res;

        }
    }
}
