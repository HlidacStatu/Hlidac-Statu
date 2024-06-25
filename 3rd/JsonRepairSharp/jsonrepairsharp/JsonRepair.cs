using System.Reflection;
using System.Text.RegularExpressions;
namespace JsonRepairSharp;


/*
   * Repair a string containing an invalid JSON document.
   * For example output from a LLM model
   *
   * Example:
    try
    {
        string json = "{name: 'John'}";
        string repaired = JSONRepair.JsonRepair(json);
        Console.WriteLine(repaired);
        // Output: {"name": "John"}
    }
    catch (JSONRepairError err)
    {
        Console.WriteLine(err.Message);
        Console.WriteLine("Position: " + err.Data["Position"]);
    }
 */

public static class JsonRepair
{
    public enum InputType
    {
        LLM,
        Other
    }

    public static bool ThrowExceptions { get; set; } = true;
    public static InputType Context    { get; set; } = InputType.Other;


    /// <summary>
    /// Dictionary of control characters and their corresponding escape sequences.
    /// </summary>
    private static readonly Dictionary<char, string> ControlCharacters = new Dictionary<char, string>
    {
        { '\b', "\b" },
        { '\f', "\f" },
        { '\n', "\n" },
        { '\r', "\r" },
        { '\t', "\t" }
    };

    /// <summary>
    /// Dictionary of escape characters and their corresponding escape sequences.
    /// </summary>
    private static readonly Dictionary<char, string> EscapeCharacters = new Dictionary<char, string>
    {
        { '\"', "\"" },
        { '\\', "\\" },
        { '/' , "/"  },
        { 'b' , "\b" },
        { 'f' , "\f" },
        { 'n' , "\n" },
        { 'r' , "\r" },
        { 't' , "\t" }
    };



    private static int     _i       = 0  ; // Current index in input text
    private static string  _text    = "" ; // input text
    private static string  _output  = "" ; // generated output
    private static readonly MatchingQuotes MatchingQuotes = new MatchingQuotes(); // Helper class to match opening and closing quotes
    private static int _closeCode   ='\0';

    /// <summary>
    /// Repairs a string containing an invalid JSON document.
    /// </summary>
    /// <param name="input">The JSON document to repair</param>
    /// <returns>The repaired JSON document</returns>
    /// <exception cref="JSONRepairError">Thrown when an error occurs during JSON repair</exception>
    public static string RepairJson(string input)
    {
        _i      = 0;
        _output = "";
        _text   = input;
        bool strippedHeadingText = false;

        if (Context == InputType.LLM)
        {
            // LLMs are prone to adding an introduction and trailing explanation to any data structure
            // Repair: remove these first
            strippedHeadingText = ParseAndSkipAllUntilFirstBrace();
        }

        var processed = ParseValue();
        if (!processed) { ThrowUnexpectedEnd(); }

        var processedComma = ParseCharacter(StringUtils.CodeComma);
        if (processedComma)
        {
            ParseWhitespaceAndSkipComments();
        }


        // trailing characters after end of the root level object
        // For LLMs we will skip this, as it is likely trailing text. e.g. giving explanation on the structure above
        if (Context == InputType.LLM && strippedHeadingText)
        {
            //Remove everything after final bracket
            var strippedTrailingText = ParseAndStripUntilLastBrace();
            if (strippedTrailingText) return _output;
        }

        if (StringUtils.IsStartOfValue(_text.CharCodeAt(_i).ToString()) && StringUtils.EndsWithCommaOrNewline(_output))
        {
            // start of a new value after end of the root level object: looks like
            // newline delimited JSON -> turn into a root level array
            if (!processedComma)
            {
                // repair missing comma
                _output = StringUtils.InsertBeforeLastWhitespace(_output, ",");
            }

            ParseNewlineDelimitedJson();
        }
        else if (processedComma)
        {
            // repair: remove trailing comma
            _output = StringUtils.StripLastOccurrence(_output, ",");
        }

        if (_i >= _text.Length)
        {
            // reached the end of the document properly
            return _output;
        }
        ThrowUnexpectedCharacter();

        return _output;
    }




    /// <summary>
    /// Parses a JSON value.
    /// </summary>
    /// <returns>True if a value was parsed, false otherwise</returns>
    private static bool ParseValue()
    {
        ParseWhitespaceAndSkipComments();
        bool processed =
            ParseObject()   ||
            ParseArray()    ||
            ParseString()   ||
            ParseNumber()   ||
            ParseKeywords() ||
            ParseUnquotedString();
        ParseWhitespaceAndSkipComments();

        return processed;
    }

    private static bool ParseAndSkipAllUntilFirstBrace()
    {
        var start = _i;

        while (
            _text.CharCodeAt(_i) != StringUtils.CodeOpeningBracket &&
            _text.CharCodeAt(_i) != StringUtils.CodeOpeningBrace)
        {
            _i++;
            if (_i >= _text.Length )
            {
                // Could not find any start brace, abort attempt
                _i = start; return false;
            }
        }
        _closeCode = (_text.CharCodeAt(_i) == StringUtils.CodeOpeningBracket) ? StringUtils.CodeClosingBracket :
                     (_text.CharCodeAt(_i) == StringUtils.CodeOpeningBrace  ) ? StringUtils.CodeClosingBrace : '\0';
        if (_text.IndexOf((char)_closeCode) == -1)
        {
            // Could not find any matching closing brace, abort attempt
            _i = start; return false;
        }

        return true;
    }

    private static bool ParseAndStripUntilLastBrace()
    {
        var start = _output.Length-1;
        var o     = _output.Length-1;
        while (_output.CharCodeAt(o) != _closeCode && o > 0) { o--; }

        if (o == 0) 
        {
            // could not find end brace/bracket, abort attempt
            return false;
        }
        o       = Math.Min(o + 1, _output.Length);
        _output = _output.Substring(0, o);
        return true;
    }


    /// <summary>
    /// Parses and repairs whitespace in the JSON document.
    /// </summary>
    /// <returns>True if any whitespace was parsed and repaired, false otherwise</returns>
    private static bool ParseWhitespaceAndSkipComments()
    {
        var start = _i;
        if (_i >= _text.Length) return false;

        var changed = ParseWhitespace();
        do
        {
            changed = ParseComment();
            if (changed)
            {
                changed = ParseWhitespace();
            }
        } while (changed);

        return _i > start;
    }

    /// <summary>
    /// Parses and repairs whitespace in the JSON document.
    /// </summary>
    /// <returns>True if any whitespace was parsed and repaired, false otherwise</returns>
    private static bool ParseWhitespace()
    {
        string whitespace = "";
        bool normal;
        while ((normal = StringUtils.IsWhitespace(_text.CharCodeAt(_i))) || StringUtils.IsSpecialWhitespace(_text.CharCodeAt(_i)))
        {
            if (normal)
            {
                whitespace += _text.CharCodeAt(_i);
            }
            else
            {
                // repair special whitespace
                whitespace += " ";
            }

            _i++;
        }

        if (whitespace.Length > 0)
        {
            _output += whitespace;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Parses and removes any comments
    /// </summary>
    /// <returns>True if any comment was parsed and removed, false otherwise</returns>
    private static bool ParseComment()
    {
        // find a block comment '/* ... */'
        if (_text.CharCodeAt(_i) == StringUtils.CodeSlash && _text.CharCodeAt(_i + 1) == StringUtils.CodeAsterisk)
        {
            // repair block comment by skipping it
            while (_i < _text.Length && !AtEndOfBlockComment())
            {
                _i++;
            }
            _i += 2;

            return true;
        }

        // find a line comment '// ...'
        if (_text.CharCodeAt(_i) == StringUtils.CodeSlash && _text.CharCodeAt(_i + 1) == StringUtils.CodeSlash)
        {
            // repair line comment by skipping it
            while (_i < _text.Length && _text.CharCodeAt(_i) != StringUtils.CodeNewline)
            {
                _i++;
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// Parses a JSON character.
    /// </summary>
    /// <param name="code">The character code to parse</param>
    /// <returns>True if the character was parsed, false otherwise</returns>
    private static bool ParseCharacter(int code)
    {
        if (_text.CharCodeAt(_i) == code)
        {
            _output += _text.CharCodeAt(_i);
            _i++;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Skips a JSON character.
    /// </summary>
    /// <param name="code">The character code to skip</param>
    /// <returns>True if the character was skipped, false otherwise</returns>
    private static bool SkipCharacter(int code)
    {
        if (_text.CharCodeAt(_i) == code)
        {
            _i++;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Skips a JSON escape character.
    /// </summary>
    /// <returns>True if the escape character was skipped, false otherwise</returns>
    private static bool SkipEscapeCharacter()
    {
        return SkipCharacter(StringUtils.CodeBackslash);
    }

    /// <summary>
    /// Parses a JSON object.
    /// </summary>
    /// <returns>True if an object was parsed, false otherwise</returns>
    private static bool ParseObject()
    {
        if (_text.CharCodeAt(_i) == StringUtils.CodeOpeningBrace)
        {
            _output += "{";
            _i++;
            ParseWhitespaceAndSkipComments();

            bool initial = true;
            while (_i < _text.Length && _text.CharCodeAt(_i) != StringUtils.CodeClosingBrace)
            {
                bool processedComma;
                if (!initial)
                {
                    processedComma = ParseCharacter(StringUtils.CodeComma);
                    if (!processedComma)
                    {
                        // repair missing comma
                        _output = StringUtils.InsertBeforeLastWhitespace(_output, ",");
                    }
                    ParseWhitespaceAndSkipComments();
                }
                else
                {
                    processedComma = true;
                    initial = false;
                }

                bool processedKey = ParseString() || ParseUnquotedString();
                if (!processedKey)
                {
                    if (
                    _text.CharCodeAt(_i) == StringUtils.CodeClosingBrace   ||
                    _text.CharCodeAt(_i) == StringUtils.CodeOpeningBrace   ||
                    _text.CharCodeAt(_i) == StringUtils.CodeClosingBracket ||
                    _text.CharCodeAt(_i) == StringUtils.CodeOpeningBracket ||
                    _text.CharCodeAt(_i) == '\0'
                    )
                    {
                        // repair trailing comma
                        _output = StringUtils.StripLastOccurrence(_output, ",");
                    }
                    else
                    {
                        ThrowObjectKeyExpected();
                    }
                    break;
                }

                ParseWhitespaceAndSkipComments();
                bool processedColon = ParseCharacter(StringUtils.CodeColon);
                if (!processedColon)
                {
                    if (StringUtils.IsStartOfValue(_text.CharCodeAt(_i).ToString()))
                    {
                        // repair missing colon
                        _output = StringUtils.InsertBeforeLastWhitespace(_output, ":");
                    }
                    else
                    {
                        ThrowColonExpected();
                    }
                }
                bool processedValue = ParseValue();
                if (!processedValue)
                {
                    if (processedColon)
                    {
                        // repair missing object value
                        _output += "null";
                    }
                    else
                    {
                        ThrowColonExpected();
                    }
                }
            }

            if (_text.CharCodeAt(_i) == StringUtils.CodeClosingBrace)
            {
                _output += "}";
                _i++;
            }
            else
            {
                // repair missing end bracket
                _output = StringUtils.InsertBeforeLastWhitespace(_output, "}");
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// Parses a JSON array.
    /// </summary>
    /// <returns>True if an array was parsed, false otherwise</returns>
    private static bool ParseArray()
    {
        if (_text.CharCodeAt(_i) == StringUtils.CodeOpeningBracket)
        {
            _output += "[";
            _i++;
            ParseWhitespaceAndSkipComments();

            bool initial = true;
            while (_i < _text.Length && _text.CharCodeAt(_i) != StringUtils.CodeClosingBracket)
            {
                if (!initial)
                {
                    bool processedComma = ParseCharacter(StringUtils.CodeComma);
                    if (!processedComma)
                    {
                        // repair missing comma
                        _output = StringUtils.InsertBeforeLastWhitespace(_output, ",");
                    }
                }
                else
                {
                    initial = false;
                }

                bool processedValue = ParseValue();
                if (!processedValue)
                {
                    // repair trailing comma
                    _output = StringUtils.StripLastOccurrence(_output, ",");
                    break;
                }
            }

            if (_text.CharCodeAt(_i) == StringUtils.CodeClosingBracket)
            {
                _output += "]";
                _i++;
            }
            else
            {
                // repair missing closing array bracket
                _output = StringUtils.InsertBeforeLastWhitespace(_output, "]");
            }

            return true;
        }

        return false;
    }

    // <summary>
    // Parses and repairs Newline Delimited JSON (NDJSON): multiple JSON objects separated by a newline character.
    // </summary>
    private static void ParseNewlineDelimitedJson()
    {
        // repair NDJSON
        bool initial = true;
        bool processedValue = true;
        while (processedValue)
        {
            if (!initial)
            {
                // parse optional comma, insert when missing
                var processedComma = ParseCharacter(StringUtils.CodeComma);
                if (!processedComma)
                {
                    // repair: add missing comma
                    _output = StringUtils.InsertBeforeLastWhitespace(_output, ",");
                }
            }
            else
            {
                initial = false;
            }

            processedValue = ParseValue();
        }

        if (!processedValue)
        {
            // repair: remove trailing comma
            _output = StringUtils.StripLastOccurrence(_output, ",");
        }

        // repair: wrap the output inside array brackets
        _output = $"[\n{_output}\n]";
    }


    /// <summary>
    /// Parses a JSON string.
    /// </summary>
    /// <returns>True if a string was parsed, false otherwise</returns>
    private static bool ParseString()
    {
        bool skipEscapeChars = _text.CharCodeAt(_i) == StringUtils.CodeBackslash;
        if (skipEscapeChars)
        {
            // repair: remove the first escape character
            _i++;
            skipEscapeChars = true;
        }

        if (StringUtils.IsQuote(_text.CharCodeAt(_i)))
        {
            MatchingQuotes.SetStartQuote(_text.CharCodeAt(_i));
            _output += "\"";
            _i++;

            while (_i < _text.Length && !MatchingQuotes.IsMatchingEndQuote(_text.CharCodeAt(_i)))
            {
                if (_text.CharCodeAt(_i) == StringUtils.CodeBackslash)
                {
                    var character = _text.CharCodeAt(_i + 1);
                    var escapeChar = EscapeCharacters.GetValueOrDefault(character);
                    if (escapeChar != null)
                    {
                        _output += _text.Substring(_i, 2);
                        _i += 2;
                    }
                    else if (character == 'u')
                    {
                        if (
                        StringUtils.IsHex(_text.CharCodeAt(_i + 2)) &&
                        StringUtils.IsHex(_text.CharCodeAt(_i + 3)) &&
                        StringUtils.IsHex(_text.CharCodeAt(_i + 4)) &&
                        StringUtils.IsHex(_text.CharCodeAt(_i + 5))
                        )
                        {
                            _output += _text.Substring(_i, 6);
                            _i += 6;
                        }
                        else
                        {
                            ThrowInvalidUnicodeCharacter(_i);
                        }
                    }
                    else
                    {
                        // repair invalid escape character: remove it
                        _output += character;
                        _i += 2;
                    }
                }
                else
                {
                    char character = _text.CharCodeAt(_i);
                    int code = _text.CharCodeAt(_i);

                    if (code == StringUtils.CodeDoubleQuote && _text.CharCodeAt(_i - 1) != StringUtils.CodeBackslash)
                    {
                        // repair unescaped double quote
                        _output += "\\" + character;
                        _i++;
                    }
                    else if (StringUtils.IsControlCharacter(character))
                    {
                        // unescaped control character
                        _output += ControlCharacters[character];
                        _i++;
                    }
                    else
                    {
                        if (!StringUtils.IsValidStringCharacter(code))
                        {
                            ThrowInvalidCharacter(character);
                        }
                        _output += character;
                        _i++;
                    }
                }

                if (skipEscapeChars)
                {
                    bool processed = SkipEscapeCharacter();
                    if (processed)
                    {
                        // repair: skipped escape character (nothing to do)
                    }
                }
            }

            if (StringUtils.IsQuote(_text.CharCodeAt(_i)))
            {
                if (_text.CharCodeAt(_i) != StringUtils.CodeDoubleQuote)
                {
                    // repair non-normalized quote. todo?
                }
                _output += "\"";
                _i++;
            }
            else
            {
                // repair missing end quote
                _output += "\"";
            }

            ParseConcatenatedString();

            return true;
        }

        return false;
    }

    /// <summary>
    /// Parses and repairs concatenated JSON strings in the JSON document.
    /// </summary>
    /// <returns>True if any concatenated strings were parsed and repaired, false otherwise</returns>
    private static bool ParseConcatenatedString()
    {
        bool processed = false;

        ParseWhitespaceAndSkipComments();
        while (_text.CharCodeAt(_i) == StringUtils.CodePlus)
        {
            processed = true;
            _i++;
            ParseWhitespaceAndSkipComments();

            // repair: remove the end quote of the first string
            _output = StringUtils.StripLastOccurrence(_output, "\"", true);
            var start = _output.Length;
            ParseString();

            // repair: remove the start quote of the second string
            _output = StringUtils.RemoveAtIndex(_output, start, 1);
        }

        return processed;
    }

    /// <summary>
    /// Parses a JSON number.
    /// </summary>
    /// <returns>True if a number was parsed, false otherwise</returns>
    private static bool ParseNumber()
    {
        int start = _i;
        if (_text.CharCodeAt(_i) == StringUtils.CodeMinus)
        {
            _i++;
            if (ExpectDigitOrRepair(start))
            {
                return true;
            }
        }

        if (_text.CharCodeAt(_i) == StringUtils.CodeZero)
        {
            _i++;
        }
        else if (StringUtils.IsNonZeroDigit(_text.CharCodeAt(_i)))
        {
            _i++;
            while (StringUtils.IsDigit(_text.CharCodeAt(_i)))
            {
                _i++;
            }
        }

        if (_text.CharCodeAt(_i) == StringUtils.CodeDot)
        {
            _i++;
            if (ExpectDigitOrRepair(start))
            {
                return true;
            }
            while (StringUtils.IsDigit(_text.CharCodeAt(_i)))
            {
                _i++;
            }
        }

        if (_text.CharCodeAt(_i) == StringUtils.CodeLowercaseE || _text.CharCodeAt(_i) == StringUtils.CodeUppercaseE)
        {
            _i++;
            if (_text.CharCodeAt(_i) == StringUtils.CodeMinus || _text.CharCodeAt(_i) == StringUtils.CodePlus)
            {
                _i++;
            }
            if (ExpectDigitOrRepair(start))
            {
                return true;
            }
            while (StringUtils.IsDigit(_text.CharCodeAt(_i)))
            {
                _i++;
            }
        }

        if (_i > start)
        {
            _output += _text.Substring(start, _i - start);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Parses and repairs JSON keywords (true, false, null) in the JSON document.
    /// </summary>
    /// <returns>True if a keyword was parsed and repaired, false otherwise</returns>
    private static bool ParseKeywords()
    {
        return
            ParseKeyword("true", "true")   ||
            ParseKeyword("false", "false") ||
            ParseKeyword("null", "null")   ||
            // repair Python keywords True, False, None
            ParseKeyword("True", "true")   ||
            ParseKeyword("False", "false") ||
            ParseKeyword("None", "null");
    }

    /// <summary>
    /// Parses a specific JSON keyword.
    /// </summary>
    /// <param name="name">The name of the keyword</param>
    /// <param name="value">The repaired value of the keyword</param>
    /// <returns>True if the keyword was parsed and repaired, false otherwise</returns>
    private static bool ParseKeyword(string name, string value)
    {
        if (_text.SubstringSafe(_i, name.Length) == name)
        {
            _output += value;
            _i      += name.Length;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Parses an unquoted JSON string or a function call.
    /// </summary>
    /// <returns>True if an unquoted string or a function call was parsed, false otherwise</returns>
    private static bool ParseUnquotedString()
    {
        // note that the symbol can end with whitespaces: we stop at the next delimiter
        int start = _i;
        while (_i < _text.Length && !StringUtils.IsDelimiter(_text.CharCodeAt(_i).ToString()))
        {
            _i++;
        }

        if (_i > start)
        {
            if (_text.CharCodeAt(_i) == StringUtils.CodeOpenParenthesis)
            {
                // repair a MongoDB function call like NumberLong("2")
                // repair a JSONP function call like callback({...});
                _i++;

                ParseValue();

                if (_text.CharCodeAt(_i) == StringUtils.CodeCloseParenthesis)
                {
                    // repair: skip close bracket of function call
                    _i++;
                    if (_text.CharCodeAt(_i) == StringUtils.CodeSemicolon)
                    {
                        // repair: skip semicolon after JSONP call
                        _i++;
                    }
                }

                return true;
            }
            else
            {
                // repair unquoted string

                // first, go back to prevent getting trailing whitespaces in the string
                while (StringUtils.IsWhitespace(_text.CharCodeAt(_i - 1)) && _i > 0)
                {
                    _i--;
                }

                string symbol = _text.Substring(start, _i - start);
                _output += symbol == "undefined" ? "null" : $"\"{symbol}\"";

                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Parses input text at current position for end of comment
    /// </summary>
    /// <returns>True if an end of block comment, false otherwise</returns>
    private static bool AtEndOfBlockComment()
    {
        return _text.CharCodeAt(_i) == '*' && _text.CharCodeAt(_i + 1) == '/';
    }

    /// <summary>
    /// Throws an error if input text at current position is not a digit
    /// </summary>
    /// <param name="start">Start position of number</param>
    private static void ExpectDigit(int start)
    {
        if (!StringUtils.IsDigit(_text.CharCodeAt(_i)))
        {
            string numSoFar = _text.Substring(start, _i - start);
            throw new JSONRepairError($"Invalid number '{numSoFar}', expecting a digit {Got()}", 2);
        }
    }


    /// <summary>
    /// Parses an number cut off at the end JSON string or a function call.
    /// </summary>
    /// <returns>True if number can be fixed</returns>
    private static bool ExpectDigitOrRepair(int start)
    {
        if (_i >= _text.Length)
        {
            // repair numbers cut off at the end
            // this will only be called when we end after a '.', '-', or 'e' and does not
            // change the number more than it needs to make it valid JSON
            _output += _text.Substring(start, _i - start) + "0";
            return true;
        }
        else
        {
            ExpectDigit(start);
            return false;
        }
    }

    /// <summary>
    /// Throws an invalid character exception
    /// Will be ignored if the ThrowExceptions property is false
    /// </summary>
    /// <param name="character"></param>
    /// <exception cref="JSONRepairError"></exception>
    private static void ThrowInvalidCharacter(char character)
    {
        if (ThrowExceptions) throw new JSONRepairError($"Invalid character {character}", _i);
    }

    /// <summary>
    /// Throws an unexpected character exception
    /// Will be ignored if the ThrowExceptions property is false
    /// </summary>
    /// <exception cref="JSONRepairError"></exception>
    private static void ThrowUnexpectedCharacter()
    {
        if (ThrowExceptions) throw new JSONRepairError($"Unexpected character {_text.CharCodeAt(_i)}", _i);
    }

    /// <summary>
    /// Throws an unexpected end exception
    /// Will be ignored if the ThrowExceptions property is false
    /// </summary>
    /// <exception cref="JSONRepairError"></exception>
    private static void ThrowUnexpectedEnd()
    {
        if (ThrowExceptions) throw new JSONRepairError("Unexpected end of json string", _text.Length);
    }

    /// <summary>
    /// Throws an unexpected object key expected exception
    /// Will be ignored if the ThrowExceptions property is false
    /// </summary>
    /// <exception cref="JSONRepairError"></exception>
    private static void ThrowObjectKeyExpected()
    {
        if (ThrowExceptions) throw new JSONRepairError("Object key expected", _i);
    }

    /// <summary>
    /// Throws an colon expected exception
    /// Will be ignored if the ThrowExceptions property is false
    /// </summary>
    /// <exception cref="JSONRepairError"></exception>
    private static void ThrowColonExpected()
    {
        if (ThrowExceptions) throw new JSONRepairError("Colon expected", _i);
    }

    /// <summary>
    /// Throws an invalid unicode character exception
    /// Will be ignored if the ThrowExceptions property is false
    /// </summary>
    /// <exception cref="JSONRepairError"></exception>
    private static void ThrowInvalidUnicodeCharacter(int start)
    {
        int end = start + 2;
        while (Regex.IsMatch(_text[end].ToString(), @"\w"))
        {
            end++;
        }
        string chars = _text.Substring(start, end - start);
        if (ThrowExceptions) throw new JSONRepairError($"Invalid unicode character \"{chars}\"", _i);
    }

    /// <summary>
    /// Helper function that returns a description of the last gotten character
    /// </summary>
    private static string Got()
    {
        return _text.CharCodeAt(_i) != '\0' ? $"but got '{_text.CharCodeAt(_i)}'" : "but reached end of input";
    }



    public static string GetVersion()
    {
        return Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "";
    }

}

/// <summary>
/// Represents an error that occurs during JSON repair.
/// </summary>
public class JSONRepairError : Exception
{
    /// <summary>
    /// The index at which the error occurred.
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="JSONRepairError"/> class with a specified error message and index.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="index">The index at which the error occurred.</param>
    public JSONRepairError(string message, int index) : base(message)
    {
        Index = index;
    }
}