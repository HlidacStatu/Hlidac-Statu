namespace DotaceProcessor.Pipeline;

public class ProcessingPipeline
{
    private readonly IList<Func<HandlerDelegate, HandlerDelegate>> _components =
        new List<Func<HandlerDelegate, HandlerDelegate>>();

    public ProcessingPipeline Use(Func<HandlerDelegate, HandlerDelegate> middleware)
    {
        _components.Add(middleware);
        return this;
    }
    
    public ProcessingPipeline Use(Func<PipelineContext, HandlerDelegate, Task> middleware)
    {
        return Use(next => context => middleware(context, next));
    }

    public async Task StartProcessing(IDictionary<string, object?> input, string filename, int rowNumber)
    {
        HandlerDelegate next = _ => Task.CompletedTask; // Initial "next" does nothing
        foreach (var component in _components.Reverse())
        {
            next = component(next);
        }

        PipelineContext context = new()
        {
            Input = input,
            FileName = filename,
            RecordNumber = rowNumber
        };

        await next(context);
    }
}