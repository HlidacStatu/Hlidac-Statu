using System;
using System.Text;

namespace HlidacStatu.Util;

public static class TextTools
{

    /// <summary>
    /// difference between two sequences. the Levenshtein distance between two words is the minimum number of single-character edits (i.e. insertions, deletions, or substitutions) required to change one word into the other.
    /// </summary>
    /// <param name="s"></param>
    /// <param name="t"></param>
    /// <returns></returns>
    public static int LevenshteinDistanceCompute(string s, string t)
    {
        if (string.IsNullOrEmpty(s))
        {
            if (string.IsNullOrEmpty(t))
                return 0;
            return t.Length;
        }

        if (string.IsNullOrEmpty(t))
        {
            return s.Length;
        }

        int n = s.Length;
        int m = t.Length;
        int[,] d = new int[n + 1, m + 1];

        // initialize the top and right of the table to 0, 1, 2, ...
        for (int i = 0; i <= n; d[i, 0] = i++) ;
        for (int j = 1; j <= m; d[0, j] = j++) ;

        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                int min1 = d[i - 1, j] + 1;
                int min2 = d[i, j - 1] + 1;
                int min3 = d[i - 1, j - 1] + cost;
                d[i, j] = Math.Min(Math.Min(min1, min2), min3);
            }
        }

        return d[n, m];
    }

    [Obsolete("Use IsBadChar - this is copied from .net code and should be more reliable")]
    public static bool IsValidCharacter(char c)
    {
        return (c <= 0x2D67) &&  //odfiltrovat čínštinu,... 
               (char.IsWhiteSpace(c) || 
                char.IsSeparator(c) || 
                char.IsPunctuation(c) || 
                char.IsSymbol(c) ||
                char.IsLetterOrDigit(c));
    }
    
    /// <summary>
    /// Detects if character is binary value => unusable for text 
    /// </summary>
    public static bool IsBadChar(char currentChar, char? nextChar )
    {
        if (currentChar >= '\uD800')
        {
            if (currentChar == '\uFFFE' || char.IsLowSurrogate(currentChar))
                return true;
            if (char.IsHighSurrogate(currentChar))
            {
                if (nextChar is null || !char.IsLowSurrogate(nextChar.Value))
                    return true;
            }
        }

        return false;
    }
    
    /// <summary>
    /// Detects if string contains binary value 
    /// </summary>
    public static bool HasInvalidUnicodeSequence(string s)
    {
        for (int index = 0; index < s.Length; ++index)
        {
            char c = s[index];
            if (c >= '\uD800')
            {
                if (c == '\uFFFE' || char.IsLowSurrogate(c))
                    return true;
                if (char.IsHighSurrogate(c))
                {
                    if (index + 1 >= s.Length || !char.IsLowSurrogate(s[index + 1]))
                        return true;
                    ++index;
                }
            }
        }
        return false;
    }
    
    public static string CleanBadBytesFromText(this string text)
    {
        StringBuilder cleaned = new StringBuilder();

        for (var index = 0; index < text.Length; ++index)
        {
            var currentChar = text[index];
            var nextChar = index + 1 < text.Length ? text[index + 1] : (char?)null;

            if (!IsBadChar(currentChar, nextChar))
            {
                cleaned.Append(currentChar);
            }
        }

        return cleaned.ToString();
    }
}