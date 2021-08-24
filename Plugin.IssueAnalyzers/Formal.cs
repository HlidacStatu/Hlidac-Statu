using HlidacStatu.Lib;
using HlidacStatu.Entities.Issues;
using System;
using System.Collections.Generic;
using System.Linq;
using HlidacStatu.Entities;
using HlidacStatu.Extensions;
using HlidacStatu.Repositories;

namespace HlidacStatu.Plugin.IssueAnalyzers
{
    public class Formal : IIssueAnalyzer
    {


        public string Description
        {
            get
            {
                return "Overeni spravnosti formalnich udaje";
            }

        }

        public string Name
        {
            get
            {
                return "Formality";
            }

        }

        System.Text.RegularExpressions.RegexOptions regexOptions =
            System.Text.RegularExpressions.RegexOptions.CultureInvariant |
            System.Text.RegularExpressions.RegexOptions.IgnorePatternWhitespace |
            System.Text.RegularExpressions.RegexOptions.IgnoreCase |
            System.Text.RegularExpressions.RegexOptions.Multiline;
        public IEnumerable<Issue> FindIssues(Smlouva item)
        {
            List<Issue> issues = new List<Issue>();


            if (item.Prijemce == null || item.Prijemce.Count() == 0)
            {
                issues.Add(new Issue(this, (int)IssueType.IssueTypes.Neuveden_dodavatel, "Neuveden dodavatel", "Žádný dodavatel u smlouvy"));
            }
            //datumy
            if (item.datumUzavreni > item.casZverejneni)
            {
                issues.Add(new Issue(this, (int)IssueType.IssueTypes.Budouci_datum_uzavreni, "Budoucí datum uzavření", "Smlouva byla uzavřena až po zveřejnění v rejstříku."));
            }

            if (item.datumUzavreni < DateTime.Now.AddYears(-40))
            {
                issues.Add(new Issue(this, (int)IssueType.IssueTypes.Neplatny_datum_uzavreni_smlouvy, "Neplatný datum uzavření smlouvy", "Údaj o datumu uzavření smlouvy obsahuje nesmyslný datum. Zápis nesplňuje zákonou podmínku platnosti."));
            }

            if (item.spadaPodRS && item.datumUzavreni > DateTime.Now)
            {
                issues.Add(
                    new Issue(this, (int)IssueType.IssueTypes.Budouci_datum_uzavreni, "Budoucí datum uzavření smlouvy", "Údaj o datumu uzavření smlouvy obsahuje budoucí datum.")
                    { Permanent = false }
                    );
            }


            DateTime prvniZverejneni = item.casZverejneni;
            if (item.OtherVersions().Length > 0)
            {
                var newMin = item.OtherVersions().Min(m => m.casZverejneni);
                if (prvniZverejneni > newMin)
                    prvniZverejneni = newMin;
            }

            if (item.spadaPodRS && item.PravniRamec == Smlouva.PravniRamce.Od072017 &&
                 (prvniZverejneni - item.datumUzavreni).TotalDays > 31 &&
                 (item.datumUzavreni.AddMonths(3).AddDays(1) > prvniZverejneni)
                )
            {
                issues.Add(
                    new Issue(this, (int)IssueType.IssueTypes.SmlouvaZverejnenaPozde, "Smlouva nebyla zveřejněna do 30 dnů od podpisu", "Smlouva nebyla uveřejněna do 30 dnů od uzavření smlouvy (dle § 5 odst. 2), její účinnost je až odedne uveřejnění")
                    );
            }

            if (item.spadaPodRS && item.PravniRamec == Smlouva.PravniRamce.Od072017 &&
                 (item.datumUzavreni.AddMonths(3).AddDays(1) <= prvniZverejneni)
                )
            {
                if (
                    (item.hodnotaBezDph > 0 && item.hodnotaBezDph <= 50000)
                    || (item.hodnotaVcetneDph> 0 && item.hodnotaVcetneDph <= (50000m * 1.21m))
                    || (item.CalculatedPriceWithVATinCZK > 0 && item.CalculatedPriceWithVATinCZK <= (50000m * 1.21m) )
                 )
                {
                    issues.Add(
                    new Issue(this, (int)IssueType.IssueTypes.SmlouvaZverejnenaPozde_NemuselaBytZverejnena, 
                    "Smlouva nebyla zveřejněna do 3 měsíců od podpisu", 
                    "Smlouva nemusela být zveřejnena v registru smluv, je zveřejněna dobrovolně nad rámec zákona. Do registru smluv byla zveřejněna později než po 3 měsících, na platnost smlouvy to nemá vliv.")
                    );

                }
                else
                    issues.Add(
                    new Issue(this, (int)IssueType.IssueTypes.SmlouvaZverejnenaPozdeNezacalaPlatit, "Smlouva nebyla zveřejněna do 3 měsíců od podpisu a nikdy nezačala platit", "Smlouva nebyla uveřejněna do 3 měsíců od uzavření smlouvy (dle § 7 odst. 1) a je tak automaticky zrušena od počátku, nikdy nezačala platit.")
                    );
            }

            //platce
            CheckSubjekt(item.Platce, item, ref issues);

            //Dodavatele
            foreach (var d in item.Prijemce)
            {
                if (d == null)
                    continue;
                CheckSubjekt(d, item, ref issues);

            }


            //simple vztahy 
            // publikujici strana = jedina strana smlouvy
            if (
                item.Prijemce.Count() == 1 && item.Prijemce.Any(m => m.ico == item.Platce.ico)
                )
                issues.Add(new Issue(this, (int)IssueType.IssueTypes.Stejne_strany_smlouvy, "Stejné strany smlouvy", string.Format("Dodavatel i objednatel jsou stejní '{0}'", item.Platce.nazev)));
            else if (
                item.Prijemce.Any(m => m.ico == item.Platce.ico)
                )
            {
                issues.Add(new Issue(this, (int)IssueType.IssueTypes.Chybne_strany_smlouvy, "Chybné strany smlouvy", "Objednatel je i na straně dodavatele"));
            }



            //VkladatelDoRejstriku
            //CheckSubjekt(item.VkladatelDoRejstriku, item, ref issues);


            if (string.IsNullOrEmpty(item.predmet))
            {
                issues.Add(new Issue(this, (int)IssueType.IssueTypes.Chybi_predmet_smlouvy, "Chybí předmět smlouvy", "Metadata neobsahují povinný předmět smlouvy"));
            }

            return issues;
        }

        public void CheckSubjekt(Smlouva.Subjekt d, Smlouva item, ref List<Issue> issues)
        {

            List<Issue> tmpIss = new List<Issue>();

            bool osoba = false;
            bool isZahranicniSubjekt = false;
            string lnazev = d.nazev.ToLowerInvariant();
            isZahranicniSubjekt = (lnazev.Contains("ltd.") || lnazev.Contains("inc.") || lnazev.Contains("gmbh"));
            var zahr = new HlidacStatu.Util.DataValidators.ZahranicniAdresa(d.adresa);
            isZahranicniSubjekt = isZahranicniSubjekt || zahr.IsZahranicniAdresa();

            //zjisti, zda nejde o 340/2015
            string dnazev = Devmasters.TextUtil.RemoveDiacritics(d.nazev).Trim();

            // obchodní tajemství
            string[] obchodni_taj_regex = new string[] {
                        "340\\s* / \\s*2015",
                        "(obchodni|bankovni|) \\s* (tajemstvi)",
                        "nezverejnuje",
                        "fyzicka \\s* osoba",
                        "§ \\s* 5 \\s* odst.\\s*",
                        "vylouceno \\s* z \\s* uverejneni"

                    };
            bool obchodni_tajemstvi = false;
            foreach (var r in obchodni_taj_regex)
            {
                if (System.Text.RegularExpressions.Regex.IsMatch(dnazev, r, regexOptions))
                {
                    obchodni_tajemstvi = true;
                    break;
                }
            }


            bool hasIco = false;
            bool hasDS = false;

            if (string.IsNullOrEmpty(d.ico))
            {
                //tmpIss.Add(new Issue(this,16, "Chybí IČO", string.Format("Neuvedeno IČO u dodavatele '{0}'", d.nazev)));

            }
            else if (Util.DataValidators.CheckCZICO(d.ico) == false && zahr.IsZahranicniAdresa() == false)
                tmpIss.Add(new Issue(this, (int)IssueType.IssueTypes.Vadne_ICO, "Vadné IČO", string.Format("Subjekt '{0}' má neplatné IČO", d.nazev)));
            else
                hasIco = true;

            if (hasIco && zahr.IsZahranicniAdresa() == false)
            {
                //check ICO in registers
                Firma f = FirmaRepo.FromIco(d.ico, true);
                if (!Firma.IsValid(f))
                {
                    issues.Add(new Issue(this, (int)IssueType.IssueTypes.Neexistujici_ICO, "Neexistující IČO", string.Format("Subjekt '{0}' má neexistující IČO", d.nazev)));
                    hasIco = false;
                }

            }

            if (string.IsNullOrEmpty(d.datovaSchranka))
            {
                //tmpIss.Add(new Issue(this, "Chybí datová schránka", string.Format("Dodavatel '{0}' bez datove schránky", d.nazev)));
            }
            else
                hasDS = true;



            if (hasDS == false && hasIco == false && string.IsNullOrEmpty(d.nazev))
            {
                issues.Add(new Issue(this, (int)IssueType.IssueTypes.Zcela_Chybi_identifikace_smluvni_strany, "Chybí identifikace smluvní strany", "Smluvní strana není nijak identifikována"));
            }
            else if (hasDS == false && hasIco == false && !string.IsNullOrEmpty(d.nazev))
            {

                if (obchodni_tajemstvi)
                {
                    issues.Add(new Issue(this, (int)IssueType.IssueTypes.NeverejnyUdaj, "Identifikace smluvní strany", "Údaj není veřejný na základě § 5 odst. 6 zákona č. 340/2015 Sb., o registru smluv. Utajení smluvní strany je možné pouze v odůvodněných případech, což při této kontrole nehodnotíme."));

                }
                else if (isZahranicniSubjekt)
                {
                    issues.Add(new Issue(this, (int)IssueType.IssueTypes.Firma_Cizi_Stat, "Identifikace smluvní strany", string.Format("Smluvní strana '{0}' bez datove schránky a platného IČO. Jedná se pravděpodobně o zahraniční subjekt", d.nazev)));
                }
                else
                {
                    //zjisti, zda nejde o osobu
                    osoba = Validators.IsOsoba(dnazev);


                    if (osoba)
                    {
                        issues.Add(new Issue(this, (int)IssueType.IssueTypes.Osoba, "Identifikace smluvní strany", string.Format("Smluvní strana '{0}' bez datove schránky a platného IČO. Jedná se pravděpodobně o fyzickou osobu", d.nazev)));
                    }
                    else
                    {
                        issues.Add(new Issue(this, (int)IssueType.IssueTypes.Chybi_identifikace_smluvni_strany, "Chybí identifikace smluvní strany", string.Format("Smluvní strana '{0}' bez datove schránky a platného IČO.", d.nazev)));
                    }

                }

            }
            else
                issues.AddRange(tmpIss);


            //casove posloupnosti

            //datum vzniku firmy
            Firma firma = null;
            if (hasIco)
                firma = FirmaRepo.FromIco(d.ico, true);
            if (!Firma.IsValid(firma) && hasDS)
                firma = FirmaRepo.FromDS(d.datovaSchranka, true);

            if (Firma.IsValid(firma))
            {
                if (firma.IsNespolehlivyPlatceDPH())
                {
                    var nespoleh = firma.NespolehlivyPlatceDPH();
                    if ((nespoleh.FromDate.HasValue && nespoleh.ToDate.HasValue
                            && nespoleh.FromDate.Value <= item.datumUzavreni && item.datumUzavreni <= nespoleh.ToDate.Value
                          )
                        ||
                        (nespoleh.FromDate.HasValue && nespoleh.ToDate.HasValue == false
                            && nespoleh.FromDate.Value <= item.datumUzavreni
                          )
                        )
                    {
                        issues.Add(new Issue(this,
                                (int)IssueType.IssueTypes.SmlouvaUzavrena_s_NespolehlivymPlatcemDPH,
                                "Smlouva uzavřena s nespolehlivým plátcem DPH.",
                                string.Format("Smlouva byla uzavřena v den, kdy byl dodavatel {0} v registru nespolehlivých plátců DPH Finanční správy.", d.nazev)));
                    }
                }
            }

            if (Firma.IsValid(firma) && firma.Datum_Zapisu_OR.HasValue)
            {
                double zalozeniPredPodpisem = (item.datumUzavreni - firma.Datum_Zapisu_OR.Value).TotalDays;

                if (zalozeniPredPodpisem < 0) //zalozeno po podpisu
                {
                    issues.Add(new Issue(this, (int)IssueType.IssueTypes.Firma_vznikla_az_po, "Firma vznikla až po podpisu smlouvy",
                        string.Format("Firma {0} vznikla {1} dní po podpisu smlouvy", d.nazev, Math.Abs((int)zalozeniPredPodpisem)),
                        null, new { days = (int)zalozeniPredPodpisem }
                        ));
                }
                else if (zalozeniPredPodpisem < 60)
                {
                    issues.Add(new Issue(this, (int)IssueType.IssueTypes.Firma_vznikla_kratce_pred, "Firma vznikla krátce před podpisem smlouvy",
                        string.Format("Firma {0} vznikla {1} dní před podpisem smlouvy", d.nazev, (int)zalozeniPredPodpisem),
                        null, new { days = (int)zalozeniPredPodpisem }, false
                        ));
                }

            }


        }

    }
}
