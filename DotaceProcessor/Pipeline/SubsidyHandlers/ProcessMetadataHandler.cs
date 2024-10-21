using System.Text.Json;
using DotaceProcessor.Services;

namespace DotaceProcessor.Pipeline.SubsidyHandlers;

public sealed class ProcessMetadataHandler : BaseHandler
{
    readonly IcoFixer _icoFixer = IcoFixer.GetInstance();
    
    public override async Task HandleAsync(PipelineContext context, HandlerDelegate next)
    {
        context.Subsidy.FileName = context.FileName; //todo: fix path in filename so it is unique
        context.Subsidy.Source = context.FileName; //todo: parse correct directory
        context.Subsidy.SourceIco = FillIcoForSource(context.Subsidy.Source, context);
        context.Subsidy.RecordNumber = context.RecordNumber;
        context.Subsidy.ProcessedDate = DateTime.Now;
        context.Subsidy.RawData = JsonSerializer.Serialize(context.Input);

        await next(context);
    }

    private string? FillIcoForSource(string sourceName, PipelineContext context)
    {
        if (string.IsNullOrEmpty(sourceName))
        {
            Logger.Warning($"{context.FileName}:{context.RecordNumber} - No source found.");
            context.Issues.Add("No source found.");
            return null;
        }


        if (_icoFixer.TryFindIcoForName(sourceName, out var ico))
        {
            return ico;
        }
        
        Logger.Warning($"{context.FileName}:{context.RecordNumber} - No ico found.");
        context.Issues.Add($"No ico found.");

        return null;
    }
}