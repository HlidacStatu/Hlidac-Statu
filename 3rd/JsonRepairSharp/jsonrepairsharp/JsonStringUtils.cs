using System.Text.RegularExpressions;
namespace JsonRepairSharp;

/// <summary>
/// This class provides utility methods working with strings for jsonrepair.
/// </summary>
public static class StringUtils
{
    public const int CodeBackslash                = 0x5c; // "\"
    public const int CodeSlash                    = 0x2f; // "/"
    public const int CodeAsterisk                 = 0x2a; // "*"
    public const int CodeOpeningBrace             = 0x7b; // "{"
    public const int CodeClosingBrace             = 0x7d; // "}"
    public const int CodeOpeningBracket           = 0x5b; // "["
    public const int CodeClosingBracket           = 0x5d; // "]"
    public const int CodeOpenParenthesis          = 0x28; // "("
    public const int CodeCloseParenthesis         = 0x29; // ")"
    public const int CodeSpace                    = 0x20; // " "
    public const int CodeNewline                  = 0xa; // "\n"
    public const int CodeTab                      = 0x9; // "\t"
    public const int CodeReturn                   = 0xd; // "\r"
    public const int CodeBackspace                = 0x08; // "\b"
    public const int CodeFormFeed                 = 0x0c; // "\f"
    public const int CodeDoubleQuote              = 0x0022; // "
    public const int CodePlus                     = 0x2b; // "+"
    public const int CodeMinus                    = 0x2d; // "-"
    public const int CodeQuote                    = 0x27; // "'"
    public const int CodeZero                     = 0x30;
    public const int CodeOne                      = 0x31;
    public const int CodeNine                     = 0x39;
    public const int CodeComma                    = 0x2c; // ","
    public const int CodeDot                      = 0x2e; // "." (dot, period)
    public const int CodeColon                    = 0x3a; // ":"/// <param name="code">The character to check.</param>
    public const int CodeSemicolon                = 0x3b; // ";"
    public const int CodeUppercaseA               = 0x41; // "A"
    public const int CodeLowercaseA               = 0x61; // "a"
    public const int CodeUppercaseE               = 0x45; // "E"
    public const int CodeLowercaseE               = 0x65; // "e"
    public const int CodeUppercaseF               = 0x46; // "F"
    public const int CodeLowercaseF               = 0x66; // "f"
    private const int CodeNonBreakingSpace        = 0xa0;
    private const int CodeEnQuad                  = 0x2000;
    private const int CodeHairSpace               = 0x200a;
    private const int CodeNarrowNoBreakSpace      = 0x202f;
    private const int CodeMediumMathematicalSpace = 0x205f;
    private const int CodeIdeographicSpace        = 0x3000;
    private const int CodeDoubleQuoteLeft         = 0x201c; // “
    private const int CodeDoubleQuoteRight        = 0x201d; // ”
    private const int CodeQuoteLeft               = 0x2018; // ‘
    private const int CodeQuoteRight              = 0x2019; // ’
    private const int CodeGraveAccent             = 0x0060; // `
    private const int CodeAcuteAccent             = 0x00b4; // ´

    private static readonly Regex RegexDelimiter     = new Regex(@"^[,:\[\]\{\}\(\)\n]$");   
    private static readonly Regex RegexStartOfValue  = new Regex(@"^[\[\{\w-]$");         

    /// <summary>
    /// Checks if the given character code represents a hexadecimal digit.
    /// </summary>
    /// <param name="code">The character to check.</param>
    /// <returns>True if the code represents a hexadecimal digit, false otherwise.</returns>
    public static bool IsHex(int code)
    {

        
        return (code >= CodeZero       && code <= CodeNine)       ||
               (code >= CodeUppercaseA && code <= CodeUppercaseF) ||
               (code >= CodeLowercaseA && code <= CodeLowercaseF);
    }

    /// <summary>
    /// Checks if the given character code represents a digit.
    /// </summary>
    /// <param name="code">The character to check.</param>
    /// <returns>True if the code represents a digit, false otherwise.</returns>
    public static bool IsDigit(int code)
    {
        return code >= CodeZero && code <= CodeNine;
    }

    /// <summary>
    /// Checks if the given character code represents a non-zero digit.
    /// </summary>
    /// <param name="code">The character to check.</param>
    /// <returns>True if the code represents a non-zero digit, false otherwise.</returns>
    public static bool IsNonZeroDigit(int code)
    {
        return code >= CodeOne && code <= CodeNine;
    }

    /// <summary>
    /// Checks if the given character code represents a valid string character.
    /// </summary>
    /// <param name="code">The character to check.</param>
    /// <returns>True if the code represents a valid string character, false otherwise.</returns>
    public static bool IsValidStringCharacter(int code)
    {
        return code >= 0x20 && code <= 0x10ffff;
    }

    /// <summary>
    /// Checks if the given character is a delimiter.
    /// </summary>
    /// <param name="character">The character to check.</param>
    /// <returns>True if the character is a delimiter, false otherwise.</returns>
    public static bool IsDelimiter(string character)
    {
        return RegexDelimiter.IsMatch(character) || (!string.IsNullOrEmpty(character) && IsQuote(character[0]));
    }

    /// <summary>
    /// Checks if the given character is the start of a value.
    /// </summary>
    /// <param name="character">The character to check.</param>
    /// <returns>True if the character is the start of a value, false otherwise.</returns>
    public static bool IsStartOfValue(string character)
    {
        return RegexStartOfValue.IsMatch(character) || (!string.IsNullOrEmpty(character) && IsQuote(character[0]));
    }

    /// <summary>
    /// Checks if the given character code represents a control character.
    /// </summary>
    /// <param name="code">The character to check.</param>
    /// <returns>True if the code represents a control character, false otherwise.</returns>
    public static bool IsControlCharacter(int code)
    {
        return code == CodeNewline   ||
               code == CodeReturn    ||
               code == CodeTab       ||
               code == CodeBackspace ||
               code == CodeFormFeed;
    }

    /// <summary>
    /// Checks if the given character code represents a whitespace character.
    /// </summary>
    /// <param name="code">The character to check.</param>
    /// <returns>True if the code represents a whitespace character, false otherwise.</returns>
    public static bool IsWhitespace(int code)
    {
        return code == CodeSpace || code == CodeNewline || code == CodeTab || code == CodeReturn;
    }

    /// <summary>
    /// Checks if the given character code represents a special whitespace character.
    /// </summary>
    /// <param name="code">The character to check.</param>
    /// <returns>True if the code represents a special whitespace character, false otherwise.</returns>
    public static bool IsSpecialWhitespace(int code)
    {
        return code == CodeNonBreakingSpace                  ||
               (code >= CodeEnQuad && code <= CodeHairSpace) ||
               code == CodeNarrowNoBreakSpace                ||
               code == CodeMediumMathematicalSpace           ||
               code == CodeIdeographicSpace;
    }

    /// <summary>
    /// Checks if the given character code represents a quote character.
    /// </summary>
    /// <param name="code">The character to check.</param>
    /// <returns>True if the code represents a quote character, false otherwise.</returns>
    public static bool IsQuote(int code)
    {
        return IsDoubleQuoteLike(code) || IsSingleQuoteLike(code);
    }

    /// <summary>
    /// Checks if the given character code represents a double quote-like character.
    /// </summary>
    /// <param name="code">The character to check.</param>
    /// <returns>True if the code represents a double quote-like character, false otherwise.</returns>
    public static bool IsDoubleQuoteLike(int code)
    {
        return code == CodeDoubleQuote || code == CodeDoubleQuoteLeft || code == CodeDoubleQuoteRight;
    }

    /// <summary>
    /// Checks if the given character code represents a double quote character.
    /// </summary>
    /// <param name="code">The character to check.</param>
    /// <returns>True if the code represents a double quote character, false otherwise.</returns>
    public static bool IsDoubleQuote(int code)
    {
        return code == CodeDoubleQuote;
    }

    /// <summary>
    /// Checks if the given character code represents a single quote-like character.
    /// </summary>
    /// <param name="code">The character to check.</param>
    /// <returns>True if the code represents a single quote-like character, false otherwise.</returns>
    public static bool IsSingleQuoteLike(int code)
    {
        return code == CodeQuote       ||
               code == CodeQuoteLeft   ||
               code == CodeQuoteRight  ||
               code == CodeGraveAccent ||
               code == CodeAcuteAccent;
    }

    /// <summary>
    /// Strips the last occurrence of a substring from the given text.
    /// </summary>
    /// <param name="text">The text to strip.</param>
    /// <param name="textToStrip">The substring to strip.</param>
    /// <param name="stripRemainingText">True to strip the remaining text after the last occurrence, false otherwise.</param>
    /// <returns>The text with the last occurrence of the substring stripped.</returns>
    public static string StripLastOccurrence(string text, string textToStrip, bool stripRemainingText = false)
    {
        var index = text.LastIndexOf(textToStrip, StringComparison.Ordinal);
        return index != -1
            ? text.Substring(0, index) + (stripRemainingText ? string.Empty : text.Substring(index + 1))
            : text;
    }

    /// <summary>
    /// Inserts a string before the last whitespace in the given text.
    /// </summary>
    /// <param name="text">The text to insert into.</param>
    /// <param name="textToInsert">The string to insert.</param>
    /// <returns>The modified text with the string inserted.</returns>
    public static string InsertBeforeLastWhitespace(string text, string textToInsert)
    {
        var index = text.Length;

        if (!IsWhitespace(text[index - 1]))
        {
            // no trailing whitespaces
            return text + textToInsert;
        }

        while (IsWhitespace(text[index - 1]))
        {
            index--;
        }

        return text.Substring(0, index) + textToInsert + text.Substring(index);
    }

    /// <summary>
    /// Removes a substring from the given text starting at the specified index.
    /// </summary>
    /// <param name="text">The text to remove from.</param>
    /// <param name="start">The starting index of the substring to remove.</param>
    /// <param name="count">The number of characters to remove.</param>
    /// <returns>The modified text with the substring removed.</returns>
    public static string RemoveAtIndex(string text, int start, int count)
    {
        return text.Substring(0, start) + text.Substring(start + count);
    }

    /// <summary>
    /// Checks if the given text ends with a comma or a newline.
    /// </summary>
    /// <param name="text">The text to check.</param>
    /// <returns>True if the text ends with a comma or a newline, false otherwise.</returns>
    public static bool EndsWithCommaOrNewline(string text)
    {
        return Regex.IsMatch(text, @"[,\n][ \t\r]*$");
    }
}

/// <summary>
/// This class provides extension methods for the string class.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Returns the character code at the specified index in the string.
    /// </summary>
    /// <param name="str">The string to retrieve the character code from.</param>
    /// <param name="i">The index of the character.</param>
    /// <returns>The character code at the specified index.</returns>
    public static char CharCodeAt(this string str, int i)
    {
        if (i < 0 || i >= str.Length) return '\0';
        return str[i];
    }

    /// <summary>
    /// Returns a substring of the specified length starting from the specified index in the string,
    /// ensuring that the substring does not exceed the string length.
    /// </summary>
    /// <param name="str">The string to retrieve the substring from.</param>
    /// <param name="startIndex">The starting index of the substring.</param>
    /// <param name="length">The length of the substring.</param>
    /// <returns>The substring of the specified length starting from the specified index.</returns>
    public static string SubstringSafe(this string str, int startIndex, int length)
    {
        return str.Substring(startIndex, Math.Min(length, str.Length - startIndex));
    }
}

/// <summary>
/// This class represents matching quotes and provides methods to check if an end quote matches a start quote.
/// </summary>
public class MatchingQuotes
{
    private bool _isSingleQuoteLike;
    private bool _isDoubleQuoteLike;
    private bool _isDoubleQuote;

    /// <summary>
    /// Sets the start quote based on the given character code.
    /// </summary>
    /// <param name="code">The code representing the start quote.</param>
    public void SetStartQuote(int code)
    {
        _isSingleQuoteLike = StringUtils.IsSingleQuoteLike(code);
        _isDoubleQuote     = StringUtils.IsDoubleQuote    (code);
        _isDoubleQuoteLike = StringUtils.IsDoubleQuoteLike(code);

    }

    /// <summary>
    /// Checks if the given character code represents a matching end quote for the start quote.
    /// </summary>
    /// <param name="code">The code representing the end quote.</param>
    /// <returns>True if the code represents a matching end quote, false otherwise.</returns>
    public bool IsMatchingEndQuote(int code)
    {
        return
            _isSingleQuoteLike ?
                StringUtils.IsSingleQuoteLike(code) :
                _isDoubleQuote ?
                    StringUtils.IsDoubleQuote(code) :
                    _isDoubleQuoteLike ?
                        StringUtils.IsDoubleQuoteLike(code) :
                        false;
    }

}