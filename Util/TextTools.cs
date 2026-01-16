using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Devmasters;

namespace HlidacStatu.Util;

public static class TextTools
{
    public static List<string> GetPermutations(string[] words)
    {
        return _getPermutations(words, 0, words.Length - 1)
            .Select(m => string.Join(" ", m))
            .ToList();
    }

    // Recursive function to generate permutations.
    static List<string[]> _getPermutations(string[] words, int start, int end)
    {
        List<string[]> result = new List<string[]>();

        if (start == end)
        {
            // Clone the array so changes in recursion don't affect it.
            result.Add((string[])words.Clone());
        }
        else
        {
            for (int i = start; i <= end; i++)
            {
                _swap(ref words[start], ref words[i]);
                result.AddRange(_getPermutations(words, start + 1, end));
                _swap(ref words[start], ref words[i]); // backtrack
            }
        }

        return result;
    }

    // Helper method to swap two elements in the array.
    static void _swap(ref string a, ref string b)
    {
        string temp = a;
        a = b;
        b = temp;
    }

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
        for (int i = 0; i <= n; d[i, 0] = i++)
        {
        }

        for (int j = 1; j <= m; d[0, j] = j++)
        {
        }

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
        return (c <= 0x2D67) && //odfiltrovat čínštinu,... 
               (char.IsWhiteSpace(c) ||
                char.IsSeparator(c) ||
                char.IsPunctuation(c) ||
                char.IsSymbol(c) ||
                char.IsLetterOrDigit(c));
    }

    /// <summary>
    /// Detects if character is binary value => unusable for text 
    /// </summary>
    public static bool IsBadChar(char currentChar, char? nextChar)
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


    /// <summary>
    /// Specific method - do not move to devmasters
    /// Used in subsidies and jobtable editor
    /// </summary>
    public static decimal? GetDecimalFromText(this string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        input = input.NormalizeToNumbersOnly();
        if (input.Length == 0)
            return null;

        // Remove all spaces upfront (spaces can only be the thousand separators)
        input = Regex.Replace(input, @"\s", "");

        // If there are no separators, parse directly
        if (!input.Contains(",") && !input.Contains("."))
        {
            return decimal.Parse(input, CultureInfo.InvariantCulture);
        }

        int lastCommaIndex = input.LastIndexOf(',');
        int lastDotIndex = input.LastIndexOf('.');

        // if there are both separators, then the last one is decimal separator
        if (lastCommaIndex > -1 && lastDotIndex > -1)
        {
            if (lastCommaIndex > lastDotIndex)
            {
                input = input.Replace(".", string.Empty).Replace(",", "."); //unify to '.' as decimal separator
            }
            else
            {
                input = input.Replace(",", string.Empty);
            }

            return decimal.Parse(input, CultureInfo.InvariantCulture);
        }

        //then there is only one separator
        int commaCount = CharCount(input, ',');
        int dotCount = CharCount(input, '.');

        // separator is used once => there are no decimals
        if (commaCount > 1 || dotCount > 1)
        {
            input = input.Replace(".", string.Empty).Replace(",", string.Empty);
            return decimal.Parse(input, CultureInfo.InvariantCulture);
        }

        // unify separator to '.'
        if (lastCommaIndex > -1)
        {
            input = input.Replace(",", ".");
        }

        // If any other case, then use try parse and return null
        // Check if '.' is exactly three digits from the start xxx.yyyyyy
        if (input.LastIndexOf('.') == 3 && input.Length == 7)
        {
            // Treat as thousand separator
            input = input.Replace(".", string.Empty);
        }

        // Attempt to parse as a decimal
        if (decimal.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }

        // If parsing fails, return null
        return null;
    }

    public static int CharCount(ReadOnlySpan<char> input, char toFind)
    {
        int count = 0;

        foreach (char c in input)
        {
            if (c == toFind)
                count++;
        }

        return count;
    }

    public static int CharCount(string input, char toFind)
    {
        return CharCount(input.AsSpan(), toFind);
    }
}