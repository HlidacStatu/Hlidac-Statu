using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Serilog;

namespace DotaceProcessor.FileParsers;

public class CsvParser : IFileParser
{
    private List<string> Headers { get; set; } = new();
    private ILogger Logger = Log.ForContext(typeof(CsvParser));

    public IEnumerable<IDictionary<string, object?>> Parse(Stream fileStream)
    {
        using var reader = new StreamReader(fileStream);
        var delimiter = DetectDelimiter(reader);
        reader.BaseStream.Seek(0, SeekOrigin.Begin); // Reset stream after detection

        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            Delimiter = delimiter,
            BadDataFound = ctx => Logger.Error($"Bad data found. Data: {ctx.RawRecord}, Field: {ctx.Field}"),
            MissingFieldFound = ctx => Logger.Warning($"Missing field at index {ctx.Index}. Header names: {ctx.HeaderNames}")
        });

        // Parse header and validate
        if (csv.Read())
        {
            csv.ReadHeader();
            Headers = csv.HeaderRecord?.Select(h => h.Trim()).ToList() ?? new List<string>(); // Trim whitespace in headers

            if (Headers.Any(string.IsNullOrWhiteSpace))
            {
                Logger.Error("Empty header names detected.");
                yield break;
            }

            if (Headers.Distinct().Count() != Headers.Count)
            {
                Logger.Error("Duplicate headers found.");
                yield break;
            }
        }

        // Parse data
        while (csv.Read())
        {
            if (IsEmptyRow(csv)) continue; // Skip empty rows

            if (!ValidateRecord(csv)) continue; // Validate for column mismatch

            var row = new Dictionary<string, object?>();
            for (var i = 0; i < Headers.Count; i++)
            {
                var value = csv.GetField(i);
                row[Headers[i]] = string.IsNullOrWhiteSpace(value) ? null : value;
            }

            yield return row;
        }
    }

    private bool IsEmptyRow(CsvReader csv)
    {
        return csv.Parser.Record?.All(string.IsNullOrWhiteSpace) == true;
    }

    private string DetectDelimiter(StreamReader reader)
    {
        var firstLine = reader.ReadLine();
        if (string.IsNullOrWhiteSpace(firstLine)) return ","; // Default to comma if empty

        var delimiters = new[] { ',', ';', '\t', '|' };
        var delimiterCount = delimiters.ToDictionary(d => d, d => firstLine.Count(c => c == d));

        return delimiterCount.Aggregate((x, y) => x.Value > y.Value ? x : y).Key.ToString();
    }

    private bool ValidateRecord(CsvReader csv)
    {
        // Check if the number of fields matches the number of headers
        if (Headers.Count > 0 && csv.Parser.Record?.Length != Headers.Count)
        {
            Logger.Error($"Column mismatch at row {csv.Parser.Row}. Expected {Headers.Count}, found {csv.Parser.Record?.Length}");
            return false;
        }
        return true;
    }
}