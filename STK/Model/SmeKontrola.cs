using System;
using System.Collections.Generic;

namespace STK.Model;

public partial class SmeKontrola
{
    public long KontrolaId { get; set; }

    public int FileYear { get; set; }

    public int FileMonth { get; set; }

    public long Sme { get; set; }

    public DateTime? Time1 { get; set; }

    public DateTime? Time2 { get; set; }

    public int Typ { get; set; }

    public int Vysledek { get; set; }

    public string Protokol { get; set; } = null!;

    public string Technik { get; set; } = null!;

    public int Poradi { get; set; }

    public string Druh { get; set; } = null!;

    public string Kategorie { get; set; } = null!;

    public string Spz { get; set; } = null!;

    public string Vin { get; set; } = null!;

    public bool Vinok { get; set; }

    public string Znacka { get; set; } = null!;

    public string Model { get; set; } = null!;

    public string Palivo { get; set; } = null!;

    public string RecVin { get; set; } = null!;

    public bool RecVinok { get; set; }

    public string RecZnacka { get; set; } = null!;

    public string RecModel { get; set; } = null!;

    public string RecPalivo { get; set; } = null!;

    public string DetVin { get; set; } = null!;

    public bool DetVinok { get; set; }

    public string DetZnacka { get; set; } = null!;

    public string DetModel { get; set; } = null!;

    public string DetPalivo { get; set; } = null!;

    public string TypMotoru { get; set; } = null!;

    public string CisloMotoru { get; set; } = null!;

    public string CisloTp { get; set; } = null!;

    public int Tachometr { get; set; }

    public DateTime? Registrace { get; set; }

    public int Rok { get; set; }

    public decimal Stari { get; set; }

    public DateTime? Pristi { get; set; }

    public bool VizualniKontrola { get; set; }

    public bool Readiness { get; set; }

    public string Mil { get; set; } = null!;

    public string Ecu { get; set; } = null!;

    public string StavEcu { get; set; } = null!;

    public int PocetZavad { get; set; }

    public bool TesnostPlynu { get; set; }

    public int EsTyp { get; set; }

    public bool? EsReadiness { get; set; }

    public long? PristrojId { get; set; }

    public virtual ICollection<SmeDefekt> SmeDefekts { get; set; } = new List<SmeDefekt>();

    public virtual ICollection<SmeKontrolaBenzin> SmeKontrolaBenzins { get; set; } = new List<SmeKontrolaBenzin>();

    public virtual ICollection<SmeKontrolaDiesel> SmeKontrolaDiesels { get; set; } = new List<SmeKontrolaDiesel>();

    public virtual ICollection<SmeKontrolaPoznamka> SmeKontrolaPoznamkas { get; set; } = new List<SmeKontrolaPoznamka>();

    public virtual ICollection<SmeMereniBenzin> SmeMereniBenzins { get; set; } = new List<SmeMereniBenzin>();

    public virtual ICollection<SmeMereniDiesel> SmeMereniDiesels { get; set; } = new List<SmeMereniDiesel>();

    public virtual ICollection<SmeMereniPlyn> SmeMereniPlyns { get; set; } = new List<SmeMereniPlyn>();

    public virtual ICollection<SmeReadiness> SmeReadinesses { get; set; } = new List<SmeReadiness>();

    public virtual ICollection<SmeSondum> SmeSonda { get; set; } = new List<SmeSondum>();
}
