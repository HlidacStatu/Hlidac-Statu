using HlidacStatu.Entities;
using HlidacStatu.Util;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HlidacStatu.LibCore.Extensions;
using Microsoft.Extensions.Configuration;

namespace SponzoriLoader
{

    public class Program
    {
        private static readonly HttpClient _client = new HttpClient();

        private static readonly string[] _addresses = new string[]
        {
            // "https://zpravy.udhpsh.cz/export/vfz2017-index.json",
            // "https://zpravy.udhpsh.cz/zpravy/vfz2018.json",
            // "https://zpravy.udhpsh.cz/zpravy/vfz2019.json",
            // "https://zpravy.udhpsh.cz/zpravy/vfz2020.json",
            // "https://zpravy.udhpsh.cz/zpravy/vfz2021.json",
            // "https://zpravy.udhpsh.cz/zpravy/vfz2022.json",
            // "https://zpravy.udhpsh.cz/zpravy/vfz2023.json"
            "https://zpravy.udhpsh.cz/zpravy/vfz2024.json"
        };
        
        private static void LoadDataFromFiles(Donations peopleDonations, Donations companyDonations)
        {
            LoadSponzoringFromFile.LoadOsoby(peopleDonations, @"/Users/suchoss/Downloads/06070108-PRAHA-SOBE-2024 - Lidi.tsv",
                "06070108", _user, _zdroj);
            LoadSponzoringFromFile.LoadFirmy(companyDonations, @"/Users/suchoss/Downloads/06070108-PRAHA-SOBE-2024 - Lidi.tsv",
                "04134940", _user, _zdroj);
        }

        private static readonly string _user = "sponzorLoader";
        private static readonly string _zdroj = "https://www.udhpsh.cz/vyrocni-financni-zpravy-stran-a-hnuti";

        private static Dictionary<string, string> _partyNames;

        static async Task Main(string[] args)
        {
            //Before running
            //- update source
            //- select true or false below
            bool loadDataFromFiles = true;
            
            IConfiguration configuration = HlidacConfigExtensions.InitializeConsoleConfiguration(args);
            Devmasters.Config.Init(configuration);
            CultureInfo.DefaultThreadCurrentCulture = Consts.czCulture;
            CultureInfo.DefaultThreadCurrentUICulture = Consts.csCulture;
            var peopleDonations = new Donations(new DonorEqualityComparer());
            var companyDonations = new Donations(new DonorEqualityComparer());
            _partyNames = Common.LoadPartyNames();
            

            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (loadDataFromFiles)
            {
                LoadDataFromFiles(peopleDonations, companyDonations);
            }
            else
            {
                await LoadDataFromWebAsync(peopleDonations, companyDonations);
            }

            //Save to DB
            Common.UploadPeopleDonations(peopleDonations, _user, _zdroj);
            Common.UploadCompanyDonations(companyDonations, _user, _zdroj);

            //await FixPeopleSponzorsAsync(); //Moved to downloader for regular runs
        }

        

        private static async Task LoadDataFromWebAsync(Donations peopleDonations, Donations companyDonations)
        {
            foreach (string indexUrl in _addresses)
            {
                var index = await LoadIndexAsync(indexUrl);
                string key = index.election.key;
                int year = GetYearFromText(key);

                foreach (var party in index.parties)
                {
                    IEnumerable<dynamic> files = party.files;
                    // osoby
                    string penizeFoUrl = files.Where(f => f.subject == "penizefo").Select(f => f.url).FirstOrDefault();
                    await LoadDonationsAsync(penizeFoUrl, peopleDonations, party, year);
                    string nepenizeFoUrl = files.Where(f => f.subject == "bupfo").Select(f => f.url).FirstOrDefault();
                    await LoadDonationsAsync(nepenizeFoUrl, peopleDonations, party, year);
                    //firmy
                    string penizePoUrl = files.Where(f => f.subject == "penizepo").Select(f => f.url).FirstOrDefault();
                    await LoadDonationsAsync(penizePoUrl, companyDonations, party, year);
                    string nepenizePoUrl = files.Where(f => f.subject == "buppo").Select(f => f.url).FirstOrDefault();
                    await LoadDonationsAsync(nepenizePoUrl, companyDonations, party, year);
                }
            }
        }

        public static async Task<dynamic> LoadIndexAsync(string url)
        {
            string response = await _client.GetStringAsync(url);
            dynamic json = JsonConvert.DeserializeObject(response);
            return json;
        }

        

        public static string NormalizePartyName(string name, string ico)
        {
            if (_partyNames.TryGetValue(ico, out string normalizedName))
            {
                return normalizedName;
            }
            return ParseTools.NormalizaceStranaShortName(name);

        }

        /// <summary>
        /// Loads all people donations from web
        /// </summary>
        public static async Task LoadDonationsAsync(string url, Donations donations, dynamic party, int year)
        {
            dynamic donationRecords = await LoadIndexAsync(url);
            foreach (var record in donationRecords)
            {
                string firstName = record.firstName;
                string lastName = record.lastName;
                string titlesBefore = record.titleBefore;
                string titlesAfter = record.titleAfter;

                var cleanedName = Validators.SeparateNameFromTitles(firstName);
                var cleanedLastName = Validators.SeparateNameFromTitles(lastName);

                titlesBefore = Common.MergeTitles(titlesBefore, cleanedName.titulyPred, cleanedLastName.titulyPred);
                titlesAfter = Common.MergeTitles(titlesAfter, cleanedName.titulyPo, cleanedLastName.titulyPo);

                titlesBefore = Common.CleanTitles(titlesBefore);
                titlesAfter = Common.CleanTitles(titlesAfter);

                //jméno firmy nacpat do jména
                if (string.IsNullOrWhiteSpace(cleanedName.jmeno))
                    cleanedName.jmeno = record.company;

                Donor donor = new Donor()
                {
                    City = record.addrCity,
                    CompanyId = record.companyId,
                    Name = cleanedName.jmeno,
                    Surname = cleanedLastName.jmeno,
                    TitleBefore = titlesBefore,
                    TitleAfter = titlesAfter,
                    DateOfBirth = record.birthDate
                };
                Gift gift = new Gift()
                {
                    Amount = record.money ?? record.value,
                    ICO = party.ic,
                    Party = party.longName,
                    Description = record.description,
                    Date = record.date ?? new DateTime(year, 1, 1),
                    GiftType = (record.money is null) ? Sponzoring.TypDaru.NefinancniDar : Sponzoring.TypDaru.FinancniDar
                };

                donations.AddDonation(donor, gift);
            }
        }

        
        public static int GetYearFromText(string text)
        {
            string yearString = Regex.Match(text, @"\d+").Value;
            if (int.TryParse(yearString, out int year))
            {
                return year;
            }
            return 0;
        }

        
    }
}
