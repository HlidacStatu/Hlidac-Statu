using System.Collections.Generic;
using System.Linq;

namespace HlidacStatu.Entities;

public class PpStat
{
    public class SimplePlatData
    {
        public string osoba { get; set; }
        public string organizace { get; set; }
        public decimal plat { get; set; }
    }
    public PpStat() { }
    public PpStat(int rok, IEnumerable<SimplePlatData> fulldata)
    {
        Rok = rok;
        PocetPrijmu = fulldata.Count();
        PocetOsob = fulldata.Select(m=>m.osoba).Distinct().Count();
        PocetOrganizaci = fulldata.Select(m => m.organizace).Distinct().Count();
        PercentilyPlatu = new Dictionary<int, decimal>
        {
            { 10, HlidacStatu.Util.MathTools.PercentileCont(0.10m, fulldata.Select(m => m.plat)) },
            { 25, HlidacStatu.Util.MathTools.PercentileCont(0.25m, fulldata.Select(m => m.plat)) },
            { 50, HlidacStatu.Util.MathTools.PercentileCont(0.50m, fulldata.Select(m => m.plat)) },
            { 75, HlidacStatu.Util.MathTools.PercentileCont(0.75m, fulldata.Select(m => m.plat)) },
            { 90, HlidacStatu.Util.MathTools.PercentileCont(0.90m, fulldata.Select(m => m.plat)) }
        };
    }
    public PpStat(int rok, int pocetOsob, int pocetOrganizaci, IEnumerable<decimal> platy)
    {
        Rok = rok;
        PocetPrijmu = platy.Count();
        PocetOsob = pocetOsob;
        PocetOrganizaci = pocetOrganizaci;
        PercentilyPlatu = new Dictionary<int, decimal>
        {
            { 10, HlidacStatu.Util.MathTools.PercentileCont(0.10m, platy) },
            { 25, HlidacStatu.Util.MathTools.PercentileCont(0.25m, platy) },
            { 50, HlidacStatu.Util.MathTools.PercentileCont(0.50m, platy) },
            { 75, HlidacStatu.Util.MathTools.PercentileCont(0.75m, platy) },
            { 90, HlidacStatu.Util.MathTools.PercentileCont(0.90m, platy) }
        };
    }
    public int? Rok { get; set; }
    public int PocetPrijmu { get; set; }
    public int PocetOsob { get; set; }
    public int PocetOrganizaci { get; set; }
    public Dictionary<int, decimal> PercentilyPlatu { get;  set; }
}