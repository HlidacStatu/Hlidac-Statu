namespace PlatyUredniku.Models;

public class PrumerPlatu
{
    public string DatovaSchrankaOrganizace { get; set; }
    public string NazevOrganizace { get; set; }
    
    public decimal? PlatPrvniRok { get; set; }
    public decimal? PlatPosledniRok { get; set; }
    public decimal? PocetMesicu { get; set; }
    public decimal? NarustAbsolutni => PlatPosledniRok - PlatPrvniRok;
    public decimal? NarustProcentualni => (PlatPosledniRok/PlatPrvniRok) - 1;    
}