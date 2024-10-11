using System.Text.Json;

namespace DotaceProcessor.FileParsers;

public class JSONParser : IFileParser
{
    public IEnumerable<IDictionary<string, object?>> Parse(Stream fileStream)
    {
        using var reader = new StreamReader(fileStream);
        while (!reader.EndOfStream)
        {
            var jsonLine = reader.ReadLine();
            var row = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonLine);
            yield return row;
        }
    }
}