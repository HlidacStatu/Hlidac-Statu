﻿using HlidacStatu.DS.Api.Firmy;
using HlidacStatu.Entities;
using HlidacStatu.Extensions;
using HlidacStatu.Repositories;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Reflection;

namespace HlidacStatu.MCPServer.Tools
{
    [McpServerToolType]
    public class MCPFirmy
    {
        static Serilog.ILogger _logger = Serilog.Log.ForContext<MCPFirmy>();

        [McpServerTool(
          //UseStructuredContent =true,
          Name = "get_legal_entity_business_info",
          Title = "Return detail information about Czech company with ICO"),
        Description("Returns basic business info abou the Czech Legal entity (included corporations, government institutions, municipalities, NGO and all other subjects )."
            + "Specify legal entity by ICO or name. Use primary ICO if it's available."
            + " For person detail use tool 'get_person_detail' with parameter 'person_id'.")]
        public static HlidacStatu.DS.Api.Firmy.SubjektFinancialInfo SubjektFinancialInfo(IMcpServer server,
          [Description("IČO of Legal entity to get detail information about.")]
            string ico,
          [Description("Name of Legal entity to get detail information about. If ICO is specified, the name is not used for filtering.")]
            string company_Name
          )
        {
            return AuditRepo.AddWithElapsedTimeMeasure(
                Audit.Operations.Call,
                server?.ServerOptions?.KnownClientInfo?.Name?.Split('|')?.FirstOrDefault(),
                server?.ServerOptions?.KnownClientInfo?.Name?.Split('|')?.LastOrDefault(),
                AuditRepo.GetClassAndMethodName(MethodBase.GetCurrentMethod()), "",
                AuditRepo.GetMethodParametersWithValues(MethodBase.GetCurrentMethod().GetParameters().Skip(1), ico, company_Name),
                null, () =>
                {

                    if (string.IsNullOrWhiteSpace(ico) && string.IsNullOrWhiteSpace(company_Name))
                        return null;

                    SubjektFinancialInfo res = HlidacStatu.Repositories.FirmaRepo.GetFinancialInfo(ico, company_Name);

                    return res;
                });

        }



        [McpServerTool(
            //UseStructuredContent = true,
            Name = "get_business_between_legal_entity_and_government",
            Title = "Return detail information about Czech company with ICO"),
        Description("Returns detail of business contracts, subsidies and statistics between the legal entity and the Czech government. "
            + "Specify legal entity by ICO or name. Use primary ICO if it's available."
            + " For person detail use tool 'get_person_detail' with parameter 'person_id'.")]
        public static HlidacStatu.DS.Api.Firmy.SubjektDetailInfo SubjektDetailInfo(IMcpServer server,
            [Description("IČO of Legal entity to get detail information about.")]
            string ico,
            [Description("Name of Legal entity to get detail information about. If ICO is specified, the name is not used for filtering.")]
            string company_Name
            )
        {
            return AuditRepo.AddWithElapsedTimeMeasure(
                Audit.Operations.Call,
                server?.ServerOptions?.KnownClientInfo?.Name?.Split('|')?.FirstOrDefault(),
                server?.ServerOptions?.KnownClientInfo?.Name?.Split('|')?.LastOrDefault(),
                AuditRepo.GetClassAndMethodName(MethodBase.GetCurrentMethod()), "",
                AuditRepo.GetMethodParametersWithValues(MethodBase.GetCurrentMethod().GetParameters().Skip(1), ico, company_Name),
                null, () =>
                {

                    if (string.IsNullOrWhiteSpace(ico))
                        return null;
                    if (Util.DataValidators.CheckCZICO(ico) == false)
                        return null;

                    DS.Api.Firmy.SubjektDetailInfo res = HlidacStatu.Repositories.FirmaRepo.GetDetailInfo(ico, company_Name);

                    return res;
                });
        }

        [McpServerTool(
            //UseStructuredContent = true,
            Name = "get_government_offices_by_type",
            Title = "Get list of Czech government offices by type"),
            Description("Returns list of Czech government offices or cities by type. You can filter by type of it.")]
        public static SimpleDetailInfo[] get_government_offices_by_type(IMcpServer server,
            [Description("Type of government office to filter by.")]
            Entities.Firma.Zatrideni.SubjektyObory type
            )
        {
            return AuditRepo.AddWithElapsedTimeMeasure(
                Audit.Operations.Call,
                server?.ServerOptions?.KnownClientInfo?.Name?.Split('|')?.FirstOrDefault(),
                server?.ServerOptions?.KnownClientInfo?.Name?.Split('|')?.LastOrDefault(),
                AuditRepo.GetClassAndMethodName(MethodBase.GetCurrentMethod()), "",
                AuditRepo.GetMethodParametersWithValues(MethodBase.GetCurrentMethod().GetParameters().Skip(1), type),
                null, () =>
                {

                    var res = FirmaRepo.Zatrideni.Subjekty(type)
                                            .Select(m => new SimpleDetailInfo()
                                            {
                                                Ico = m.Ico,
                                                Jmeno = m.Jmeno,
                                                Kraj = m.Kraj,
                                                Source_Url = HlidacStatu.Entities.Firma.GetUrl(m.Ico, false),
                                            })
                            .DistinctBy(m => m.Ico)
                            .ToArray();
                    ;


                    return res;
                });
        }

        [McpServerTool(
            //UseStructuredContent = true,
            Name = "get_subsidiaries_of_legal_entity",
            Title = "Get list of IČO and name of all subsidiaries of Czech legal entity with provided IČO"),
            Description("Get list of IČO and name of all subsidiaries of Czech legal entity with provided IČO.")]
        public static SimpleDetailInfo[] get_subsidiaries_of_legal_entity(IMcpServer server,
            [Description("IČO of Legal entity to get subsidiaries of.")]
            string ico,
            [Description("Historic view to get subsidiaries. Valid values are: \n"
            + "Aktualni : current status; \n"
            + "Nedavny : Currently according to the commercial register or the last 5 years; \n"
            + "Libovolny : Currently according to the commercial register or at any time in the past")]
            DS.Graphs.Relation.AktualnostType historic_view = DS.Graphs.Relation.AktualnostType.Nedavny)
        {

            return AuditRepo.AddWithElapsedTimeMeasure(
                Audit.Operations.Call,
                server?.ServerOptions?.KnownClientInfo?.Name?.Split('|')?.FirstOrDefault(),
                server?.ServerOptions?.KnownClientInfo?.Name?.Split('|')?.LastOrDefault(),
                AuditRepo.GetClassAndMethodName(MethodBase.GetCurrentMethod()), "",
                AuditRepo.GetMethodParametersWithValues(MethodBase.GetCurrentMethod().GetParameters().Skip(1), ico, historic_view),
                null, () =>
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
                                Source_Url = HlidacStatu.Entities.Firma.GetUrl(m, false),
                            })
                            .ToArray();
                    ;


                    return res;
                });
        }

    }
}
