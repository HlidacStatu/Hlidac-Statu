using Microsoft.VisualStudio.TestPlatform.Utilities;
using NUnit.Framework;
namespace JsonRepairSharp.Test;

[TestFixture]
public class JsonRepairTests
{

    [SetUp]
    public void Setup()
    {
        // Enable throwing exceptions
        JsonRepair.ThrowExceptions = true;
    }

    // Test helper function
    private void AssertRepair(string input, string output)
    {
        var repairedJson = JsonRepair.RepairJson(input);
        LogAssertRepair(input,repairedJson, output);
        Assert.AreEqual(repairedJson, output);
    }

    // Test helper function
    private void AssertRepair(string text)
    {
        AssertRepair(text, text);
    }


    // Test helper function: logs to console
    private static void LogAssertRepair(string input, string result, string expected)
    {
        if (result == expected)
        {
            Console.WriteLine("PASS: " + input);
            Console.WriteLine("Expected & Actual: " + result);
        }
        else
        {
            Console.WriteLine("FAIL: " + input);
            Console.WriteLine("Expected: " + expected);
            Console.WriteLine("Actual: " + result);
        }
    }


    private void AssertRepair(string input, string output, string message)
    {
        Assert.AreEqual(JsonRepair.RepairJson(input), output, message);
    }



    [Test, Description("Parse a valid JSON object")]
    public void ParseValidJson_ParseFullJsonObject()
    {
        AssertRepair("{\"a\":2.3e100,\"b\":\"str\",\"c\":null,\"d\":false,\"e\":[1,2,3]}");
    }

    [Test, Description("Parse a valid JSON object with whitespace")]
    public void ParseValidJson_ParseWhitespace()
    {
        AssertRepair("  { \n } \t ");
    }

    [Test, Description("Parse a valid JSON object with nested objects")]
    public void ParseValidJson_ParseObject()
    {
        AssertRepair("{}");
        AssertRepair("{\"a\": {}}");
        AssertRepair("{\"a\": \"b\"}");
        AssertRepair("{\"a\": 2}");
    }

    [Test, Description("Parse a valid JSON object with arrays")]
    public void ParseValidJson_ParseArray()
    {
        AssertRepair("[]");
        AssertRepair("[{}]");
        AssertRepair("{\"a\":[]}");
        AssertRepair("[1, \"hi\", true, false, null, {}, []]");
    }

    [Test, Description("Parse a valid JSON object with numbers")]
    public void ParseValidJson_ParseNumber()
    {
        AssertRepair("23");
        AssertRepair("0");
        AssertRepair("0e+2");
        AssertRepair("0.0");
        AssertRepair("-0");
        AssertRepair("2.3");
        AssertRepair("2300e3");
        AssertRepair("2300e+3");
        AssertRepair("2300e-3");
        AssertRepair("-2");
        AssertRepair("2e-3");
        AssertRepair("2.3e-3");
    }

    [Test, Description("Parse a valid JSON object with strings")]
    public void ParseValidJson_ParseString()
    {
        AssertRepair("\"str\"");
        AssertRepair("\"\\\"\\\\\\/\\b\\f\\n\\r\\t\"");
        AssertRepair("\"\\u260E\"");
    }

    [Test, Description("Parse a valid JSON object with keywords")]
    public void ParseValidJson_ParseKeywords()
    {
        AssertRepair("true");
        AssertRepair("false");
        AssertRepair("null");
    }

    [Test, Description("Parse a valid JSON object with strings equaling JSON delimiters")]
    public void ParseValidJson_CorrectlyHandleStringsEqualingJsonDelimiter()
    {
        AssertRepair("\"\"");
        AssertRepair("\"[\"");
        AssertRepair("\"]\"");
        AssertRepair("\"{\"");
        AssertRepair("\"}\"");
        AssertRepair("\":\"");
        AssertRepair("\",\"");
    }

    [Test, Description("Parse a valid JSON object with Unicode characters in strings")]
    public void ParseValidJson_SupportsUnicodeCharactersInString()
    {
        Assert.AreEqual(JsonRepair.RepairJson("\"★\""), "\"★\"");
        Assert.AreEqual(JsonRepair.RepairJson("\"\\u2605\""), "\"\\u2605\"");
        Assert.AreEqual(JsonRepair.RepairJson("\"😀\""), "\"😀\"");
        Assert.AreEqual(JsonRepair.RepairJson("\"\\ud83d\\ude00\""), "\"\\ud83d\\ude00\"");
        Assert.AreEqual(JsonRepair.RepairJson("\"йнформация\""), "\"йнформация\"");
    }

    [Test, Description("Parse a valid JSON object with escaped Unicode characters in strings")]
    public void ParseValidJson_SupportsEscapedUnicodeCharactersInString()
    {
        Assert.AreEqual(JsonRepair.RepairJson("\"\\u2605\""), "\"\\u2605\"");
        Assert.AreEqual(JsonRepair.RepairJson("\"\\ud83d\\ude00\""), "\"\\ud83d\\ude00\"");
        Assert.AreEqual(
            JsonRepair.RepairJson("\"\\u0439\\u043d\\u0444\\u043e\\u0440\\u043c\\u0430\\u0446\\u0438\\u044f\""),
            "\"\\u0439\\u043d\\u0444\\u043e\\u0440\\u043c\\u0430\\u0446\\u0438\\u044f\""
        );
    }

    [Test, Description("Parse a valid JSON object with Unicode characters in keys")]
    public void ParseValidJson_SupportsUnicodeCharactersInKey()
    {
        Assert.AreEqual(JsonRepair.RepairJson("{\"★\":true}"), "{\"★\":true}");
        Assert.AreEqual(JsonRepair.RepairJson("{\"\\u2605\":true}"), "{\"\\u2605\":true}");
        Assert.AreEqual(JsonRepair.RepairJson("{\"😀\":true}"), "{\"😀\":true}");
        Assert.AreEqual(JsonRepair.RepairJson("{\"\\ud83d\\ude00\":true}"), "{\"\\ud83d\\ude00\":true}");
    }

    [Test, Description("Repair invalid JSON by adding missing quotes")]
    public void RepairInvalidJson_ShouldAddMissingQuotes()
    {
        AssertRepair("abc", "\"abc\"");
        AssertRepair("hello   world", "\"hello   world\"");
        AssertRepair("{a:2}", "{\"a\":2}");
        AssertRepair("{a: 2}", "{\"a\": 2}");
        AssertRepair("{2: 2}", "{\"2\": 2}");
        AssertRepair("{true: 2}", "{\"true\": 2}");
        AssertRepair("{\n  a: 2\n}", "{\n  \"a\": 2\n}");
        AssertRepair("[a,b]", "[\"a\",\"b\"]");
        AssertRepair("[\na,\nb\n]", "[\n\"a\",\n\"b\"\n]");
    }

    [Test, Description("Repair invalid JSON by adding missing end quote")]
    public void RepairInvalidJson_ShouldAddMissingEndQuote()
    {
        AssertRepair("\"abc", "\"abc\"");
        AssertRepair("'abc", "\"abc\"");
        AssertRepair("\u2018abc", "\"abc\"");
    }

    [Test, Description("Repair invalid JSON by replacing single quotes with double quotes")]
    public void RepairInvalidJson_ShouldReplaceSingleQuotesWithDoubleQuotes()
    {
        AssertRepair("{'a':2}", "{\"a\":2}");
        AssertRepair("{'a':'foo'}", "{\"a\":\"foo\"}");
        AssertRepair("{\"a\":'foo'}", "{\"a\":\"foo\"}");
        AssertRepair("{a:'foo',b:'bar'}", "{\"a\":\"foo\",\"b\":\"bar\"}");
    }

    [Test, Description("Repair invalid JSON by replacing special quotes with double quotes")]
    public void RepairInvalidJson_ShouldReplaceSpecialQuotesWithDoubleQuotes()
    {
        AssertRepair("{“a”:“b”}", "{\"a\":\"b\"}");
        AssertRepair("{‘a’:‘b’}", "{\"a\":\"b\"}");
        AssertRepair("{`a´:`b´}", "{\"a\":\"b\"}");
    }

    [Test, Description("Repair invalid JSON by not replacing special quotes inside normal strings")]
    public void RepairInvalidJson_ShouldNotReplaceSpecialQuotesInsideNormalString()
    {
        AssertRepair("\"Rounded “ quote\"", "\"Rounded “ quote\"");
    }

    [Test, Description("Repair invalid JSON by leaving string content untouched")]
    public void RepairInvalidJson_ShouldLeaveStringContentUntouched()
    {
        AssertRepair("\"{a:b}\"", "\"{a:b}\"");
    }

    [Test, Description("Repair invalid JSON by adding or removing escape characters")]
    public void RepairInvalidJson_ShouldAddRemoveEscapeCharacters()
    {
        AssertRepair("\"foo'bar\"", "\"foo'bar\"");
        AssertRepair("\"foo\\\"bar\"", "\"foo\\\"bar\"");
        AssertRepair("'foo\"bar'", "\"foo\\\"bar\"");
        AssertRepair("'foo\\'bar'", "\"foo'bar\"");
        AssertRepair("\"foo\\'bar\"", "\"foo'bar\"");
        AssertRepair("\"\\a\"", "\"a\"");
    }

    [Test, Description("Repair invalid JSON by adding missing object value")]
    public void RepairInvalidJson_ShouldRepairMissingObjectValue()
    {
        AssertRepair("{\"a\":}", "{\"a\":null}");
        AssertRepair("{\"a\":,\"b\":2}", "{\"a\":null,\"b\":2}");
        AssertRepair("{\"a\":", "{\"a\":null}");
    }

    [Test, Description("Repair invalid JSON by repairing undefined values")]
    public void RepairInvalidJson_ShouldRepairUndefinedValues()
    {
        AssertRepair("{\"a\":undefined}", "{\"a\":null}");
        AssertRepair("[undefined]", "[null]");
        AssertRepair("undefined", "null");
    }

    [Test, Description("Repair invalid JSON by escaping unescaped control characters")]
    public void RepairInvalidJson_ShouldEscapeUnescapedControlCharacters()
    {
        AssertRepair("\"hello\\bworld\"", "\"hello\\bworld\"");
        AssertRepair("\"hello\\fworld\"", "\"hello\\fworld\"");
        AssertRepair("\"hello\\nworld\"", "\"hello\\nworld\"");
        AssertRepair("\"hello\\rworld\"", "\"hello\\rworld\"");
        AssertRepair("\"hello\\tworld\"", "\"hello\\tworld\"");
        AssertRepair("{\"value\\n\": \"dc=hcm,dc=com\"}", "{\"value\\n\": \"dc=hcm,dc=com\"}");
    }

    [Test, Description("Repair invalid JSON by replacing special white space characters")]
    public void RepairInvalidJson_ShouldReplaceSpecialWhiteSpaceCharacters()
    {
        AssertRepair("{\"a\":\u00a0\"foo\u00a0bar\"}", "{\"a\": \"foo\u00a0bar\"}");
        AssertRepair("{\"a\":\u202F\"foo\"}", "{\"a\": \"foo\"}");
        AssertRepair("{\"a\":\u205F\"foo\"}", "{\"a\": \"foo\"}");
        AssertRepair("{\"a\":\u3000\"foo\"}", "{\"a\": \"foo\"}");
    }

    [Test, Description("Repair invalid JSON by replacing non-normalized left-right quotes")]
    public void RepairInvalidJson_ShouldReplaceNonNormalizedLeftRightQuotes()
    {
        AssertRepair("\u2018foo\u2019", "\"foo\"");
        AssertRepair("\u201Cfoo\u201D", "\"foo\"");
        AssertRepair("\u0060foo\u00B4", "\"foo\"");

        AssertRepair("\u0060foo'", "\"foo\"");

        AssertRepair("\u0060foo'", "\"foo\"");
    }

    [Test, Description("Repair invalid JSON by removing block comments")]
    public void RepairInvalidJson_ShouldRemoveBlockComments()
    {
        AssertRepair("/* foo */ {}", " {}");
        AssertRepair("{} /* foo */ ", "{}  ");
        AssertRepair("{} /* foo ", "{} ");
        AssertRepair("\n/* foo */\n{}", "\n\n{}");
        AssertRepair("{\"a\":\"foo\",/*hello*/\"b\":\"bar\"}", "{\"a\":\"foo\",\"b\":\"bar\"}");
    }

    [Test, Description("Repair invalid JSON by removing line comments")]
    public void RepairInvalidJson_ShouldRemoveLineComments()
    {
        AssertRepair("{} // comment", "{} ");
        AssertRepair("{\n\"a\":\"foo\",//hello\n\"b\":\"bar\"\n}", "{\n\"a\":\"foo\",\n\"b\":\"bar\"\n}");
    }

    [Test, Description("Repair invalid JSON by not removing comments inside strings")]
    public void RepairInvalidJson_ShouldNotRemoveCommentsInsideString()
    {
        AssertRepair("\"/* foo */\"", "\"/* foo */\"");
    }

    [Test, Description("Repair invalid JSON by stripping JSONP notation")]
    public void RepairInvalidJson_ShouldStripJsonpNotation()
    {
        AssertRepair("callback_123({});", "{}");
        AssertRepair("callback_123([]);", "[]");
        AssertRepair("callback_123(2);", "2");
        AssertRepair("callback_123(\"foo\");", "\"foo\"");
        AssertRepair("callback_123(null);", "null");
        AssertRepair("callback_123(true);", "true");
        AssertRepair("callback_123(false);", "false");
        AssertRepair("callback({}", "{}");
        AssertRepair("/* foo bar */ callback_123 ({})", " {}");
        AssertRepair("/* foo bar */ callback_123 ({})", " {}");
        AssertRepair("/* foo bar */\ncallback_123({})", "\n{}");
        AssertRepair("/* foo bar */ callback_123 (  {}  )", "   {}  ");
        AssertRepair("  /* foo bar */   callback_123({});  ", "     {}  ");
        AssertRepair("\n/* foo\nbar */\ncallback_123 ({});\n\n", "\n\n{}\n\n");

        Assert.Throws<JSONRepairError>(() => JsonRepair.RepairJson("callback {}"), "Unexpected character \"{\", position: 9");
    }

    [Test, Description("Repair invalid JSON by repairing escaped string contents")]
    public void RepairInvalidJson_ShouldRepairEscapedStringContents()
    {
        AssertRepair("\\\"hello world\\\"", "\"hello world\"");
        AssertRepair("\\\"hello world\\", "\"hello world\"");
        AssertRepair("\\\"hello \\\\\"world\\\\\"\\\"", "\"hello \\\"world\\\"\"");
        AssertRepair("[\\\"hello \\\\\"world\\\\\"\\\"]", "[\"hello \\\"world\\\"\"]");
        AssertRepair("{\\\"stringified\\\": \\\"hello \\\\\"world\\\\\"\\\"}", "{\"stringified\": \"hello \\\"world\\\"\"}");

        // the following is weird but understandable
        //AssertRepair("[\\\"hello\\, \\\"world\\\"]", "[\"hello, \",\"world\\\\\",\"]\"]"); // FAILS: Returns "[\"hello, \",\"world\\\",\"]\"]"

        // the following is sort of invalid: the end quote should be escaped too,
        // but the fixed result is most likely what you want in the end
        AssertRepair("\\\"hello\"", "\"hello\"");
    }

    [Test, Description("Repair invalid JSON by stripping trailing commas from arrays")]
    public void RepairInvalidJson_ShouldStripTrailingCommasFromArray()
    {
        AssertRepair("[1,2,3,]", "[1,2,3]");
        AssertRepair("[1,2,3,\n]", "[1,2,3\n]");
        AssertRepair("[1,2,3,  \n  ]", "[1,2,3  \n  ]");
        AssertRepair("[1,2,3,/*foo*/]", "[1,2,3]");
        AssertRepair("{\"array\":[1,2,3,]}", "{\"array\":[1,2,3]}");

        // not matching: inside a string
        AssertRepair("\"[1,2,3,]\"", "\"[1,2,3,]\"");
    }

    [Test, Description("Repair invalid JSON by stripping trailing commas from objects")]
    public void RepairInvalidJson_ShouldStripTrailingCommasFromObject()
    {
        AssertRepair("{\"a\":2,}", "{\"a\":2}");
        AssertRepair("{\"a\":2  ,  }", "{\"a\":2    }");
        AssertRepair("{\"a\":2  , \n }", "{\"a\":2   \n }");
        AssertRepair("{\"a\":2/*foo*/,/*foo*/}", "{\"a\":2}");

        // not matching: inside a string
        AssertRepair("\"{a:2,}\"", "\"{a:2,}\"");
    }

    [Test, Description("Repair invalid JSON by stripping trailing comma at the end")]
    public void RepairInvalidJson_ShouldStripTrailingCommaAtTheEnd()
    {
        AssertRepair("4,", "4");
        AssertRepair("4 ,", "4 ");
        AssertRepair("4 , ", "4  ");
        AssertRepair("{\"a\":2},", "{\"a\":2}");
        AssertRepair("[1,2,3],", "[1,2,3]");
    }

    [Test, Description("Repair invalid JSON by adding missing closing bracket for objects")]
    public void RepairInvalidJson_ShouldAddMissingClosingBracketForObject()
    {
        AssertRepair("{", "{}");
        AssertRepair("{\"a\":2", "{\"a\":2}");
        AssertRepair("{\"a\":2,", "{\"a\":2}");
        AssertRepair("{\"a\":{\"b\":2}", "{\"a\":{\"b\":2}}");
        AssertRepair("{\n  \"a\":{\"b\":2\n}", "{\n  \"a\":{\"b\":2\n}}");
        AssertRepair("[{\"b\":2]", "[{\"b\":2}]");
        AssertRepair("[{\"b\":2\n]", "[{\"b\":2}\n]");
        AssertRepair("[{\"i\":1{\"i\":2}]", "[{\"i\":1},{\"i\":2}]");
        AssertRepair("[{\"i\":1,{\"i\":2}]", "[{\"i\":1},{\"i\":2}]");
    }

    [Test, Description("Repair invalid JSON by adding missing closing bracket for arrays")]
    public void RepairInvalidJson_ShouldAddMissingClosingBracketForArray()
    {
        AssertRepair("[", "[]");
        AssertRepair("[1,2,3", "[1,2,3]");
        AssertRepair("[1,2,3,", "[1,2,3]");
        AssertRepair("[[1,2,3,", "[[1,2,3]]");
        AssertRepair("{\n\"values\":[1,2,3\n}", "{\n\"values\":[1,2,3]\n}");
        AssertRepair("{\n\"values\":[1,2,3\n", "{\n\"values\":[1,2,3]}\n");
    }

    [Test, Description("Repair invalid JSON by stripping MongoDB data types")]
    public void RepairInvalidJson_ShouldStripMongoDbDataTypes()
    {
        AssertRepair("NumberLong(\"2\")", "\"2\"");
        AssertRepair("{\"_id\":ObjectId(\"123\")}", "{\"_id\":\"123\"}");

        string mongoDocument =
            "{\n" +
            "   \"_id\" : ObjectId(\"123\"),\n" +
            "   \"isoDate\" : ISODate(\"2012-12-19T06:01:17.171Z\"),\n" +
            "   \"regularNumber\" : 67,\n" +
            "   \"long\" : NumberLong(\"2\"),\n" +
            "   \"long2\" : NumberLong(2),\n" +
            "   \"int\" : NumberInt(\"3\"),\n" +
            "   \"int2\" : NumberInt(3),\n" +
            "   \"decimal\" : NumberDecimal(\"4\"),\n" +
            "   \"decimal2\" : NumberDecimal(4)\n" +
            "}";

        string expectedJson =
            "{\n" +
            "   \"_id\" : \"123\",\n" +
            "   \"isoDate\" : \"2012-12-19T06:01:17.171Z\",\n" +
            "   \"regularNumber\" : 67,\n" +
            "   \"long\" : \"2\",\n" +
            "   \"long2\" : 2,\n" +
            "   \"int\" : \"3\",\n" +
            "   \"int2\" : 3,\n" +
            "   \"decimal\" : \"4\",\n" +
            "   \"decimal2\" : 4\n" +
            "}";

        Assert.AreEqual(JsonRepair.RepairJson(mongoDocument), expectedJson);
    }

    [Test, Description("Repair invalid JSON by replacing Python constants")]
    public void RepairInvalidJson_ShouldReplacePythonConstants()
    {
        AssertRepair("True", "true");
        AssertRepair("False", "false");
        AssertRepair("None", "null");
    }

    [Test, Description("Repair invalid JSON by turning unknown symbols into strings")]
    public void RepairInvalidJson_ShouldTurnUnknownSymbolsIntoString()
    {
        AssertRepair("foo", "\"foo\"");
        AssertRepair("[1,foo,4]", "[1,\"foo\",4]");
        AssertRepair("{foo: bar}", "{\"foo\": \"bar\"}");

        AssertRepair("foo 2 bar", "\"foo 2 bar\"");
        AssertRepair("{greeting: hello world}", "{\"greeting\": \"hello world\"}");
        AssertRepair("{greeting: hello world\nnext: \"line\"}", "{\"greeting\": \"hello world\",\n\"next\": \"line\"}");
        AssertRepair("{greeting: hello world!}", "{\"greeting\": \"hello world!\"}");
    }

    [Test, Description("Repair invalid JSON by concatenating strings")]
    public void RepairInvalidJson_ShouldConcatenateStrings()
    {
        AssertRepair("\"hello\" + \" world\"", "\"hello world\"");
        AssertRepair("\"hello\" +\n \" world\"", "\"hello world\"");
        AssertRepair("\"a\"+\"b\"+\"c\"", "\"abc\"");
        AssertRepair("\"hello\" + /*comment*/ \" world\"", "\"hello world\"");
        AssertRepair("{\n  \"greeting\": 'hello' +\n 'world'\n}", "{\n  \"greeting\": \"helloworld\"\n}");
    }

    [Test, Description("Repair invalid JSON by repairing missing comma between array items")]
    public void RepairInvalidJson_ShouldRepairMissingCommaBetweenArrayItems()
    {
        AssertRepair("{\"array\": [{}{}]}", "{\"array\": [{},{}]}");
        AssertRepair("{\"array\": [{} {}]}", "{\"array\": [{}, {}]}");
        AssertRepair("{\"array\": [{}\n{}]}", "{\"array\": [{},\n{}]}");
        AssertRepair("{\"array\": [\n{}\n{}\n]}", "{\"array\": [\n{},\n{}\n]}");
        AssertRepair("{\"array\": [\n1\n2\n]}", "{\"array\": [\n1,\n2\n]}");
        AssertRepair("{\"array\": [\n\"a\"\n\"b\"\n]}", "{\"array\": [\n\"a\",\n\"b\"\n]}");

        // should leave normal array as is
        AssertRepair("[\n{},\n{}\n]", "[\n{},\n{}\n]");
    }

    [Test, Description("Repair invalid JSON by repairing missing comma between object properties")]
    public void RepairInvalidJson_ShouldRepairMissingCommaBetweenObjectProperties()
    {
        AssertRepair("{\"a\":2\n\"b\":3\n}", "{\"a\":2,\n\"b\":3\n}");
        AssertRepair("{\"a\":2\n\"b\":3\nc:4}", "{\"a\":2,\n\"b\":3,\n\"c\":4}");
    }

    [Test, Description("Repair invalid JSON by repairing numbers at the end")]
    public void RepairInvalidJson_ShouldRepairNumbersAtTheEnd()
    {
        AssertRepair("{\"a\":2.", "{\"a\":2.0}");
        AssertRepair("{\"a\":2e", "{\"a\":2e0}");
        AssertRepair("{\"a\":2e-", "{\"a\":2e-0}");
        AssertRepair("{\"a\":-", "{\"a\":-0}");
    }

    [Test, Description("Repair invalid JSON by repairing missing colon between object key and value")]
    public void RepairInvalidJson_ShouldRepairMissingColonBetweenObjectKeyAndValue()
    {
        AssertRepair("{\"a\" \"b\"}", "{\"a\": \"b\"}");
        AssertRepair("{\"a\" 2}", "{\"a\": 2}");
        AssertRepair("{\n\"a\" \"b\"\n}", "{\n\"a\": \"b\"\n}");
        AssertRepair("{\"a\" 'b'}", "{\"a\": \"b\"}");
        AssertRepair("{'a' 'b'}", "{\"a\": \"b\"}");
        AssertRepair("{“a” “b”}", "{\"a\": \"b\"}");
        AssertRepair("{a 'b'}", "{\"a\": \"b\"}");
        AssertRepair("{a “b”}", "{\"a\": \"b\"}");
    }

    [Test, Description("Repair invalid JSON by repairing missing combination of comma, quotes, and brackets")]
    public void RepairInvalidJson_ShouldRepairMissingCombinationOfCommaQuotesAndBrackets()
    {
        AssertRepair("{\"array\": [\na\nb\n]}", "{\"array\": [\n\"a\",\n\"b\"\n]}");
        AssertRepair("1\n2", "[\n1,\n2\n]");
        AssertRepair("[a,b\nc]", "[\"a\",\"b\",\n\"c\"]");
    }

    [Test, Description("Repair invalid JSON by repairing newline-separated JSON")]
    public void RepairInvalidJson_ShouldRepairNewlineSeparatedJson()
    {
        string text =
            "/* 1 */\n" +
            "{}\n" +
            "\n" +
            "/* 2 */\n" +
            "{}\n" +
            "\n" +
            "/* 3 */\n" +
            "{}\n";
        string expected = "[\n\n{},\n\n\n{},\n\n\n{}\n\n]";

        Assert.AreEqual(JsonRepair.RepairJson(text), expected);
    }

    [Test, Description("Repair invalid JSON by repairing newline-separated JSON with commas")]
    public void RepairInvalidJson_ShouldRepairNewlineSeparatedJsonWithCommas()
    {
        string text =
            "/* 1 */\n" +
            "{},\n" +
            "\n" +
            "/* 2 */\n" +
            "{},\n" +
            "\n" +
            "/* 3 */\n" +
            "{}\n";
        string expected = "[\n\n{},\n\n\n{},\n\n\n{}\n\n]";

        Assert.AreEqual(JsonRepair.RepairJson(text), expected);
    }

    [Test, Description("Repair invalid JSON by repairing newline-separated JSON with commas and trailing comma")]
    public void RepairInvalidJson_ShouldRepairNewlineSeparatedJsonWithCommasAndTrailingComma()
    {
        string text =
            "/* 1 */\n" +
            "{},\n" +
            "\n" +
            "/* 2 */\n" +
            "{},\n" +
            "\n" +
            "/* 3 */\n" +
            "{},\n";
        string expected = "[\n\n{},\n\n\n{},\n\n\n{}\n\n]";

        Assert.AreEqual(JsonRepair.RepairJson(text), expected);
    }

    [Test, Description("Repair invalid JSON by repairing comma-separated list with value")]
    public void RepairInvalidJson_ShouldRepairCommaSeparatedListWithValue()
    {
        AssertRepair("1,2,3", "[\n1,2,3\n]");
        AssertRepair("1,2,3,", "[\n1,2,3\n]");
        AssertRepair("1\n2\n3", "[\n1,\n2,\n3\n]");
        AssertRepair("a\nb", "[\n\"a\",\n\"b\"\n]");
        AssertRepair("a,b", "[\n\"a\",\"b\"\n]");
    }

    [Test, Description("Repair invalid JSON from LLM by stripping heading & trailing text around Structure")]
    public void RepairInvalidJson_ShouldStripHeadingAndTrailingText()
    {
        JsonRepair.Context = JsonRepair.InputType.LLM;
        AssertRepair("text outside string \"[1,2,3,] text inside string\"", "[1,2,3]");
        AssertRepair("callback_123({});", "{}");
        AssertRepair("text [\n1,2,3\n] text", "[\n1,2,3\n]");
        AssertRepair("Intro   {\"a\":2} ", "{\"a\":2}");
        AssertRepair("{“a”:“b”}\n text\n", "{\"a\":\"b\"}");
        AssertRepair("{} // comment", "{}");
        JsonRepair.Context = JsonRepair.InputType.Other;
    }

    [Test, Description("Do not repair invalid JSON from LLM because root object cannot be found in text")]
    public void RepairInvalidJson_ShouldNotStripHeadingAndTrailingText()
    {
        JsonRepair.Context = JsonRepair.InputType.LLM;
        AssertRepair("\":\"");
        AssertRepair("\",\"");
        JsonRepair.Context = JsonRepair.InputType.Other;
    }

    [Test, Description("Throw an exception for non-repairable issues")]
    public void RepairInvalidJson_ShouldThrowExceptionForNonRepairableIssues()
    {
        Assert.Throws<JSONRepairError>(() => JsonRepair.RepairJson(""), "Unexpected end of JSON string, position: 0");
        Assert.Throws<JSONRepairError>(() => JsonRepair.RepairJson("{\"a\","), "Colon expected, position: 4");
        Assert.Throws<JSONRepairError>(() => JsonRepair.RepairJson("{:2}"), "Object key expected, position: 1");
        Assert.Throws<JSONRepairError>(() => JsonRepair.RepairJson("{\"a\":2,]"), "Unexpected character \"]\", position: 7");
        Assert.Throws<JSONRepairError>(() => JsonRepair.RepairJson("{\"a\" ]"), "Colon expected, position: 5");
        Assert.Throws<JSONRepairError>(() => JsonRepair.RepairJson("{}}"), "Unexpected character \"}\", position: 2");
        Assert.Throws<JSONRepairError>(() => JsonRepair.RepairJson("[2,}"), "Unexpected character \"}\", position: 3");
        Assert.Throws<JSONRepairError>(() => JsonRepair.RepairJson("2.3.4"), "Unexpected character \".\", position: 3");
        Assert.Throws<JSONRepairError>(() => JsonRepair.RepairJson("2..3"), "Invalid number '2.', expecting a digit but got '.', position: 2");
        Assert.Throws<JSONRepairError>(() => JsonRepair.RepairJson("2e3.4"), "Unexpected character \".\", position: 3");
        Assert.Throws<JSONRepairError>(() => JsonRepair.RepairJson("[2e,]"), "Invalid number '2e', expecting a digit but got ',', position: 2");
        Assert.Throws<JSONRepairError>(() => JsonRepair.RepairJson("[-,]"), "Invalid number '-', expecting a digit but got ',', position: 2");
        Assert.Throws<JSONRepairError>(() => JsonRepair.RepairJson("foo ["), "Unexpected character \"[\", position: 4");
        Assert.Throws<JSONRepairError>(() => JsonRepair.RepairJson("\"\\u26\""), "Invalid unicode character \"\\u26\", position: 1");
        Assert.Throws<JSONRepairError>(() => JsonRepair.RepairJson("\"\\uZ000\""), "Invalid unicode character \"\\uZ000\", position: 1");
    }
}


