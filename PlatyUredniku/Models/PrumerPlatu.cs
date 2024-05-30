namespace PlatyUredniku.Models;

public class PrumerPlatu
{
    public string DatovaSchrankaOrganizace { get; set; }
    public string NazevOrganizace { get; set; }
    
    public decimal? PlatPrvniRok { get; set; }
    public int PocetPlatuPrvniRok { get; set; }
    public decimal? PocetMesicuPrvniRok { get; set; }
    public decimal? PlatPosledniRok { get; set; }
    public int PocetPlatuPosledniRok { get; set; }
    public decimal? PocetMesicuPosledniRok { get; set; }
    public decimal? NarustAbsolutni => PlatPosledniRok - PlatPrvniRok;
    public decimal? NarustProcentualni => (PlatPosledniRok/PlatPrvniRok) - 1;    
}