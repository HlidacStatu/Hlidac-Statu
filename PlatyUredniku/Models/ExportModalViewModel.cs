using System;

namespace PlatyUredniku.Models;

public class ExportModalViewModel
{
    public string Part { get; set; } = "urednici";
    public int? Rok { get; set; }
    public string? DatovaSchranka { get; set; }
    public string Titulek { get; set; }
    
    public string? NazevTlacitka { get; set; }


    public override int GetHashCode()
    {
        return HashCode.Combine(Rok, DatovaSchranka, Titulek);
    }
}