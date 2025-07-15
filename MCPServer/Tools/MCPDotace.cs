using HlidacStatu.Entities;
using HlidacStatu.Repositories;
using ModelContextProtocol.Server;
using OpenAI.Moderations;
using System.ComponentModel;
using System.Reflection;

namespace HlidacStatu.MCPServer.Tools
{
    [McpServerToolType]
    public class MCPDotace
    {
        static Serilog.ILogger _logger = Serilog.Log.ForContext<MCPDotace>();


        [McpServerTool(
            Name = "get_subsidy_detail",
            Title = "Get detail of specific subsidy from czech government or EU"),
        Description("Return detail of specific subsidy from czech government or EU")]
        public async static Task<HlidacStatu.DS.Api.Dotace.Detail> Get_subsidy_detail(IMcpServer server,
            [Description("ID of subsidy")]
            string subsidy_id)
        {
            return await AuditRepo.AddWithElapsedTimeMeasureAsync(
                Audit.Operations.Call,
                server?.ServerOptions?.KnownClientInfo?.Name?.Split('|')?.FirstOrDefault(),
                server?.ServerOptions?.KnownClientInfo?.Name?.Split('|')?.LastOrDefault(),
                AuditRepo.GetClassAndMethodName(MethodBase.GetCurrentMethod()), "",
                AuditRepo.GetMethodParametersWithValues(MethodBase.GetCurrentMethod().GetParameters().Skip(1), subsidy_id),
                null, async () =>
                {

                    if (string.IsNullOrWhiteSpace(subsidy_id))
                        return null;

                    Entities.Dotace dotace = await HlidacStatu.Repositories.DotaceRepo.GetAsync(subsidy_id);

                    return dotace.ToApiSubsidyDetail();
                });
        }


        [McpServerTool(
    Name = "search_subsidies",
    Title = "Find subsidies based on parameters"),
Description("Search subsidies from Czech government for specified parameters. You can combine any of parameters.")]
        public async static Task<DS.Api.Subsidy.SearchResult> Search_subsidies(IMcpServer server,


[Description("Year filter in YYYY format. Only subsidies from this year onwards will be included")]
    int? for_year = null,


[Description("Minimum subsidy amount in Czech Koruna (CZK). Filters out subsidies below this monetary threshold")]
    string? minimal_amount = null,

[Description("Maximum subsidy amount in Czech Koruna (CZK). Filters out subsidies above this monetary threshold")]
    string? maximal_amount = null,

[Description("ICO number (Czech company identification numbers). Filters out subsidies provided by selected provider's ICOs")]
    string? subsidy_provider_ICO = null,

[Description("ICO number (Czech company identification numbers). Filters out subsidies received by selected recipient's ICOs")]
    string? subsidy_recipient_ICO = null,

[Description("ICO number (Czech company identification numbers). Filters out subsidies received by selected corporate hierarchy")]
    string? ICO_of_holding_structure = null,

[Description("Space-separated search keywords that must all be present in subsidy content (AND logic applied)")]
    string? keywords = null,

[Description("Space-separated keywords to exclude from results. subsidies containing any of these terms will be filtered out")]
    string? negative_keywords = null,

[Description("Maximum number of subsidy records to return per page for pagination purposes. Default value is 10")]
    int number_of_results = 10,

[Description("Page number for pagination, starting from 1. Use with number_of_results to navigate through large result sets")]
    int page = 1,

[Description("Sorting order for search results. Determines how subsidies are ordered in the response (e.g., by relevance, date, price)")]
    Repositories.Searching.DotaceSearchResult.DotaceOrderResult order_result = Repositories.Searching.DotaceSearchResult.DotaceOrderResult.Relevance
    )
        {
            return await AuditRepo.AddWithElapsedTimeMeasureAsync(Audit.Operations.Call,
                server?.ServerOptions?.KnownClientInfo?.Name?.Split('|')?.FirstOrDefault(),
                server?.ServerOptions?.KnownClientInfo?.Name?.Split('|')?.LastOrDefault(),
                AuditRepo.GetClassAndMethodName(MethodBase.GetCurrentMethod()), "",
                AuditRepo.GetMethodParametersWithValues(MethodBase.GetCurrentMethod().GetParameters().Skip(1), for_year, minimal_amount, maximal_amount, subsidy_provider_ICO, subsidy_recipient_ICO, ICO_of_holding_structure, keywords, negative_keywords, number_of_results, page, order_result),
                null, async () =>
                {

                    string[] splitChars = new string[] { " " };
                    string query = "";


                    if (!string.IsNullOrWhiteSpace(keywords))
                    {
                        query += " " + keywords;
                    }

                    if (!string.IsNullOrWhiteSpace(negative_keywords))
                    {
                        query += " NOT ( "
                            + negative_keywords.ToString().Split(splitChars, StringSplitOptions.RemoveEmptyEntries).Select(s => s.StartsWith("-") ? s : "-" + s).Aggregate((f, s) => f + " " + s)
                            + " ) ";
                    }

                    if (subsidy_recipient_ICO != null && Devmasters.TextUtil.IsNumeric(subsidy_recipient_ICO))
                        query += " recipient.ico:" + subsidy_recipient_ICO;

                    if (subsidy_provider_ICO != null && Devmasters.TextUtil.IsNumeric(subsidy_provider_ICO))
                        query += " subsidyProviderIco:" + subsidy_provider_ICO;

                    if (ICO_of_holding_structure != null && Devmasters.TextUtil.IsNumeric(ICO_of_holding_structure))
                        query += " holding:" + ICO_of_holding_structure;


                    if (Devmasters.TextUtil.IsNumeric(minimal_amount))
                        query += " castka:>=" + minimal_amount;

                    if (Devmasters.TextUtil.IsNumeric(maximal_amount))
                        query += " castka:<=" + maximal_amount;

                    if (for_year.HasValue)
                    {
                        query += $" approvedYear:{for_year}";
                    }

                    query = query.Trim();
                    if (query.Length == 0)
                    {
                        return null;
                    }

                    var sres = await DotaceRepo.Searching.SimpleSearchAsync(query, page,
                        number_of_results,
                        order_result);

                    if (sres?.IsValid == true && sres?.Results?.Count() > 0)
                    {
                        var res = new DS.Api.Subsidy.SearchResult()
                        {
                            Total_Found_Results = sres.Total,
                            Current_Page = sres.Page,
                            Page_Size = number_of_results,
                            Found_subsidies = sres.Results.Select(m => m.ToApiSubsidyListItem()).ToArray(),
                        };

                        return res;
                    }


                    return null;

                });
        }
    }
}
