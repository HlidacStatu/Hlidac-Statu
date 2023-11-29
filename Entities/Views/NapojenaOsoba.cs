using Microsoft.EntityFrameworkCore;

namespace HlidacStatu.Entities.Views;

[Keyless]
public class NapojenaOsoba
{
    public Osoba Osoba { get; set; }
    public OsobaEvent OsobaEvent { get; set; }
    
}