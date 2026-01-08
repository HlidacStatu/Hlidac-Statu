using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HlidacStatu.Entities;
using HlidacStatu.Extensions;
using HlidacStatu.Repositories;

namespace SponzoriLoader;

public static class Common
{
    /// <summary>
        /// Uploads new donations to Sponzoring table
        /// </summary>
        public static async Task UploadPeopleDonationsAsync(Donations donations, string user, string zdroj)
        {
            foreach (var personDonations in donations.GetDonations())
            {
                var donor = personDonations.Key;

                Osoba osoba = await OsobaRepo.GetOrCreateNewAsync(donor.TitleBefore, donor.Name, donor.Surname, donor.TitleAfter,
                                                   donor.DateOfBirth, Osoba.StatusOsobyEnum.Sponzor, user);

                // Výjimka pro Radek Jonke 24.12.1970
                if (osoba.Jmeno == "Radek"
                    && osoba is { Prijmeni: "Jonke", Narozeni: not null }
                    && osoba.Narozeni.Value.Year == 1970
                    && osoba.Narozeni.Value.Month == 12
                    && osoba.Narozeni.Value.Day == 24)
                {
                    continue;
                }

                foreach (var donation in personDonations.Value)
                {
                    // add event
                    var sponzoring = new Sponzoring()
                    {
                        DarovanoDne = donation.Date,
                        Hodnota = donation.Amount,
                        IcoPrijemce = donation.ICO,
                        Zdroj = zdroj,
                        Popis = donation.Description,
                        Typ = (int)donation.GiftType
                    };

                    try
                    {
                        await osoba.AddSponsoringAsync(sponzoring, user);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"{donor.Name} {donor.Surname} {donor.DateOfBirth}, {donation.Date} {donation.Amount} {zdroj} {donation.Description} failed");
                    }
                    
                }

            }
        }

        /// <summary>
        /// Uploads new donations to FirmaEvent table
        /// </summary>
        public static async Task UploadCompanyDonationsAsync(Donations donations, string user, string zdroj)
        {
            foreach (var companyDonations in donations.GetDonations())
            {
                var donor = companyDonations.Key;

                Firma firma = null;
                try
                {
                    firma = await FirmaRepo.FromIcoAsync(donor.CompanyId);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

                if (firma is null || firma.Valid == false)
                {
                    Console.WriteLine($"Chybějící firma v db - ICO: {donor.CompanyId}, nazev: {donor.Name}");
                    continue;
                }

                foreach (var donation in companyDonations.Value)
                {
                    // add event
                    var sponzoring = new Sponzoring()
                    {
                        DarovanoDne = donation.Date,
                        Hodnota = donation.Amount,
                        IcoPrijemce = donation.ICO,
                        Zdroj = zdroj,
                        Popis = donation.Description,
                        Typ = (int)donation.GiftType
                    };
                    try
                    {
                        await firma.AddSponsoringAsync(sponzoring, user);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"{donor.CompanyId}, {donation.Date} {donation.Amount} {zdroj} {donation.Description} failed");
                    }
                }
            }
        }
        
        public static Dictionary<string, string> LoadPartyNames()
        {
            using (DbEntities db = new DbEntities())
            {
                return db.ZkratkaStrany.ToDictionary(ks => ks.Ico, es => es.KratkyNazev);
            }
        }
        
        public static string MergeTitles(string titlesBefore, string tituly, string tituly2)
        {
            titlesBefore += " " + tituly + " " + tituly2;
            titlesBefore = Regex.Replace(titlesBefore, @"\s{2,}", " ");
            titlesBefore = titlesBefore.Trim();
            return titlesBefore;
        }
        
        public static string CleanTitles(string titles)
        {
            return titles.Replace("\"", "");
        }
}