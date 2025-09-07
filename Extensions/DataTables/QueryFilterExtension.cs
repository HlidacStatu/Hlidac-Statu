using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace HlidacStatu.Extensions.DataTables;

public static class QueryFilters
{
    private static readonly StringComparer CI = StringComparer.InvariantCultureIgnoreCase;

    public static string[] Choices(this IQueryCollection q, string key)
        => q.TryGetValue(key, out StringValues v) ? v.ToArray() : Array.Empty<string>();

    public static (int? From, int? To) RangeInt(this IQueryCollection q, string key)
        => (TryParseInt(q, key + "From"), TryParseInt(q, key + "To"));

    public static (decimal? From, decimal? To) RangeDecimal(this IQueryCollection q, string key)
        => (TryParseDecimal(q, key + "From"), TryParseDecimal(q, key + "To"));

    private static int? TryParseInt(IQueryCollection q, string key)
        => q.TryGetValue(key, out var v) && int.TryParse(v, out var n) ? n : null;

    private static decimal? TryParseDecimal(IQueryCollection q, string key)
        => q.TryGetValue(key, out var v) && decimal.TryParse(v, NumberStyles.Number, CultureInfo.InvariantCulture, out var n) ? n : null;

    public static bool ContainsCI(this IEnumerable<string> src, string value)
        => src is ISet<string> set ? set.Contains(value) : src.Contains(value, CI);
}