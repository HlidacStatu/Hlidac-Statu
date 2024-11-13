using System;
using System.Collections.Generic;

namespace STK.Model;

public partial class SmeDefekt
{
    public long KontrolaDefektId { get; set; }

    public long KontrolaId { get; set; }

    public string Kod { get; set; } = null!;

    public virtual SmeKontrola Kontrola { get; set; } = null!;
}
