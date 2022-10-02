using System.Text;

namespace VolicskyPrukaz.Models;

public class Zadost
{
    public string AdresaZadatele
    {
        get
        {
            StringBuilder sb = new StringBuilder(Ulice);
            if (!string.IsNullOrWhiteSpace(CastObce))
                sb.Append($", {CastObce}");
            sb.Append($", {Obec}");
            return sb.ToString();
        }
    }

    public string AdresaUradu
    {
        get
        {
            StringBuilder sb = new StringBuilder(UradUlice);
            if (!string.IsNullOrWhiteSpace(UradCastObce))
                sb.Append($", {UradCastObce}");
            sb.Append($", {UradObec}");
            return sb.ToString();
        }
    }


    public string? JmenoZadatele { get; set; }
    public string? DatumNarozeniZadatele { get; set; }
    public string? TelefonZadatele { get; set; }
    public string? Prevzeti { get; set; }
    public string? PrevzetiAdresa { get; set; }
    public string? Ulice { get; set; }
    public string? CastObce { get; set; }
    public string? Obec { get; set; }
    public string? UradNazev { get; set; }
    public string? UradUlice { get; set; }
    public string? UradCastObce { get; set; }
    public string? UradObec { get; set; }
    public bool PrvniKolo { get; set; }
    public bool DruheKolo { get; set; }
}