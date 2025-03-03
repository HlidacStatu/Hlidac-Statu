using System.Text.RegularExpressions;

namespace HlidacStatu.Regexes;

public static partial class All
{
    [GeneratedRegex(@"\b(19|20)\d{2}\b")]
    public static partial Regex MachIsolatedYear();
    [GeneratedRegex(@"(19|20)\d{2}")]
    public static partial Regex MatchLooseYear();
    [GeneratedRegex(@"(\d{1,2}[-./\\]\d{1,2}[-./\\]\d{2,4})|(\d{2,4}[-./\\]\d{1,2}[-./\\]\d{1,2})")]
    public static partial Regex MatchNumericDate();
    [GeneratedRegex(@"\b\d{3}\s?\d{2}\b")]
    public static partial Regex MatchPSC();
    [GeneratedRegex(@"([^,.\\:;/-]+)[,.\\:;/-] ?\1")]
    public static partial Regex MatchRepeatedTextWithDelimiter();
    [GeneratedRegex(@"\b(\d\s*){4,8}\b")]
    public static partial Regex MatchIcoWithPossibleSpacesInBetweenNumbers();
    
    
    
    [GeneratedRegex(@"\s{1,}")]
    public static partial Regex MatchOneOrMoreWhitespaces();
    
    [GeneratedRegex(@"(s|vc|vcetne)[\. ]{0,2}(dph)", RegexOptions.IgnoreCase)]
    public static partial Regex MatchWithDphString();
    [GeneratedRegex(@"(bez)[\. ]{0,2}(dph)", RegexOptions.IgnoreCase)]
    public static partial Regex MatchWithoutDphString();
    [GeneratedRegex(@"(kc|eur|korun|czk)", RegexOptions.IgnoreCase)]
    public static partial Regex MatchCurrencyCodeOrSymbol();
    [GeneratedRegex(@"(/|za| |\b)+(mili|micro|kilo|centi|m|k|c)?(metr|minut|hodin|gram|mesic|litr|min|hod|den|kus|kpl|mes|ks|ha|m|h|g|t|l)(a|u|y)?", RegexOptions.IgnoreCase)]
    public static partial Regex MatchUnitWithPrefix();
    
}