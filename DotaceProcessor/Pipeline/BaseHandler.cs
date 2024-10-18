using Serilog;

namespace DotaceProcessor.Pipeline;

public abstract class BaseHandler : IHandler
{
    protected ILogger Logger { get; }

    protected BaseHandler()
    {
        Logger = Log.ForContext(GetType());    
    }

    public abstract Task HandleAsync(PipelineContext context, HandlerDelegate next);
}