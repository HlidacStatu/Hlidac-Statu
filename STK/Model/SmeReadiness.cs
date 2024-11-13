using System;
using System.Collections.Generic;

namespace STK.Model;

public partial class SmeReadiness
{
    public long ReadinessId { get; set; }

    public long KontrolaId { get; set; }

    public bool? AcS { get; set; }

    public bool? AcT { get; set; }

    public bool? BoostS { get; set; }

    public bool? BoostT { get; set; }

    public bool? CatfuncS { get; set; }

    public bool? CatfuncT { get; set; }

    public bool? ColdS { get; set; }

    public bool? ColdT { get; set; }

    public bool? CompS { get; set; }

    public bool? CompT { get; set; }

    public bool? DpfS { get; set; }

    public bool? DpfT { get; set; }

    public bool? EgrvvtS { get; set; }

    public bool? EgrvvtT { get; set; }

    public bool? EgsS { get; set; }

    public bool? EgsT { get; set; }

    public bool? EgsfuncS { get; set; }

    public bool? EgsfuncT { get; set; }

    public bool? EgsheatS { get; set; }

    public bool? EgsheatT { get; set; }

    public bool? EvapS { get; set; }

    public bool? EvapT { get; set; }

    public bool? FuelS { get; set; }

    public bool? FuelT { get; set; }

    public bool? HcatS { get; set; }

    public bool? HcatT { get; set; }

    public bool? MisfS { get; set; }

    public bool? MisfT { get; set; }

    public bool? NmhcS { get; set; }

    public bool? NmhcT { get; set; }

    public bool? NoxS { get; set; }

    public bool? NoxT { get; set; }

    public bool? O2sfuncS { get; set; }

    public bool? O2sfuncT { get; set; }

    public bool? O2sheatS { get; set; }

    public bool? O2sheatT { get; set; }

    public bool? ReserveS { get; set; }

    public bool? ReserveT { get; set; }

    public bool? SasS { get; set; }

    public bool? SasT { get; set; }

    public virtual SmeKontrola Kontrola { get; set; } = null!;
}
