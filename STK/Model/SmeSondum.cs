using System;
using System.Collections.Generic;

namespace STK.Model;

public partial class SmeSondum
{
    public long KontrolaSondaId { get; set; }

    public long KontrolaId { get; set; }

    public string Typ { get; set; } = null!;

    public string Vyusteni { get; set; } = null!;

    public int? OtackyMin { get; set; }

    public int? OtackyMax { get; set; }

    public int? OtackyHodnota { get; set; }

    public int? OtackyVysledek { get; set; }

    public decimal? AmplitudaMin { get; set; }

    public decimal? AmplitudaMax { get; set; }

    public decimal? AmplitudaHodnota { get; set; }

    public int? AmplitudaVysledek { get; set; }

    public decimal? FrekvenceMin { get; set; }

    public decimal? FrekvenceMax { get; set; }

    public decimal? FrekvenceHodnota { get; set; }

    public int? FrekvenceVysledek { get; set; }

    public decimal? SignalMin { get; set; }

    public decimal? SignalMax { get; set; }

    public decimal? SignalHodnota1 { get; set; }

    public decimal? SignalHodnota2 { get; set; }

    public int? SignalVysledek { get; set; }

    public virtual SmeKontrola Kontrola { get; set; } = null!;
}
