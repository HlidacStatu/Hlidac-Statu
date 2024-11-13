using System;
using System.Collections.Generic;

namespace STK.Model;

public partial class SmeMereniPlyn
{
    public long MereniPlynId { get; set; }

    public long KontrolaId { get; set; }

    public string NadrzVyrobce { get; set; } = null!;

    public string NadrzHomologace { get; set; } = null!;

    public string NadrzZivotnost { get; set; } = null!;

    public string NadrzKontrola { get; set; } = null!;

    public string Palivo { get; set; } = null!;

    public int TypPaliva { get; set; }

    public int Otacky { get; set; }

    public int? Nhodnota { get; set; }

    public int? Nvysledek { get; set; }

    public int? Nmin { get; set; }

    public int? Nmax { get; set; }

    public decimal? Cohodnota { get; set; }

    public int? Covysledek { get; set; }

    public decimal? Comin { get; set; }

    public decimal? Comax { get; set; }

    public decimal? Co2hodnota { get; set; }

    public int? Co2vysledek { get; set; }

    public decimal? Co2min { get; set; }

    public decimal? Co2max { get; set; }

    public decimal? Hchodnota { get; set; }

    public int? Hcvysledek { get; set; }

    public decimal? Hcmin { get; set; }

    public decimal? Hcmax { get; set; }

    public decimal? LambdaHodnota { get; set; }

    public int? LambdaVysledek { get; set; }

    public decimal? LambdaMin { get; set; }

    public decimal? LambdaMax { get; set; }

    public decimal? O2hodnota { get; set; }

    public int? O2vysledek { get; set; }

    public decimal? O2min { get; set; }

    public decimal? O2max { get; set; }

    public decimal? CocoorHodnota { get; set; }

    public int? CocoorVysledek { get; set; }

    public decimal? CocoorMin { get; set; }

    public decimal? CocoorMax { get; set; }

    public decimal? NoxHodnota { get; set; }

    public int? NoxVysledek { get; set; }

    public decimal? NoxMin { get; set; }

    public decimal? NoxMax { get; set; }

    public decimal? TpsHodnota { get; set; }

    public int? TpsVysledek { get; set; }

    public decimal? TpsMin { get; set; }

    public decimal? TpsMax { get; set; }

    public virtual SmeKontrola Kontrola { get; set; } = null!;
}
