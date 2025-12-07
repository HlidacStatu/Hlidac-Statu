using Amazon.Runtime.Internal.Transform;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Text;
using HlidacStatu.Repositories.Cache;
using static HlidacStatu.Entities.Osoba;

namespace HlidacStatu.Repositories
{
    public static partial class FirmaRepo
    {
        public partial class Merk
        {
            /*
             How to create this code
            1. curl -X GET -H 'Authorization: Token merk-api-token' 'https://api.merk.cz//enums/?country_code=cz' > merk.enums.json
            2. use this prompt in Claude (split json into smaller parts if needed):
               "Analyze attached JSON.
                In JSON , first level is list of categories.
                Create C# class, which loads this JSON into memory and contains c# convert function for every category.
                C# functions must convert keys to corresponding values for every category.
                Keep lookup keys as string, don't convert it to integer or other type."
             */

            public class MerkEnumConverters
            {
                private static string LoadMerkEnums()
                {
                    //curl -X GET -H 'Authorization: Token merk-api-token' 'https://api.merk.cz//enums/?country_code=cz'
                    string s = "";
                    try
                    {
                        Dictionary<string, string> headers = new();
                        headers.Add("Authorization", "Token " + Devmasters.Config.GetWebConfigValue("MerkApiToken"));
                        s = Devmasters.Net.HttpClient.Simple.Get(
                            "https://api.merk.cz/enums/?country_code=cz",
                            headers: headers);
                    }
                    catch (Exception e)
                    {
                        try
                        {
                            _logger.Error(e, "Error loading Merk enums from API");
                            s = Devmasters.Net.HttpClient.Simple.Get(
                                "https://somedata.hlidacstatu.cz/appdata/merk.enums.json");
                        }
                        catch (Exception ex)
                        {
                            _logger.Error(ex, "Error loading Merk enums from somedata.hlidacstatu.cz");
                        }
                    }

                    return s;
                }

                static object lockObj = new object();
                static CzechEnumsData _czechEnumsData = null;
                // Main loader class with conversion methods
                private static CzechEnumsData czechEnumsData
                {
                    get
                    {
                        if (_czechEnumsData == null)
                        {
                            lock (lockObj)
                            {
                                if (_czechEnumsData == null)
                                {
                                    _czechEnumsData = JsonConvert.DeserializeObject<CzechEnumsData>(LoadMerkEnums());
                                }
                            }
                        }
                        return _czechEnumsData;
                    }
                }

                static MerkEnumConverters()
                {

                }

                // Base class for basic enum items
                public class BasicEnumItem
                {
                    [JsonProperty("code")]
                    public object Code { get; set; }

                    [JsonProperty("text")]
                    public string Text { get; set; }

                    [JsonProperty("short_text")]
                    public string ShortText { get; set; }
                }

                // For financial statements with additional properties
                public class FinancialStatementItem : BasicEnumItem
                {
                    [JsonProperty("label")]
                    public object Label { get; set; }

                    [JsonProperty("level")]
                    public int Level { get; set; }

                    [JsonProperty("section")]
                    public string Section { get; set; }
                }

                // For magnitude and turnover with range properties
                public class RangeEnumItem : BasicEnumItem
                {
                    [JsonProperty("pretty")]
                    public string Pretty { get; set; }

                    [JsonProperty("lower_bound")]
                    public string LowerBound { get; set; }

                    [JsonProperty("upper_bound")]
                    public string UpperBound { get; set; }

                    [JsonProperty("lower_bound_val")]
                    public int? LowerBoundVal { get; set; }

                    [JsonProperty("upper_bound_val")]
                    public int? UpperBoundVal { get; set; }
                }

                // For regions with inflected property
                public class RegionItem : BasicEnumItem
                {
                    [JsonProperty("inflected")]
                    public string Inflected { get; set; }
                }

                // For industry with additional business properties
                public class IndustryItem : BasicEnumItem
                {
                    [JsonProperty("parent")]
                    public string Parent { get; set; }

                    [JsonProperty("is_b2b")]
                    public bool IsB2B { get; set; }

                    [JsonProperty("is_b2c")]
                    public bool IsB2C { get; set; }
                }

                // Main data container
                public class CzechEnumsData
                {
                    [JsonProperty("country_codes")]
                    public Dictionary<string, BasicEnumItem> CountryCodes { get; set; }

                    [JsonProperty("company_financial_statements")]
                    public Dictionary<string, FinancialStatementItem> CompanyFinancialStatements { get; set; }

                    [JsonProperty("company_magnitude")]
                    public Dictionary<string, RangeEnumItem> CompanyMagnitude { get; set; }

                    [JsonProperty("company_owning_type")]
                    public Dictionary<string, BasicEnumItem> CompanyOwningType { get; set; }

                    [JsonProperty("district")]
                    public Dictionary<string, BasicEnumItem> District { get; set; }

                    [JsonProperty("company_license_type")]
                    public Dictionary<string, BasicEnumItem> CompanyLicenseType { get; set; }

                    [JsonProperty("region")]
                    public Dictionary<string, RegionItem> Region { get; set; }

                    [JsonProperty("company_insolvency_type")]
                    public Dictionary<string, BasicEnumItem> CompanyInsolvencyType { get; set; }

                    [JsonProperty("company_events")]
                    public Dictionary<string, BasicEnumItem> CompanyEvents { get; set; }

                    [JsonProperty("company_insolvency_statuses")]
                    public Dictionary<string, BasicEnumItem> CompanyInsolvencyStatuses { get; set; }

                    [JsonProperty("company_status")]
                    public Dictionary<string, BasicEnumItem> CompanyStatus { get; set; }

                    [JsonProperty("company_business_premises_types")]
                    public Dictionary<string, BasicEnumItem> CompanyBusinessPremisesTypes { get; set; }

                    [JsonProperty("company_esi_status")]
                    public Dictionary<string, BasicEnumItem> CompanyEsiStatus { get; set; }

                    [JsonProperty("company_events_actions")]
                    public Dictionary<string, BasicEnumItem> CompanyEventsActions { get; set; }

                    [JsonProperty("company_license_status")]
                    public Dictionary<string, BasicEnumItem> CompanyLicenseStatus { get; set; }

                    [JsonProperty("banks")]
                    public Dictionary<string, BasicEnumItem> Banks { get; set; }

                    [JsonProperty("company_role")]
                    public Dictionary<string, BasicEnumItem> CompanyRole { get; set; }

                    [JsonProperty("company_industry")]
                    public Dictionary<string, IndustryItem> CompanyIndustry { get; set; }

                    [JsonProperty("company_court")]
                    public Dictionary<string, BasicEnumItem> CompanyCourt { get; set; }

                    [JsonProperty("company_legal_form")]
                    public Dictionary<string, BasicEnumItem> CompanyLegalForm { get; set; }

                    [JsonProperty("company_turnover")]
                    public Dictionary<string, RangeEnumItem> CompanyTurnover { get; set; }
                }


                // Conversion methods for each category
                public static BasicEnumItem ConvertCountryCode(string key)
                {
                    if (string.IsNullOrWhiteSpace(key))
                        return null;

                    return czechEnumsData.CountryCodes?.TryGetValue(key, out var value) == true ? value : null;
                }

                public static FinancialStatementItem ConvertCompanyFinancialStatement(string key)
                {
                    if (string.IsNullOrWhiteSpace(key))
                        return null;

                    return czechEnumsData.CompanyFinancialStatements?.TryGetValue(key, out var value) == true ? value : null;
                }

                public static RangeEnumItem ConvertCompanyMagnitude(string key)
                {
                    if (string.IsNullOrWhiteSpace(key))
                        return null;

                    return czechEnumsData.CompanyMagnitude?.TryGetValue(key, out var value) == true ? value : null;
                }

                public static BasicEnumItem ConvertCompanyOwningType(string key)
                {
                    if (string.IsNullOrWhiteSpace(key))
                        return null;

                    return czechEnumsData.CompanyOwningType?.TryGetValue(key, out var value) == true ? value : null;
                }

                public static BasicEnumItem ConvertDistrict(string key)
                {
                    if (string.IsNullOrWhiteSpace(key))
                        return null;

                    return czechEnumsData.District?.TryGetValue(key, out var value) == true ? value : null;
                }

                public static BasicEnumItem ConvertCompanyLicenseType(string key)
                {
                    if (string.IsNullOrWhiteSpace(key))
                        return null;

                    return czechEnumsData.CompanyLicenseType?.TryGetValue(key, out var value) == true ? value : null;
                }

                public static RegionItem ConvertRegion(string key)
                {
                    if (string.IsNullOrWhiteSpace(key))
                        return null;

                    return czechEnumsData.Region?.TryGetValue(key, out var value) == true ? value : null;
                }

                public static BasicEnumItem ConvertCompanyInsolvencyType(string key)
                {
                    if (string.IsNullOrWhiteSpace(key))
                        return null;

                    return czechEnumsData.CompanyInsolvencyType?.TryGetValue(key, out var value) == true ? value : null;
                }

                public static BasicEnumItem ConvertCompanyEvents(string key)
                {
                    if (string.IsNullOrWhiteSpace(key))
                        return null;

                    return czechEnumsData.CompanyEvents?.TryGetValue(key, out var value) == true ? value : null;
                }

                public static BasicEnumItem ConvertCompanyInsolvencyStatus(string key)
                {
                    if (string.IsNullOrWhiteSpace(key))
                        return null;

                    return czechEnumsData.CompanyInsolvencyStatuses?.TryGetValue(key, out var value) == true ? value : null;
                }

                public static BasicEnumItem ConvertCompanyStatus(string key)
                {
                    if (string.IsNullOrWhiteSpace(key))
                        return null;

                    return czechEnumsData.CompanyStatus?.TryGetValue(key, out var value) == true ? value : null;
                }

                public static BasicEnumItem ConvertCompanyBusinessPremisesType(string key)
                {
                    if (string.IsNullOrWhiteSpace(key))
                        return null;

                    return czechEnumsData.CompanyBusinessPremisesTypes?.TryGetValue(key, out var value) == true ? value : null;
                }

                public static BasicEnumItem ConvertCompanyEsiStatus(string key)
                {
                    if (string.IsNullOrWhiteSpace(key))
                        return null;

                    return czechEnumsData.CompanyEsiStatus?.TryGetValue(key, out var value) == true ? value : null;
                }

                public static BasicEnumItem ConvertCompanyEventsAction(string key)
                {
                    if (string.IsNullOrWhiteSpace(key))
                        return null;

                    return czechEnumsData.CompanyEventsActions?.TryGetValue(key, out var value) == true ? value : null;
                }

                public static BasicEnumItem ConvertCompanyLicenseStatus(string key)
                {
                    if (string.IsNullOrWhiteSpace(key))
                        return null;

                    return czechEnumsData.CompanyLicenseStatus?.TryGetValue(key, out var value) == true ? value : null;
                }

                public static BasicEnumItem ConvertBank(string key)
                {
                    if (string.IsNullOrWhiteSpace(key))
                        return null;

                    return czechEnumsData.Banks?.TryGetValue(key, out var value) == true ? value : null;
                }

                public static BasicEnumItem ConvertCompanyRole(string key)
                {
                    if (string.IsNullOrWhiteSpace(key))
                        return null;

                    return czechEnumsData.CompanyRole?.TryGetValue(key, out var value) == true ? value : null;
                }

                public static IndustryItem ConvertCompanyIndustry(string key)
                {
                    if (string.IsNullOrWhiteSpace(key))
                        return null;

                    return czechEnumsData.CompanyIndustry?.TryGetValue(key, out var value) == true ? value : null;
                }

                public static string ConvertCompanyIndustryToFullName(string key, bool wholePath, bool includedB2B_B2C,
                    string delimiter = " - ")
                {
                    if (string.IsNullOrWhiteSpace(key))
                        return null;

                    StringBuilder sb = new StringBuilder();
                    var industry = ConvertCompanyIndustry(key);
                    bool first = true;
                    while (industry != null)
                    {
                        var item = industry.Text;
                        if (includedB2B_B2C && (industry.IsB2B || industry.IsB2C))
                        {
                            if (industry.IsB2B && industry.IsB2C)
                                item += "(B2B, B2C)";
                            else if (industry.IsB2B)
                                item += " (B2B)";
                            else
                                item += " (B2C)";
                        }

                        if (wholePath == false)
                            break;

                        if (!string.IsNullOrWhiteSpace(industry.Parent))
                        {
                            industry = ConvertCompanyIndustry(industry.Parent);
                        }
                        else
                        {
                            industry = null;
                            if (first)
                                sb.Insert(0, item);
                            else
                                sb.Insert(0, item + delimiter);
                        }

                        first = false;
                    }

                    return sb.ToString();
                }

                public static BasicEnumItem ConvertCompanyCourt(string key)
                {
                    if (string.IsNullOrWhiteSpace(key))
                        return null;

                    return czechEnumsData.CompanyCourt?.TryGetValue(key, out var value) == true ? value : null;
                }

                public static BasicEnumItem ConvertCompanyLegalForm(string key)
                {
                    if (string.IsNullOrWhiteSpace(key))
                        return null;
                    return czechEnumsData.CompanyLegalForm?.TryGetValue(key, out var value) == true ? value : null;
                }

                public static RangeEnumItem ConvertCompanyTurnover(string key)
                {
                    if (string.IsNullOrWhiteSpace(key))
                        return null;
                    return czechEnumsData.CompanyTurnover?.TryGetValue(key, out var value) == true ? value : null;
                }

                // Utility methods to get all keys for a category
                public static IEnumerable<string> GetCountryCodeKeys() =>
                    czechEnumsData.CountryCodes?.Keys?.ToArray() ?? Array.Empty<string>();

                public static IEnumerable<string> GetCompanyFinancialStatementKeys() =>
                    czechEnumsData.CompanyFinancialStatements?.Keys?.ToArray() ?? Array.Empty<string>();

                public static IEnumerable<string> GetCompanyMagnitudeKeys() =>
                    czechEnumsData.CompanyMagnitude?.Keys?.ToArray() ?? Array.Empty<string>();

                public static IEnumerable<string> GetCompanyOwningTypeKeys() =>
                    czechEnumsData.CompanyOwningType?.Keys?.ToArray() ?? Array.Empty<string>();

                public static IEnumerable<string> GetDistrictKeys() =>
                    czechEnumsData.District?.Keys?.ToArray() ?? Array.Empty<string>();

                public static IEnumerable<string> GetCompanyLicenseTypeKeys() =>
                    czechEnumsData.CompanyLicenseType?.Keys?.ToArray() ?? Array.Empty<string>();

                public static IEnumerable<string> GetRegionKeys() =>
                    czechEnumsData.Region?.Keys?.ToArray() ?? Array.Empty<string>();

                public static IEnumerable<string> GetCompanyInsolvencyTypeKeys() =>
                    czechEnumsData.CompanyInsolvencyType?.Keys?.ToArray() ?? Array.Empty<string>();

                public static IEnumerable<string> GetCompanyEventsKeys() =>
                    czechEnumsData.CompanyEvents?.Keys?.ToArray() ?? Array.Empty<string>();

                public static IEnumerable<string> GetCompanyInsolvencyStatusKeys() =>
                    czechEnumsData.CompanyInsolvencyStatuses?.Keys?.ToArray() ?? Array.Empty<string>();

                public static IEnumerable<string> GetCompanyStatusKeys() =>
                    czechEnumsData.CompanyStatus?.Keys?.ToArray() ?? Array.Empty<string>();

                public static IEnumerable<string> GetCompanyBusinessPremisesTypeKeys() =>
                    czechEnumsData.CompanyBusinessPremisesTypes?.Keys?.ToArray() ?? Array.Empty<string>();

                public static IEnumerable<string> GetCompanyEsiStatusKeys() =>
                    czechEnumsData.CompanyEsiStatus?.Keys?.ToArray() ?? Array.Empty<string>();

                public static IEnumerable<string> GetCompanyEventsActionKeys() =>
                    czechEnumsData.CompanyEventsActions?.Keys?.ToArray() ?? Array.Empty<string>();

                public static IEnumerable<string> GetCompanyLicenseStatusKeys() =>
                    czechEnumsData.CompanyLicenseStatus?.Keys?.ToArray() ?? Array.Empty<string>();

                public static IEnumerable<string> GetBankKeys() =>
                    czechEnumsData.Banks?.Keys?.ToArray() ?? Array.Empty<string>();

                public static IEnumerable<string> GetCompanyRoleKeys() =>
                    czechEnumsData.CompanyRole?.Keys?.ToArray() ?? Array.Empty<string>();

                public static IEnumerable<string> GetCompanyIndustryKeys() =>
                    czechEnumsData.CompanyIndustry?.Keys?.ToArray() ?? Array.Empty<string>();

                public static IEnumerable<string> GetCompanyCourtKeys() =>
                    czechEnumsData.CompanyCourt?.Keys?.ToArray() ?? Array.Empty<string>();

                public static IEnumerable<string> GetCompanyLegalFormKeys() =>
                    czechEnumsData.CompanyLegalForm?.Keys?.ToArray() ?? Array.Empty<string>();

                public static IEnumerable<string> GetCompanyTurnoverKeys() =>
                    czechEnumsData.CompanyTurnover?.Keys?.ToArray() ?? Array.Empty<string>();
            }
        }
    }
}