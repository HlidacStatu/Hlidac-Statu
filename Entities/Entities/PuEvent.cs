using Devmasters.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HlidacStatu.Entities;

[Table("PU_Event")]
public partial class PuEvent
{

    [ShowNiceDisplayName]
    public enum TypUdalosti : int
    {
        [NiceDisplayName("Ostatní")]
        Neurceno = 0,
        [NiceDisplayName("Zaslání žádosti")]
        ZaslaniZadosti = 1,
        [NiceDisplayName("Upřesnění žádosti")]
        Upresneni = 2,
        [NiceDisplayName("Úplné odmítnutí poskytnutí informací")]
        UplneOdmitnutiPoskytnutiInformaci = 3,
        [NiceDisplayName("Částečné odmítnutí poskytnutí informací")]
        CastecneOdmitnutiPoskytnutiInformaci = 4,
        [NiceDisplayName("Poskytnutí informací")]
        PoskytnutiInformace = 5,
        [NiceDisplayName("Žádost o úhradu nákladů")]
        Zadost_o_UhraduNakladu = 6,
        [NiceDisplayName("Stížnost")]
        Stiznost = 7,
        [NiceDisplayName("Odvolání")]
        Odvolani = 8,
        [NiceDisplayName("Jiné")]
        Jine = 9,
        [NiceDisplayName("Zapsání zaslaných údajů do naší databáze")]
        NahraniUdaju = 10,
    }

    public enum SmerKomunikace : int
    {
        ObecnaUdalost = 0,
        ZpravaOdNas = 1,
        ZpravaProNas = 2
    }
    public enum KomunikacniKanal : int
    {
        Jiny = 0,
        DatovaSchranka = 1,
        Email = 2,
        Telefon = 3,
    }
    public enum DruhDotazovaneInformace
    {
        Neuveden = 0,
        Urednik = 1,
        Politik = 2
    }

    [Key]
    [Column("pk")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Pk { get; set; }

    public int? ProRok { get; set; } 
    public string? OsobaNameId { get; set; }

    public TypUdalosti Typ { get; set; } = 0;
    public SmerKomunikace Smer { get; set; } = 0;

    public KomunikacniKanal Kanal { get; set; } = 0;
    public DruhDotazovaneInformace DotazovanaInformace { get; set; } = 0;

    public int IdOrganizace { get; set; }
    public string IcoOrganizace { get; set; }
    public string DsOrganizace { get; set; }
    public DateTime Datum { get; set; }

    public string? NaseCJ { get; set; }
    public string? Poznamka { get; set; }

    public string? PoznamkaSkryta { get; set; }

}