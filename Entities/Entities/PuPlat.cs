using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Dynamic;
using HlidacStatu.Entities.Dotace;

namespace HlidacStatu.Entities;

[Table("PU_Plat")]
public class PuPlat
{
    [Key]
    public int Id { get; set; }

    public int IdOrganizace { get; set; }
    public int Rok { get; set; }
    public string NazevPozice { get; set; }
    public decimal? Plat { get; set; }
    public decimal? Odmeny { get; set; }

    decimal? _uvazek = 1;
    public decimal? Uvazek
    {
        get {
            if (_uvazek == null)
                _uvazek = 1;
            return _uvazek;
        }

        set
        {
            if (value == null || value == 0)
                _uvazek = 1;
            else
                _uvazek = value;
        }
    }

    decimal? _pocetMesicu = 12;
    public decimal? PocetMesicu
    {
        get  {
            if (_pocetMesicu == null)
                _pocetMesicu = 12;
            return _pocetMesicu;
        }
        set
        {
            if (value == null || value == 0)
                _pocetMesicu = 12;
            else
                _pocetMesicu = value;
        }
    }
    public string NefinancniBonus { get; set; }
    public string PoznamkaPozice { get; set; }
    public string PoznamkaPlat { get; set; }
    public string SkrytaPoznamka { get; set; }
    public bool? JeHlavoun { get; set; }
    public int? DisplayOrder { get; set; }

    // Navigation properties
    [ForeignKey("IdOrganizace")]
    public virtual PuOrganizace Organizace { get; set; }

    public decimal HrubyMesicniPlat => ((Plat ?? 0) + (Odmeny ?? 0)) * (1 / Uvazek ?? 1) / (PocetMesicu ?? 12);

    public decimal CelkovyRocniPlat => (Plat ?? 0) + (Odmeny ?? 0);
    
    public ExpandoObject FlatExport()
    {
        dynamic v = new ExpandoObject();
        v.Organizace = Organizace.Nazev;
        v.Rok = Rok;
        v.Pozice = NazevPozice;
        v.Plat = Plat;
        v.Odmeny = Odmeny;
        v.PocetMesicu = PocetMesicu;
        v.Uvazek = Uvazek;
        v.NefinancniBonus = NefinancniBonus;

        return v;
    }
}