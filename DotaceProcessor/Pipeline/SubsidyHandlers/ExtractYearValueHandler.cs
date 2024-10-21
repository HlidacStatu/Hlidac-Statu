using System.Text.RegularExpressions;

namespace DotaceProcessor.Pipeline.SubsidyHandlers;

public sealed class ExtractYearValueHandler : BaseHandler
{

    private readonly string[] _columnNames;
    private readonly string _handlerName;
    private readonly bool _isValueMandatory;
    private readonly Action<PipelineContext, string, int?> _propertySetter;
    
    private Regex YearRegex = new(@"\b(19|20)\d{2}\b");

    public ExtractYearValueHandler(string[] columnNames, string handlerName, Action<PipelineContext, string, int?> propertySetter, bool isValueMandatory)
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
                var dateString = Convert.ToString(value);

                int? year = YearFromDateString(dateString);
                
                _propertySetter(context, columnName, year);
                isValueFound = year.HasValue;
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

    private int? YearFromDateString(string? dateString)
    {
        if (dateString is null)
            return null;
        
        var match = YearRegex.Match(dateString);
        if (match.Success)
        {
            var year = int.Parse(match.Value);
            return year;
        }

        return null;
    }
}

