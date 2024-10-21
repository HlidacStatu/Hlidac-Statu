using DotaceProcessor.FileParsers;
using DotaceProcessor.Pipeline;
using DotaceProcessor.Pipeline.SubsidyHandlers;
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


        var processMetadataHandler = new ProcessMetadataHandler();



        _pipeline.Use((context, next) => processMetadataHandler.HandleAsync(context, next)) //todo: fix metadata before saving them in this step
            .Use((context, next) => HandlerFactory.ApprovedYearHandler.Value.HandleAsync(context, next))
            .Use((context, next) => HandlerFactory.PayedAmountHandler.Value.HandleAsync(context, next))
            .Use((context, next) => HandlerFactory.ProgramCodeHandler.Value.HandleAsync(context, next))
            .Use((context, next) => HandlerFactory.ProgramNameHandler.Value.HandleAsync(context, next))
            .Use((context, next) => HandlerFactory.ProjectCodeHandler.Value.HandleAsync(context, next))
            .Use((context, next) => HandlerFactory.ProjectNameHandler.Value.HandleAsync(context, next))
            .Use((context, next) => HandlerFactory.RecipientCityHandler.Value.HandleAsync(context, next))
            .Use((context, next) => HandlerFactory.RecipientIcoHandler.Value.HandleAsync(context, next))
            .Use((context, next) => HandlerFactory.RecipientNameHandler.Value.HandleAsync(context, next))
            .Use((context, next) => HandlerFactory.RecipientOkresHandler.Value.HandleAsync(context, next))
            .Use((context, next) => HandlerFactory.RecipientPscHandler.Value.HandleAsync(context, next))
            .Use((context, next) => HandlerFactory.SubsidyAmountHandler.Value.HandleAsync(context, next))
            .Use((context, next) => HandlerFactory.YearOfBirthHandler.Value.HandleAsync(context, next));
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
                        await _pipeline.StartProcessing(row, file, rowNumber);
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