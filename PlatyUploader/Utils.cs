using System.Text.RegularExpressions;

namespace PlatyUploader;

public class Utils
{
    private static readonly Regex YearRegexLoose = new(@"(19|20)\d{2}");

    public static int? GetYearFromString(string? yearstring)
    {
        if (yearstring is null)
            return null;

        var match = YearRegexLoose.Match(yearstring);
        if (match.Success)
        {
            var year = int.Parse(match.Value);
            return year;
        }

        return null;
    }
    
    public static bool IsDigitsOnly(ReadOnlySpan<char> input)
    {
        if (input.IsEmpty)
            return false;

        foreach (char c in input)
        {
            if (c < '0' || c > '9')
                return false;
        }

        return true;
    }
    

    public static string? TrimBadChars(string? input)
    {
        return input?.ToLower()
            .Replace("kc", "")
            .Replace("kč", "")
            .Replace(",-", "")
            .Replace("-", "")
            .Replace(" ", "")//normal space
            .Replace(@" ", "")//nbsp
            .Trim();
    }
}