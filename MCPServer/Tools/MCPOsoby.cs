using Devmasters.Enums;
using HlidacStatu.Extensions;
using HlidacStatu.Repositories;
using HlidacStatu.Repositories.Statistics;
using ModelContextProtocol.Server;
using NPOI.SS.Formula.Functions;
using System.ComponentModel;

namespace HlidacStatu.MCPServer.Tools
{
    [McpServerToolType]
    public class MCPOsoby
    {
        static Serilog.ILogger _logger = Serilog.Log.ForContext<MCPOsoby>();

        [McpServerTool(
    Name = "get_politician_detail",
    Title = "Get detail of specific politician in Czech Republic"),
    Description("Return detail of specific politician in Czech Republic by his ID. Use method 'find_person_by_name' or 'find_politician_by_name' to find person's ID. "
    + " For company detail use tool 'get_legal_entity_full_detail' with parameter 'ico'.")]
        public static HlidacStatu.DS.Api.Osoba.Detail get_politician_detail(
    [Description("ID of person.")]
            string person_id)
        {
            return get_person_detail(person_id);
        }


            [McpServerTool(
            Name = "get_person_detail",
            Title = "Get detail of specific person in Czech Republic"),
            Description("Return detail of specific person in Czech Republic by his ID. Use method 'find_person_by_name' to find person's ID. "
            + " For company detail use tool 'get_legal_entity_full_detail' with parameter 'ico'.")]
        public static HlidacStatu.DS.Api.Osoba.Detail get_person_detail(
            [Description("ID of person.")]
            string person_id)
        {
            if (string.IsNullOrWhiteSpace(person_id) || string.IsNullOrWhiteSpace(person_id))
                return null;
            Entities.Osoba o = HlidacStatu.Repositories.Osoby.GetByNameId.Get(person_id);


            var res = o.ToApiOsobaDetail(DateTime.Now.AddYears(-10));
            return res;
        }

        [McpServerTool(
            Name = "find_politicians_by_name",
            Title = "Find politicians by name and optional by year of birth."),
            Description("Find politicians by searching with their name. Always use order 'first name' and 'last name'. You may also provide a year of birth parameter to enhance the precision of search results. "
            + "Find other poeple by using tool 'find_persons_by_name'. "
            + " For person detail use tool 'get_politician_detail' with parameter 'person_id'.")]
        public static HlidacStatu.DS.Api.Osoba.SearchResult find_politicians_by_name(
            [Description("full name of person")]
            string name,
            [Description("Year of Birth of person, optional")]
            string? year_of_birth = null
            )
        {
            int numOfResults = 10;
            return _find_persons_by_name(numOfResults, name, year_of_birth, true);
        }

        [McpServerTool(
        Name = "find_persons_by_name",
        Title = "Find persons by name and optional by year of birth."),
        Description("Find people by searching with their name. Always use order 'first name' and 'last name'. You may also provide a year of birth parameter to enhance the precision of search results. "
        + " For person detail use tool 'get_person_detail' with parameter 'person_id'.")]
        public static HlidacStatu.DS.Api.Osoba.SearchResult find_persons_by_name(
        [Description("full name of person")]
            string name,
        [Description("Year of Birth of person, optional")]
            string? year_of_birth = null
        )
        {
            int numOfResults = 10;
            return _find_persons_by_name(numOfResults, name, year_of_birth, false);
        }


        public static HlidacStatu.DS.Api.Osoba.SearchResult _find_persons_by_name(
            int numOfResults,
            string name,
            string? year_of_birth = null,
            bool onlyPoliticians = false
            )
        {


            var ofound = OsobaRepo.Searching.FindAll(name, year_of_birth, false)
                     .OrderByDescending(o => o.Status)
                     .ThenByDescending(o => o.Narozeni ?? new DateTime(1970, 1, 1))
                     .Take(numOfResults)
                     .ToList();

            if (ofound.Any() == false && onlyPoliticians == false)
                ofound = OsobaRepo.Searching.FindAll(name, year_of_birth, true)
                    .OrderByDescending(o => o.Status)
                    .ThenByDescending(o => o.Narozeni ?? new DateTime(1970, 1, 1))
                    .Take(numOfResults)
                    .ToList();

            var res = ofound.Select(o => o.ToApiOsobaListItem())
                .ToArray();

            return new DS.Api.Osoba.SearchResult()
            {
                Current_Page = 1,
                Found_Persons = res,
                Total_Found_Results = res.Length,
                Total_Pages = 1,
                Page_Size = 10,
            };

        }
    }
}