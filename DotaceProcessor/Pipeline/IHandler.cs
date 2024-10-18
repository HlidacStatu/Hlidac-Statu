namespace DotaceProcessor.Pipeline;

public interface IHandler
{
    Task HandleAsync(PipelineContext context, HandlerDelegate next);
}