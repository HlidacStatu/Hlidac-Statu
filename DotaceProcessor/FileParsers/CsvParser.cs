using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace DotaceProcessor.FileParsers;

public class CsvParser : IFileParser
{
    public IEnumerable<IDictionary<string, object?>> Parse(Stream fileStream)
    {
        using var reader = new StreamReader(fileStream);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture));
        while (csv.Read())
        {
            var row = new Dictionary<string, object?>();
            for (var i = 0; csv.TryGetField(i, out var value); i++)
            {
                row[$"Column{i}"] = value;
            }
            yield return row;
        }
    }
}