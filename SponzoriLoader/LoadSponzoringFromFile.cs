using System;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;
using HlidacStatu.Entities;

namespace SponzoriLoader;

public static class LoadSponzoringFromFile
{
    //csv
    // prijmeni | jmeno | datum narozeni | darovano dne | hodnota daru
    public static void LoadOsoby(Donations peopleDonations, string filename, string icoStrany, string user,
        string zdroj)
    {
        var partyNames = Common.LoadPartyNames();
        
        var config = new CsvConfiguration(CultureInfo.CurrentCulture)
        {
            HasHeaderRecord = true,
            Delimiter = "\t",
        };
        using var reader = new StreamReader(filename);
        using var csv = new CsvReader(reader, config);
        
        csv.Context.RegisterClassMap<OsobaMap>();
        var records = csv.GetRecords<SponzorujiciOsoba>().ToList();
        foreach (var record in records)
        {
            var cleanedName = Validators.SeparateNameFromTitles(record.Jmeno);
            var cleanedLastName = Validators.SeparateNameFromTitles(record.Prjimeni);
            
            var titlesBefore = Common.MergeTitles("", cleanedName.titulyPred, cleanedLastName.titulyPred);
            var titlesAfter = Common.MergeTitles("", cleanedName.titulyPo, cleanedLastName.titulyPo);

            titlesBefore = Common.CleanTitles(titlesBefore);
            titlesAfter = Common.CleanTitles(titlesAfter);

            // schvalně neošetřeno. Pokud se objeví chyba, je potřeba soubor opravit
            var narozeni = DateTime.Parse(record.Narozeni, HlidacStatu.Util.Consts.czCulture);
            var darovanoDne = DateTime.Parse(record.DarovanoDne, HlidacStatu.Util.Consts.czCulture);
            var amount = Decimal.Parse(record.Hodnota);
            
            var popis = record.Popis?.Trim();
            var giftType = string.IsNullOrWhiteSpace(popis)
                ? Sponzoring.TypDaru.FinancniDar
                : Sponzoring.TypDaru.NefinancniDar;
            
            Donor donor = new Donor()
            {
                Name = cleanedName.jmeno,
                Surname = cleanedLastName.jmeno,
                DateOfBirth = narozeni,
                TitleAfter = titlesAfter,
                TitleBefore = titlesBefore
                
            };
            Gift gift = new Gift()
            {
                Amount = amount,
                ICO = icoStrany,
                Party = partyNames[icoStrany],
                Date = darovanoDne,
                Description = popis,
                GiftType = giftType
            };
            peopleDonations.AddDonation(donor, gift);
        }

    }
    
    public static void LoadFirmy(Donations companyDonations, string filename, string icoStrany, string user, string zdroj)
    {
        var partyNames = Common.LoadPartyNames();
        
        var config = new CsvConfiguration(CultureInfo.CurrentCulture)
        {
            HasHeaderRecord = true,
            Delimiter = "\t",
        };
        using var reader = new StreamReader(filename);
        using var csv = new CsvReader(reader, config);
        
        csv.Context.RegisterClassMap<FirmaMap>();
        var records = csv.GetRecords<SponzorujiciFirma>().ToList();
        foreach (var record in records)
        {
            var darovanoDne = DateTime.Parse(record.DarovanoDne, HlidacStatu.Util.Consts.czCulture);
            var amount = Decimal.Parse(record.Hodnota);
            
            var popis = record.Popis?.Trim();
            var giftType = string.IsNullOrWhiteSpace(popis)
                ? Sponzoring.TypDaru.FinancniDar
                : Sponzoring.TypDaru.NefinancniDar;
            
            Donor donor = new Donor()
            {
                CompanyId = record.Ico
            };
            Gift gift = new Gift()
            {
                Amount = amount,
                ICO = icoStrany,
                Party = partyNames[icoStrany],
                Date = darovanoDne,
                Description = popis,
                GiftType = giftType
            };
            companyDonations.AddDonation(donor, gift);
        }
    }
}

public class SponzorujiciOsoba
{
    public string Prjimeni { get; init; }
    public string Jmeno { get; init; }
    public string Narozeni { get; init; }
    public string DarovanoDne { get; init; }
    public string Hodnota { get; init; }
    public string Popis { get; init; }
}

public class SponzorujiciFirma
{
    public string Nazev { get; init; }
    public string Ico { get; init; }
    public string DarovanoDne { get; init; }
    public string Hodnota { get; init; }
    public string Popis { get; init; }
}

public sealed class OsobaMap : ClassMap<SponzorujiciOsoba>
{
    public OsobaMap()
    {
        Map(m => m.Prjimeni).Index(0);
        Map(m => m.Jmeno).Index(1);
        Map(m => m.Narozeni).Index(2);
        Map(m => m.DarovanoDne).Index(3);
        Map(m => m.Hodnota).Index(4);
        Map(m => m.Popis).Name("Popis").Optional();
    }
}

public sealed class FirmaMap : ClassMap<SponzorujiciFirma>
{
    public FirmaMap()
    {
        Map(m => m.Nazev).Index(0);
        Map(m => m.Ico).Index(1);
        Map(m => m.DarovanoDne).Index(2);
        Map(m => m.Hodnota).Index(3);
        Map(m => m.Popis).Name("Popis").Optional();
    }
}