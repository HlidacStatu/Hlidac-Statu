using DotaceProcessor.FileParsers;
using DotaceProcessor.Pipeline;
using Serilog;

namespace DotaceProcessor;

public class FileProcessor
{
    private readonly IFileParserFactory _parserFactory;
    private readonly ProcessingPipeline _pipeline;
    
    private ILogger Logger { get; } = Log.ForContext<FileProcessor>();

    public FileProcessor()
    {
        _parserFactory = new FileParserFactory();
        _pipeline = new ProcessingPipeline();
    }

    public async Task ProcessFilesAsync(string rootDirectoryPath)
    {
        foreach (var directory in Directory.EnumerateDirectories(rootDirectoryPath))
        {
            var files = Directory.EnumerateFiles(directory);
            foreach (var file in files)
            {
                var parser = _parserFactory.GetFileParser(file);
                if (parser == null)
                {
                    Logger.Error($"No parser found for file: {file}. If the file is an old binary .xls file, just export it to csv file(for a good reason).");
                    continue;
                }

                await using var stream = File.OpenRead(file);
                var rows = parser.Parse(stream);
                int rowNumber = 1;
                foreach (var row in rows)
                {
                    try
                    {
                        Console.WriteLine(row.Count);
                        //await _pipeline.StartProcessing(row, file, rowNumber);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, $"Error processing row {rowNumber} in file {file}: {ex.Message}");
                    }
                    rowNumber++;
                }
            }    
        }
    }
}