using HlidacStatu.Entities.Entities;

namespace DotaceProcessor.Pipeline;

public class PipelineContext
{
    public IDictionary<string, object?> Input { get; set; } = new Dictionary<string, object?>();
    public Subsidy Subsidy { get; set; } = new Subsidy();
    public string FileName { get; set; } = string.Empty;
    public int RowNumber { get; set; }
    
}