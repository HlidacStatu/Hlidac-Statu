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


            DateTime historyLimit = DateTime.Now.AddYears(-10);
            var res = new HlidacStatu.DS.Api.Osoba.Detail
            {
                Person_Id = o.NameId,
                Name = o.Jmeno,
                Surname = o.Prijmeni,
                Year_Of_Birth = o.Narozeni.HasValue ? o.Narozeni.Value.Year.ToString() : null,
                Political_Involvement = o.StatusOsoby().ToNiceDisplayName(),
                Photo_Url = o.HasPhoto() ? o.GetPhotoUrl(false) : null,
                Current_Political_Party = o.CurrentPoliticalParty() ?? "None",

                Recent_Public_Activities_Description = o.Description(false, m => m.DatumDo == null || m.DatumDo > historyLimit, 5, itemDelimeter: ", "),

                Involved_In_Companies = o.AktualniVazby(DS.Graphs.Relation.AktualnostType.Nedavny)
                    .Where(v => v.To != null && v.To.Type == HlidacStatu.DS.Graphs.Graph.Node.NodeType.Company)
                    .GroupBy(f => f.To.Id, v => v, (ico, v) => new
                    {
                        ICO = ico,
                        FirmaName = v.First().To.PrintName(),//HlidacStatu.Lib.Data.External.FirmyDB.NameFromIco(ico, true),
                    })
                    .Select(m => new HlidacStatu.DS.Api.Osoba.Detail.Subject
                    {
                        Ico = m.ICO,
                        Name = m.FirmaName
                    }).ToArray(),

                Business_Contracts_With_Government = o.StatistikaRegistrSmluv(DS.Graphs.Relation.AktualnostType.Nedavny)
                .SmlouvyStat_SoukromeFirmySummary()
                    .Select(m => new HlidacStatu.DS.Api.Osoba.Detail.Stats
                    {
                        Year = m.Year,
                        Number_Of_Contracts = m.Value.PocetSmluv,
                        Total_Contract_Value = m.Value.CelkovaHodnotaSmluv
                    }).ToArray()

            };
            return res;
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



            var ofound = OsobaRepo.Searching.FindAll(name, year_of_birth, false)
                     .OrderByDescending(o => o.Status)
                     .ThenByDescending(o => o.Narozeni ?? new DateTime(1970, 1, 1))
                     .Take(numOfResults)
                     .ToList();

            if (ofound.Any() == false)
                ofound = OsobaRepo.Searching.FindAll(name, year_of_birth, true)
                    .OrderByDescending(o => o.Status)
                    .ThenByDescending(o => o.Narozeni ?? new DateTime(1970, 1, 1))
                    .Take(numOfResults)
                    .ToList();

            var res = ofound.Select(o => new HlidacStatu.DS.Api.Osoba.ListItem
            {
                Person_Id = o.NameId,
                Name = o.Jmeno,
                Surname = o.Prijmeni,
                Year_Of_Birth = o.Narozeni.HasValue ? o.Narozeni.Value.Year.ToString() : null,
                Political_Involvement = o.StatusOsoby().ToNiceDisplayName(),
                Photo_Url = o.HasPhoto() ? o.GetPhotoUrl(false) : null,
                Current_Political_Party = o.CurrentPoliticalParty() ?? "None",
                Have_More_Details = true,
                Involved_In_Companies_Count = o.AktualniVazby(DS.Graphs.Relation.AktualnostType.Nedavny)
                        .Where(v => v.To != null && v.To.Type == HlidacStatu.DS.Graphs.Graph.Node.NodeType.Company)
                        .GroupBy(f => f.To.Id, v => v, (ico, v) => new
                        {
                            ICO = ico,
                            FirmaName = v.First().To.PrintName(),//HlidacStatu.Lib.Data.External.FirmyDB.NameFromIco(ico, true),
                        }).Count(),
            }).ToArray();

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