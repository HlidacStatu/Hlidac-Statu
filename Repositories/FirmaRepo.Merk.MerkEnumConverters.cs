using Amazon.Runtime.Internal.Transform;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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
                private static readonly ILogger _logger = Log.ForContext(typeof(MerkEnumConverters));

                private static Devmasters.Cache.AWS_S3.AutoUpdatebleCache<string> _merkEnumsCache =
                   new Devmasters.Cache.AWS_S3.AutoUpdatebleCache<string>(
                       new string[] { Devmasters.Config.GetWebConfigValue("Minio.Cache.Endpoint") }, Devmasters.Config.GetWebConfigValue("Minio.Cache.Bucket"), Devmasters.Config.GetWebConfigValue("Minio.Cache.AccessKey"), Devmasters.Config.GetWebConfigValue("Minio.Cache.SecretKey"),
                   TimeSpan.FromDays(20), "merk.enums.json", (obj) =>
                   {
                   //curl -X GET -H 'Authorization: Token merk-api-token' 'https://api.merk.cz//enums/?country_code=cz'
                   string s = "";
                       try
                       {
                           Dictionary<string, string> headers = new();
                           headers.Add("Authorization", "Token " + Devmasters.Config.GetWebConfigValue("MerkApiToken"));

                           s = Devmasters.Net.HttpClient.Simple.GetAsync("https://api.merk.cz//enums/?country_code=cz", headers: headers)
                               .ConfigureAwait(false).GetAwaiter().GetResult();
                       }
                       catch (Exception e)
                       {
                           try
                           {
                               _logger.Error(e, "Error loading Merk enums from API");
                               s = Devmasters.Net.HttpClient.Simple.GetAsync("https://somedata.hlidacstatu.cz/appdata/merk.enums.json").Result;
                           }
                           catch (Exception ex)
                           {
                               _logger.Error(e, "Error loading Merk enums from somedata.hlidacstatu.cz");
                           }
                       }
                       return s;
                   }, null);

                // Main loader class with conversion methods
                private static CzechEnumsData _data;

                static MerkEnumConverters()
                {
                    _data = new CzechEnumsData(); //falback to empty data if not loaded
                    try
                    {
                        _data = JsonConvert.DeserializeObject<CzechEnumsData>(_merkEnumsCache.Get());

                    }
                    catch (Exception e)
                    {
                    }
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
                    return _data.CountryCodes?.TryGetValue(key, out var value) == true ? value : null;
                }

                public static FinancialStatementItem ConvertCompanyFinancialStatement(string key)
                {
                    return _data.CompanyFinancialStatements?.TryGetValue(key, out var value) == true ? value : null;
                }

                public static RangeEnumItem ConvertCompanyMagnitude(string key)
                {
                    return _data.CompanyMagnitude?.TryGetValue(key, out var value) == true ? value : null;
                }

                public static BasicEnumItem ConvertCompanyOwningType(string key)
                {
                    return _data.CompanyOwningType?.TryGetValue(key, out var value) == true ? value : null;
                }

                public static BasicEnumItem ConvertDistrict(string key)
                {
                    return _data.District?.TryGetValue(key, out var value) == true ? value : null;
                }

                public static BasicEnumItem ConvertCompanyLicenseType(string key)
                {
                    return _data.CompanyLicenseType?.TryGetValue(key, out var value) == true ? value : null;
                }

                public static RegionItem ConvertRegion(string key)
                {
                    return _data.Region?.TryGetValue(key, out var value) == true ? value : null;
                }

                public static BasicEnumItem ConvertCompanyInsolvencyType(string key)
                {
                    return _data.CompanyInsolvencyType?.TryGetValue(key, out var value) == true ? value : null;
                }

                public static BasicEnumItem ConvertCompanyEvents(string key)
                {
                    return _data.CompanyEvents?.TryGetValue(key, out var value) == true ? value : null;
                }

                public static BasicEnumItem ConvertCompanyInsolvencyStatus(string key)
                {
                    return _data.CompanyInsolvencyStatuses?.TryGetValue(key, out var value) == true ? value : null;
                }

                public static BasicEnumItem ConvertCompanyStatus(string key)
                {
                    return _data.CompanyStatus?.TryGetValue(key, out var value) == true ? value : null;
                }

                public static BasicEnumItem ConvertCompanyBusinessPremisesType(string key)
                {
                    return _data.CompanyBusinessPremisesTypes?.TryGetValue(key, out var value) == true ? value : null;
                }

                public static BasicEnumItem ConvertCompanyEsiStatus(string key)
                {
                    return _data.CompanyEsiStatus?.TryGetValue(key, out var value) == true ? value : null;
                }

                public static BasicEnumItem ConvertCompanyEventsAction(string key)
                {
                    return _data.CompanyEventsActions?.TryGetValue(key, out var value) == true ? value : null;
                }

                public static BasicEnumItem ConvertCompanyLicenseStatus(string key)
                {
                    return _data.CompanyLicenseStatus?.TryGetValue(key, out var value) == true ? value : null;
                }

                public static BasicEnumItem ConvertBank(string key)
                {
                    return _data.Banks?.TryGetValue(key, out var value) == true ? value : null;
                }

                public static BasicEnumItem ConvertCompanyRole(string key)
                {
                    return _data.CompanyRole?.TryGetValue(key, out var value) == true ? value : null;
                }

                public static IndustryItem ConvertCompanyIndustry(string key)
                {
                    return _data.CompanyIndustry?.TryGetValue(key, out var value) == true ? value : null;
                }

                public static BasicEnumItem ConvertCompanyCourt(string key)
                {
                    return _data.CompanyCourt?.TryGetValue(key, out var value) == true ? value : null;
                }

                public static BasicEnumItem ConvertCompanyLegalForm(string key)
                {
                    return _data.CompanyLegalForm?.TryGetValue(key, out var value) == true ? value : null;
                }

                public static RangeEnumItem ConvertCompanyTurnover(string key)
                {
                    return _data.CompanyTurnover?.TryGetValue(key, out var value) == true ? value : null;
                }

                // Utility methods to get all keys for a category
                public static IEnumerable<string> GetCountryCodeKeys() => _data.CountryCodes?.Keys?.ToArray() ?? Array.Empty<string>();
                public static IEnumerable<string> GetCompanyFinancialStatementKeys() => _data.CompanyFinancialStatements?.Keys?.ToArray() ?? Array.Empty<string>();
                public static IEnumerable<string> GetCompanyMagnitudeKeys() => _data.CompanyMagnitude?.Keys?.ToArray() ?? Array.Empty<string>();
                public static IEnumerable<string> GetCompanyOwningTypeKeys() => _data.CompanyOwningType?.Keys?.ToArray() ?? Array.Empty<string>();
                public static IEnumerable<string> GetDistrictKeys() => _data.District?.Keys?.ToArray() ?? Array.Empty<string>();
                public static IEnumerable<string> GetCompanyLicenseTypeKeys() => _data.CompanyLicenseType?.Keys?.ToArray() ?? Array.Empty<string>();
                public static IEnumerable<string> GetRegionKeys() => _data.Region?.Keys?.ToArray() ?? Array.Empty<string>();
                public static IEnumerable<string> GetCompanyInsolvencyTypeKeys() => _data.CompanyInsolvencyType?.Keys?.ToArray() ?? Array.Empty<string>();
                public static IEnumerable<string> GetCompanyEventsKeys() => _data.CompanyEvents?.Keys?.ToArray() ?? Array.Empty<string>();
                public static IEnumerable<string> GetCompanyInsolvencyStatusKeys() => _data.CompanyInsolvencyStatuses?.Keys?.ToArray() ?? Array.Empty<string>();
                public static IEnumerable<string> GetCompanyStatusKeys() => _data.CompanyStatus?.Keys?.ToArray() ?? Array.Empty<string>();
                public static IEnumerable<string> GetCompanyBusinessPremisesTypeKeys() => _data.CompanyBusinessPremisesTypes?.Keys?.ToArray() ?? Array.Empty<string>();
                public static IEnumerable<string> GetCompanyEsiStatusKeys() => _data.CompanyEsiStatus?.Keys?.ToArray() ?? Array.Empty<string>();
                public static IEnumerable<string> GetCompanyEventsActionKeys() => _data.CompanyEventsActions?.Keys?.ToArray() ?? Array.Empty<string>();
                public static IEnumerable<string> GetCompanyLicenseStatusKeys() => _data.CompanyLicenseStatus?.Keys?.ToArray() ?? Array.Empty<string>();
                public static IEnumerable<string> GetBankKeys() => _data.Banks?.Keys?.ToArray() ?? Array.Empty<string>();
                public static IEnumerable<string> GetCompanyRoleKeys() => _data.CompanyRole?.Keys?.ToArray() ?? Array.Empty<string>();
                public static IEnumerable<string> GetCompanyIndustryKeys() => _data.CompanyIndustry?.Keys?.ToArray() ?? Array.Empty<string>();
                public static IEnumerable<string> GetCompanyCourtKeys() => _data.CompanyCourt?.Keys?.ToArray() ?? Array.Empty<string>();
                public static IEnumerable<string> GetCompanyLegalFormKeys() => _data.CompanyLegalForm?.Keys?.ToArray() ?? Array.Empty<string>();
                public static IEnumerable<string> GetCompanyTurnoverKeys() => _data.CompanyTurnover?.Keys?.ToArray() ?? Array.Empty<string>();
            }
        }


    }
}
