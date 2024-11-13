using System;
using System.Collections.Generic;

namespace STK.Model;

public partial class SmeKontrolaDiesel
{
    public long KontrolaDieselId { get; set; }

    public long KontrolaId { get; set; }

    public int VolnobehLimitMin { get; set; }

    public bool VolnobehLimitMinMan { get; set; }

    public int VolnobehLimitMax { get; set; }

    public bool VolnobehLimitMaxMan { get; set; }

    public int VolnobehHodnota { get; set; }

    public int VolnobehVysledek { get; set; }

    public int PrebehoveLimitMin { get; set; }

    public bool PrebehoveLimitMinMan { get; set; }

    public int PrebehoveLimitMax { get; set; }

    public bool PrebehoveLimitMaxMan { get; set; }

    public int PrebehoveHodnota { get; set; }

    public int PrebehoveVysledek { get; set; }

    public decimal AkceleraceLimitMax { get; set; }

    public bool AkceleraceLimitMaxMan { get; set; }

    public decimal AkceleraceHodnota { get; set; }

    public int AkceleraceVysledek { get; set; }

    public decimal KourivostLimitMax { get; set; }

    public bool KourivostLimitMaxMan { get; set; }

    public decimal KourivostHodnota { get; set; }

    public int KourivostVysledek { get; set; }

    public decimal KourivostRozpetiLimitMax { get; set; }

    public bool KourivostRozpetiLimitMaxMan { get; set; }

    public decimal KourivostRozpetiHodnota { get; set; }

    public int KourivostRozpetiVysledek { get; set; }

    public decimal AbsorbceHodnota { get; set; }

    public int AbsorbceVysledek { get; set; }

    public virtual SmeKontrola Kontrola { get; set; } = null!;
}
