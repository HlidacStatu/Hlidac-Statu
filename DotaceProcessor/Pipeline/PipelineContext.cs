using HlidacStatu.Entities.Entities;

namespace DotaceProcessor.Pipeline;

public class PipelineContext
{
    public IDictionary<string, object> Input { get; set; } = new Dictionary<string, object>();
    public Subsidy Subsidy { get; set; } = new Subsidy();
    
}