namespace DotaceProcessor.FileParsers;

public class FileParserFactory : IFileParserFactory
{
    public IFileParser GetFileParser(string filePath)
    {
        if (filePath.EndsWith(".csv"))
        {
            return new CsvParser();
        }
        if (filePath.EndsWith(".json"))
        {
            return new JSONParser();
        }
        if (filePath.EndsWith(".xls") || filePath.EndsWith(".xlsx"))
        {
            return new ExcelParser();
        }
        return null;
    }
}