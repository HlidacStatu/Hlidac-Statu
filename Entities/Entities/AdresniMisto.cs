using CsvHelper.Configuration.Attributes;

namespace HlidacStatu.Entities;

public class AdresniMisto
{
    [Ignore]
    public int Id {get; set;}
    [Name("Kód ADM")]
    public int KodAdm {get; set;}
    [Name("Kód obce")]
    public int KodObce {get; set;}
    [Name("Název obce")]
    public string NazevObce {get; set;}
    [Name("Kód MOMC")]
    [Optional]
    public int? KodMomc {get; set;}
    [Name("Název MOMC")]
    public string NazevMomc {get; set;}
    [Name("Název MOP")]
    public string NazevMop {get; set;}
    [Name("Kód části obce")]
    public int? KodCastiObce {get; set;}
    [Name("Název části obce")]
    public string NazevCastiObce {get; set;}
    [Name("Název ulice")]
    public string NazevUlice {get; set;}
    [Name("Typ SO")]
    public string TypSO {get; set;}
    [Name("Číslo domovní")]
    public int? CisloDomovni {get; set;}
    [Name("Číslo orientační")]
    public string CisloOrientacni {get; set;}
    [Name("Znak čísla orientačního")]
    public string ZnakCislaOrientacniho {get; set;}
    [Name("PSČ")]
    public string Psc {get; set;}
    [Name("Souřadnice X")]
    public string CoordX {get; set;}
    [Name("Souřadnice Y")]
    public string CoordY {get; set;}
    [Name("Jednořádková adresa")]
    public string OneLiner {get; set;}
    [Name("Číslo volebního okrsku")]
    public string CisloVolebnihoOkrsku {get; set;}    
}