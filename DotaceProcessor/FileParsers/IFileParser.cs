namespace DotaceProcessor.FileParsers;

public interface IFileParser
{
    IEnumerable<IDictionary<string, object?>> Parse(Stream fileStream);
}