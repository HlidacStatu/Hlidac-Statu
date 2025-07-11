using HlidacStatu.Entities;
using HlidacStatu.Repositories;
using ModelContextProtocol.Server;
using RazorEngine.Compilation.ImpromptuInterface;
using System.ComponentModel;

namespace HlidacStatu.MCPServer.Tools
{
    [McpServerToolType]
    public class MCPSmlouvy
    {

        [McpServerTool(
            Name = "get_contract_detail",
            Title = "Get detail of specific contract of czech government.  "),
        Description("Return detail of specific contract between government and company")]
        public async static Task<HlidacStatu.Entities.Smlouva> ContractDetailInfo(
            [Description("ID of contract.")]
            string contract_id,

            [Description("Set true to get full text of contract. Text of contract can be a very long text.")]
            [DefaultValue(false)]
            bool include_text_of_contract = false)
        {

            if (string.IsNullOrWhiteSpace(contract_id))
                return null;
            if (Util.DataValidators.CheckCZICO(contract_id) == false)
                return null;

            var res = await HlidacStatu.Repositories.SmlouvaRepo.LoadAsync(contract_id, includePrilohy: include_text_of_contract);

            return res;

        }

        [McpServerTool(
            Name = "search_contracts",
            Title = "Find contracts based on parameters"),
        Description("Search contract of Czech government for specified parameters. You can combine any of parameters.")]
        public async static Task<HlidacStatu.Entities.Smlouva[]> Search(

                [Description("Array of contract classification types to filter search results by specific categories")]
    HlidacStatu.Entities.Smlouva.SClassification.ClassificationsTypes[]? categories = null,

    [Description("Start date for date range filter in YYYY-MM-DD format. Only contracts from this date onwards will be included")]
    string? from_date = null,

    [Description("End date for date range filter in YYYY-MM-DD format. Only contracts up to this date will be included")]
    string? to_date = null,

    [Description("Minimum contract value in Czech Koruna (CZK). Filters out contracts below this monetary threshold")]
    string? minimal_price = null,

    [Description("Maximum contract value in Czech Koruna (CZK). Filters out contracts above this monetary threshold")]
    string? maximal_price = null,

    [Description("Array of ICO numbers (Czech company identification numbers) to filter contracts by specific contracting parties")]
    string[]? ICOs_of_contracting_party = null,

    [Description("ICO number of a holding company structure to filter contracts within a specific corporate hierarchy")]
    string? ICO_of_holding_structure = null,

    [Description("Space-separated search keywords that must all be present in contract content (AND logic applied)")]
    string? keywords = null,

    [Description("Space-separated keywords to exclude from results. Contracts containing any of these terms will be filtered out")]
    string? negative_keywords = null,

    [Description("Maximum number of contract records to return per page for pagination purposes. Default value is 10")]
    int number_of_results = 10,

    [Description("Page number for pagination, starting from 1. Use with number_of_results to navigate through large result sets")]
    int page = 1,

    [Description("Sorting order for search results. Determines how contracts are ordered in the response (e.g., by relevance, date, price)")]
    SmlouvaRepo.Searching.OrderResult order_result = SmlouvaRepo.Searching.OrderResult.Relevance

            )
        {
            Smlouva[] res = Array.Empty<Smlouva>();

            string[] splitChars = new string[] { " " };
            string query = "";


            if (!string.IsNullOrWhiteSpace(keywords))
            {
                query += " " + keywords;
            }

            if (!string.IsNullOrWhiteSpace(negative_keywords))
            {
                query += " " + negative_keywords.ToString().Split(splitChars, StringSplitOptions.RemoveEmptyEntries).Select(s => s.StartsWith("-") ? s : "-" + s).Aggregate((f, s) => f + " " + s);
            }

            List<KeyValuePair<string, string>> icos = new();
            if (ICOs_of_contracting_party != null)
                foreach (var val in ICOs_of_contracting_party
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Where(s=> Devmasters.TextUtil.IsNumeric(s))
                    )
                {
                    icos.Add(new KeyValuePair<string, string>("ico", val));
                }

            List<KeyValuePair<string, string>> holding = new();
            if (ICO_of_holding_structure != null && Devmasters.TextUtil.IsNumeric(ICO_of_holding_structure))
            {
                holding.Add(new KeyValuePair<string, string>("holding", ICO_of_holding_structure));
            }


            if (icos.Count(m => !string.IsNullOrWhiteSpace(m.Value)) > 1)
            { // into ()
                query += " ("
                        + icos.Where(m => !string.IsNullOrWhiteSpace(m.Value)).Select(m => m.Key + ":" + m.Value).Aggregate((f, s) => f + " OR " + s)
                        + ")";
            }
            
            if (holding.Count(m => !string.IsNullOrWhiteSpace(m.Value)) == 1)
            {
                query += " " + holding.Where(m => !string.IsNullOrWhiteSpace(m.Value)).Select(m => m.Key + ":" + m.Value).First();
            }

            if (Devmasters.TextUtil.IsNumeric(minimal_price))
                query += " cena:>=" + minimal_price;

            if (Devmasters.TextUtil.IsNumeric(maximal_price))
                query += " cena:<=" + maximal_price;

            var fdate = Devmasters.DT.Util.ToDate(from_date);
            var tdate = Devmasters.DT.Util.ToDate(to_date);
            if (fdate.HasValue && tdate.HasValue)
            {
                query += $" zverejneno:[{fdate:yyyy-MM-dd} TO {tdate:yyyy-MM-dd}]";
            }
            else if (fdate.HasValue)
            {
                query += $" zverejneno:[{fdate:yyyy-MM-dd} TO *]";
            }
            else if (tdate.HasValue)
            {
                query += $" zverejneno:[* TO {tdate:yyyy-MM-dd}]";
            }


            if (categories?.Length > 0)
            {
                query += " ( " 
                    + string.Join(" OR ", categories.Select(obor => "oblast:" + obor.ToString().ToLowerInvariant()))
                    + " ) ";
            }

            query = query.Trim();
            if (query.Length == 0)
            {
                return res;
            }

            var sres = await SmlouvaRepo.Searching.SimpleSearchAsync(query, page,
                number_of_results,
                order_result,
                includeNeplatne: false,
                anyAggregation: new Nest.AggregationContainerDescriptor<Smlouva>().Sum("sumKc", m => m.Field(f => f.CalculatedPriceWithVATinCZK)),
                logError: false);

            if (sres?.IsValid == true && sres?.Results?.Count() > 0)
                res = sres.Results.ToArray();

            return res;

        }
    }
}
