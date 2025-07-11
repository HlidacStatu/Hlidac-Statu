using HlidacStatu.DS.Api.Firmy;
using HlidacStatu.Entities;
using HlidacStatu.Extensions;
using HlidacStatu.Repositories;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace HlidacStatu.MCPServer.Tools
{
    [McpServerToolType]
    public class MCPFirmy
    {

        [McpServerTool(
            Name = "get_legal_entity_detail",
            Title = "Return detail information about Czech company with ICO"),
        Description("Returns the Czech Legal entity by its ICO (included companies and corporations,Government agencies and institutions, Cities and municipalities, Non-profit organizations adn all other subjects with legal personality).")]
        public static HlidacStatu.DS.Api.Firmy.FirmaDetailInfo SubjektDetailInfo(
            [Description("IČO of Legal entity to get detail information about.")]
            string ico,
            [Description("Name of Legal entity to get detail information about. If ICO is specified, the name is not used for filtering.")]
            string companyName
            )
        {

            if (string.IsNullOrWhiteSpace(ico))
                return null;
            if (Util.DataValidators.CheckCZICO(ico) == false)
                return null;

            DS.Api.Firmy.FirmaDetailInfo res = HlidacStatu.Repositories.FirmaRepo.GetDetailInfo(ico, companyName);

            return res;

        }

        [McpServerTool(
            Name = "get_government_offices_by_type",
            Title = "Get list of Czech government offices by type"),
            Description("Returns list of Czech government offices or cities by type. You can filter by type of it.")]
        public static SimpleDetailInfo[] get_government_offices_by_type(
            [Description("Type of government office to filter by.")]
            Entities.Firma.Zatrideni.SubjektyObory type
            )
        {
            var res = FirmaRepo.Zatrideni.Subjekty(type)
                                    .Select(m => new SimpleDetailInfo()
                                    {
                                        Ico = m.Ico,
                                        Jmeno = m.Jmeno,
                                        Kraj = m.Kraj,
                                        ZdrojUrl = HlidacStatu.Entities.Firma.GetUrl(m.Ico, false),
                                    })
                    .DistinctBy(m=>m.Ico)
                    .ToArray();
            ;


            return res;
        }

        [McpServerTool(
            Name = "get_subsidiaries_of_legal_entity",
            Title = "Get list of IČO and name of all subsidiaries of Czech legal entity with provided IČO"),
            Description("Get list of IČO and name of all subsidiaries of Czech legal entity with provided IČO.")]
        public static SimpleDetailInfo[] get_subsidiaries_of_legal_entity(
            [Description("IČO of Legal entity to get subsidiaries of.")]
            string ico, 
            [Description("Historic view to get subsidiaries. Valid values are: \n" 
            + "Aktualni : current status; \n"
            + "Nedavny : AktCurrently according to the commercial register or the last 5 years; \n"
            + "Libovolny : Currently according to the commercial register or at any time in the past")]
            DS.Graphs.Relation.AktualnostType historic_view = DS.Graphs.Relation.AktualnostType.Nedavny)
        {
            if (string.IsNullOrWhiteSpace(ico))
                return null;
            if (Util.DataValidators.CheckCZICO(ico) == false)
                return null;
            Firma f = HlidacStatu.Repositories.Firmy.Get(ico);
            if (f.Valid == false)
                return null;

            var res = f.IcosInHolding(historic_view)
                    .Select(m => new SimpleDetailInfo()
                    {
                        Ico = m,
                        Jmeno = HlidacStatu.Repositories.Firmy.GetJmeno(m),
                        ZdrojUrl = HlidacStatu.Entities.Firma.GetUrl(m, false),
                    })
                    .ToArray();
            ;


            return res;
        }

    }
}
