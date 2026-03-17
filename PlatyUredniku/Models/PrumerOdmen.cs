namespace PlatyUredniku.Models;

public class PrumerOdmen
{
    public string DatovaSchrankaOrganizace { get; set; }
    public string NazevOrganizace { get; set; }
    
    public decimal? PrepoctenaMesicniOdmenaPrvniRok { get; set; }
    public decimal? PrumernaOdmenaPrvniRok { get; set; }
    public int PocetPlatuPrvniRok { get; set; }
    public decimal? PocetMesicuPrvniRok { get; set; }
    public decimal? PrepoctenaMesicniOdmenaPosledniRok { get; set; }
    public decimal? PrumernaOdmenaPosledniRok { get; set; }
    public int PocetPlatuPosledniRok { get; set; }
    public decimal? PocetMesicuPosledniRok { get; set; }
    public decimal? NarustMesicniAbsolutni => PrepoctenaMesicniOdmenaPosledniRok - PrepoctenaMesicniOdmenaPrvniRok;
    public decimal? NarustRocniAbsolutni => PrumernaOdmenaPosledniRok - PrumernaOdmenaPrvniRok;
}