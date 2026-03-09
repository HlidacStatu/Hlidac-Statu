using System.Threading;
using HlidacStatu.Repositories;

namespace PlatyUredniku;

public static class YearPicker
{
    //Nastavi kontextovou promennou, ktera je per request
    private static readonly AsyncLocal<int?> _puYear = new();
    private static readonly AsyncLocal<int?> _ppYear = new();

    public static int PuDefaultYear => _puYear.Value ?? PuRepo.DefaultYear;
    public static int PpDefaultYear => _ppYear.Value ?? PpRepo.DefaultYear;

    internal static void Set(int? puYear, int? ppYear)
    {
        _puYear.Value = puYear;
        _ppYear.Value = ppYear;
    }

}
