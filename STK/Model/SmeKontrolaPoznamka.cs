using System;
using System.Collections.Generic;

namespace STK.Model;

public partial class SmeKontrolaPoznamka
{
    public long KontrolaPoznamkaId { get; set; }

    public long KontrolaId { get; set; }

    public string Text { get; set; } = null!;

    public virtual SmeKontrola Kontrola { get; set; } = null!;
}
