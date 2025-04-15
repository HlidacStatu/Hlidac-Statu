using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HlidacStatu.Entities;

[Table("PU_Event")]
public class PuEvent
{
    public enum TypUdalosti : int
    {
        Neurceno = 0,
        ZaslaniZadosti = 1,
        Upresneni = 2,
        UplneOdmitnutiPoskytnutiInformaci = 3,
        CastecneOdmitnutiPoskytnutiInformaci = 4,
        PoskytnutiInformace = 5,
        Zadost_o_UhraduNakladu = 6,
        Stiznost = 7,
        Odvolani = 8,
        Jine = 9,
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