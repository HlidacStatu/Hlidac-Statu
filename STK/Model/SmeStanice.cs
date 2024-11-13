using System;
using System.Collections.Generic;

namespace STK.Model;

public partial class SmeStanice
{
    public long Sme { get; set; }

    public string Nazev { get; set; } = null!;

    public string Ulice { get; set; } = null!;

    public string Obec { get; set; } = null!;

    public string Psc { get; set; } = null!;

    public string Kraj { get; set; } = null!;

    public string Orp { get; set; } = null!;

    public string Telefon { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Kategorie { get; set; } = null!;

    public string Majitel { get; set; } = null!;

    public string Jednatel { get; set; } = null!;

    public string Skupina { get; set; } = null!;

    public long Stk { get; set; }
}
