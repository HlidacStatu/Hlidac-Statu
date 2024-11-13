using System;
using System.Collections.Generic;

namespace STK.Model;

public partial class StkStanice
{
    public long Stk { get; set; }

    public string Kod { get; set; } = null!;

    public string Nazev { get; set; } = null!;

    public string Ulice { get; set; } = null!;

    public string Obec { get; set; } = null!;

    public string Psc { get; set; } = null!;

    public string Kraj { get; set; } = null!;

    public string Orp { get; set; } = null!;

    public string Telefon { get; set; } = null!;

    public string Email { get; set; } = null!;
}
