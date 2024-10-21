namespace DotaceProcessor.Pipeline.SubsidyHandlers;

public sealed class ExtractStringValueHandler : BaseHandler
{

    private readonly string[] _columnNames;
    private readonly string _handlerName;
    private readonly bool _isValueMandatory;
    private readonly Action<PipelineContext, string, string?> _propertySetter;

    public ExtractStringValueHandler(string[] columnNames, string handlerName, Action<PipelineContext, string, string?> propertySetter, bool isValueMandatory)
    {
        _columnNames = columnNames;
        _handlerName = handlerName;
        _isValueMandatory = isValueMandatory;
        _propertySetter = propertySetter;
    }
    
    public override async Task HandleAsync(PipelineContext context, HandlerDelegate next)
    {
        bool isValueFound = false;
        foreach (var columnName in _columnNames)
        {
            if (context.Input.TryGetValue(columnName, out var value))
            {
                _propertySetter(context, columnName, Convert.ToString(value));
                isValueFound = true;
                break;
            }
        }

        if (!isValueFound)
        {
            if (_isValueMandatory)
            {
                Logger.Error($"{context.FileName}:{context.RecordNumber}:{_handlerName} - Not found.");
                return;
            }
            
            Logger.Information($"{context.FileName}:{context.RecordNumber}:{_handlerName} - Not found.");
            context.Issues.Add($"{_handlerName} - Not found.");
            
        }
        
        await next(context);
    }
}