using System;
using System.Text;

namespace HlidacStatu.Util;

public static class TextTools
{
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