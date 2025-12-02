using System.Threading.Channels;
using HlidacStatu.DS.Api;

namespace HlidacStatuApi;

public class BlurredPageBackgroundQueue
{
    private readonly Channel<BlurredPage.BpSave> _queue;
    private readonly ILogger<BlurredPageBackgroundQueue> _logger;

    public BlurredPageBackgroundQueue(ILogger<BlurredPageBackgroundQueue> logger, int capacity = 200)
    {
        _logger = logger;
            
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        _queue = Channel.CreateBounded<BlurredPage.BpSave>(options);
    }

    public async ValueTask QueueWorkAsync(BlurredPage.BpSave data, CancellationToken cancellationToken = default)
    {
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        await _queue.Writer.WriteAsync(data, cancellationToken);
            
        _logger.LogInformation(
            "Queued blurred page work for {smlouvaId} with {pageCount} pages",
            data.smlouvaId,
            data.prilohy?.Sum(p => p.pages?.Count() ?? 0) ?? 0
        );
    }

    public IAsyncEnumerable<BlurredPage.BpSave> DequeueAsync(CancellationToken cancellationToken)
    {
        return _queue.Reader.ReadAllAsync(cancellationToken);
    }

    public int GetQueuedItemCount()
    {
        return _queue.Reader.Count;
    }
}