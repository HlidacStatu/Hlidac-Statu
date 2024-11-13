using System;
using System.Collections.Generic;

namespace STK.Model;

public partial class SmeMereniDiesel
{
    public long MereniDieselId { get; set; }

    public long KontrolaId { get; set; }

    public int Typ { get; set; }

    public int Poradi { get; set; }

    public int VolnobehHodnota { get; set; }

    public int VolnobehVysledek { get; set; }

    public int PrebehoveHodnota { get; set; }

    public int PrebehoveVysledek { get; set; }

    public decimal AkceleraceHodnota { get; set; }

    public int AkceleraceVysledek { get; set; }

    public decimal KourivostHodnota { get; set; }

    public int KourivostVysledek { get; set; }

    public decimal TpsHodnota { get; set; }

    public int TpsVysledek { get; set; }

    public int TeplotaHodnota { get; set; }

    public int TeplotaVysledek { get; set; }

    public virtual SmeKontrola Kontrola { get; set; } = null!;
}
