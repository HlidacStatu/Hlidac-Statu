namespace DotaceProcessor.FileParsers;

public interface IFileParserFactory
{
    IFileParser GetFileParser(string filePath);
}