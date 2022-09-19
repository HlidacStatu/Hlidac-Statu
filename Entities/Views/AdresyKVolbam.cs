using System;
using Microsoft.EntityFrameworkCore;

namespace HlidacStatu.Entities.Views;

[Keyless]
public class AdresyKVolbam : IEquatable<AdresyKVolbam>
{
    public int Id { get; set; }
    public string Adresa { get; set; }
    public string Obec { get; set; }
        
    public int TypOvm { get; set; }
    public string DatovkaUradu { get; set; }
    public string NazevUradu { get; set; }
    public string AdresaUradu { get; set; }
    

    public bool Equals(AdresyKVolbam other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((AdresyKVolbam)obj);
    }

    public override int GetHashCode()
    {
        return Id;
    }
}