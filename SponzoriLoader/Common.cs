using System;
using System.Collections.Generic;
using System.Linq;
using HlidacStatu.Entities;
using HlidacStatu.Extensions;
using HlidacStatu.Repositories;

namespace SponzoriLoader;

public static class Common
{
    /// <summary>
        /// Uploads new donations to Sponzoring table
        /// </summary>
        public static void UploadPeopleDonations(Donations donations, string user, string zdroj)
        {
            foreach (var personDonations in donations.GetDonations())
            {
                var donor = personDonations.Key;

                Osoba osoba = OsobaRepo.GetOrCreateNew(donor.TitleBefore, donor.Name, donor.Surname, donor.TitleAfter,
                                                   donor.DateOfBirth, Osoba.StatusOsobyEnum.Sponzor, user);

                // Výjimka pro Radek Jonke 24.12.1970
                if (osoba.Jmeno == "Radek"
                    && osoba.Prijmeni == "Jonke"
                    && osoba.Narozeni != null
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

                    osoba.AddSponsoring(sponzoring, user);
                }

            }
        }

        /// <summary>
        /// Uploads new donations to FirmaEvent table
        /// </summary>
        public static void UploadCompanyDonations(Donations donations, string user, string zdroj)
        {
            foreach (var companyDonations in donations.GetDonations())
            {
                var donor = companyDonations.Key;

                Firma firma = null;
                try
                {
                    firma = FirmaRepo.FromIco(donor.CompanyId);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

                if (firma is null)
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
                    firma.AddSponsoring(sponzoring, user);
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
}