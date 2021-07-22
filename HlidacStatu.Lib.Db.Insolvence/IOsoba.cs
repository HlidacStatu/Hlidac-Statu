using System;

namespace HlidacStatu.Lib.Db.Insolvence
{
    public interface IOsoba
    {

        long pk { get; set; }
        string RizeniId { get; set; }
        string IdPuvodce { get; set; }
        string IdOsoby { get; set; }
        string PlneJmeno { get; set; }
        string Role { get; set; }
        string Typ { get; set; }
        string RC { get; set; }
        DateTime? Zalozen { get; set; }
        DateTime? Odstranen { get; set; }


        Nullable<DateTime> DatumNarozeni { get; set; }
        string ICO { get; set; }
        string Mesto { get; set; }
        string Okres { get; set; }
        string Zeme { get; set; }
        string PSC { get; set; }
        string OsobaId { get; set; }
    }
}
