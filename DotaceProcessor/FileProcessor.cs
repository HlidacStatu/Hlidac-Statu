using DotaceProcessor.FileParsers;
using DotaceProcessor.Pipeline;

namespace DotaceProcessor;

public class FileProcessor
{
    private readonly IFileParserFactory _parserFactory;
    private readonly ProcessingPipeline _pipeline;

    public FileProcessor()
    {
        _parserFactory = new FileParserFactory();
        _pipeline = new ProcessingPipeline();
    }

    public async Task ProcessFilesAsync(string directoryPath)
    {
        var files = Directory.EnumerateFiles(directoryPath);
        foreach (var file in files)
        {
            var parser = _parserFactory.GetFileParser(file);
            if (parser == null)
            {
                Console.WriteLine($"No parser found for file: {file}");
                continue;
            }

            await using var stream = File.OpenRead(file);
            var rows = parser.Parse(stream);
            int rowNumber = 1;
            foreach (var row in rows)
            {
                try
                {
                    var processedData = await _pipeline.ProcessAsync(file, row, rowNumber);
                    if (processedData != null)
                    {
                        await _pipeline.StoreToElasticsearchAsync(processedData);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing row {rowNumber} in file {file}: {ex.Message}");
                }
                rowNumber++;
            }
        }
    }
}