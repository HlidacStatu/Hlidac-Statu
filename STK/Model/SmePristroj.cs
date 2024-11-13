using System;
using System.Collections.Generic;

namespace STK.Model;

public partial class SmePristroj
{
    public long PristrojId { get; set; }

    public string Vyrobce { get; set; } = null!;

    public string Typ { get; set; } = null!;

    public string Verze { get; set; } = null!;

    public string Software { get; set; } = null!;

    public string Obd { get; set; } = null!;
}
