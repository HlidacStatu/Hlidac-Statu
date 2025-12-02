using HlidacStatu.DS.Api;
using HlidacStatu.Entities;
using HlidacStatu.Repositories;

namespace HlidacStatuApi;

public class BlurredPageProcessor : BackgroundService
{
    private readonly BlurredPageBackgroundQueue _queue;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<BlurredPageProcessor> _logger;

    private long _processedCount = 0;
    private long _failedCount = 0;

    public BlurredPageProcessor(
        BlurredPageBackgroundQueue queue,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<BlurredPageProcessor> logger)
    {
        _queue = queue;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BlurredPageProcessor background service started");

        //yield stuff until canceled
        await foreach (var data in _queue.DequeueAsync(stoppingToken))
        {
            if (stoppingToken.IsCancellationRequested)
                break;

            await ProcessDataAsync(data, stoppingToken);
        }

        _logger.LogInformation(
            "BlurredPageProcessor background service stopped. Processed: {processed}, Failed: {failed}",
            _processedCount,
            _failedCount
        );
    }

    private async Task ProcessDataAsync(BlurredPage.BpSave data, CancellationToken cancellationToken)
    {
        int numOfPages = data.prilohy?.Sum(m => m.pages?.Count() ?? 0) ?? 0;

        _logger.LogInformation(
            "Starting background processing for {smlouvaId} with {pages} pages",
            data.smlouvaId,
            numOfPages
        );

        var sw = Devmasters.DT.StopWatchEx.StartNew();

        try
        {
            // Create a new scope for this work item to get fresh scoped services
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                // If you need scoped services (like DbContext), resolve them here:
                // var dbContext = scope.ServiceProvider.GetRequiredService<YourDbContext>();

                var success = await SaveDataAsync(data);
                if (!success)
                {
                    _logger.LogWarning("First save attempt failed, retrying for {smlouvaId}", data.smlouvaId);
                    success = await SaveDataAsync(data);
                }

                if (success)
                {
                    _ = Interlocked.Increment(ref _processedCount);

                    _logger.LogInformation(
                        "Successfully processed {smlouvaId} with {pages} pages in {duration:F2} sec",
                        data.smlouvaId,
                        numOfPages,
                        sw.Elapsed.TotalSeconds
                    );
                }
                else
                {
                    _ = Interlocked.Increment(ref _failedCount);

                    _logger.LogError(
                        "Failed to process {smlouvaId} after retry",
                        data.smlouvaId
                    );
                }
            }
        }
        catch (Exception ex)
        {
            _ = Interlocked.Increment(ref _failedCount);

            _logger.LogError(
                ex,
                "Exception processing {smlouvaId} with {pages} pages",
                data.smlouvaId,
                numOfPages
            );
        }
    }

    private async Task<bool> SaveDataAsync(BlurredPage.BpSave data)
        {
            List<Task> tasks = new List<Task>();
            List<PageMetadata> pagesMD = new List<PageMetadata>();
            foreach (var p in data.prilohy)
            {
                foreach (var page in p.pages)
                {
                    PageMetadata pm = new PageMetadata();
                    pm.SmlouvaId = data.smlouvaId;
                    pm.PrilohaId = p.uniqueId;
                    pm.PageNum = page.page;
                    pm.Blurred = new PageMetadata.BlurredMetadata()
                    {
                        BlackenAreaBoundaries = page.blackenAreaBoundaries
                            .Select(b => new PageMetadata.BlurredMetadata.Boundary()
                            {
                                X = b.x,
                                Y = b.y,
                                Width = b.width,
                                Height = b.height
                            }
                            ).ToArray(),
                        TextAreaBoundaries = page.textAreaBoundaries
                            .Select(b => new PageMetadata.BlurredMetadata.Boundary()
                            {
                                X = b.x,
                                Y = b.y,
                                Width = b.width,
                                Height = b.height
                            }
                            ).ToArray(),
                        AnalyzerVersion = page.analyzerVersion,
                        Created = DateTime.Now,
                        ImageWidth = page.imageWidth,
                        ImageHeight = page.imageHeight,
                        BlackenArea = page.blackenArea,
                        TextArea = page.textArea
                    };
                    pagesMD.Add(pm);
                    Task t = PageMetadataRepo.SaveAsync(pm);
                    tasks.Add(t);
                    //t.Wait();
                }
            }


            if (pagesMD.Count > 0)
            {
                var sml = await SmlouvaRepo.LoadAsync(data.smlouvaId);
                foreach (var pril in sml.Prilohy)
                {
                    IEnumerable<PageMetadata>? blurredPages = pagesMD.Where(m => m.PrilohaId == pril.UniqueHash());

                    if (blurredPages.Any())
                    {
                        var pb = new Smlouva.Priloha.BlurredPagesStats(blurredPages);
                        pril.BlurredPages = pb;
                    }
                    else
                    {
                        if (pril.BlurredPages != null)
                        {
                            //keep
                        }
                        else
                        {
                            pril.BlurredPages = null;
                        }
                    }
                }
                _ = await SmlouvaRepo.SaveAsync(sml, updateLastUpdateValue: false, skipPrepareBeforeSave: true, fireOCRDone:false);

            }
            try
            {
                Task.WaitAll(tasks.ToArray());
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                    "{action} {code} for {part} exception.",
                    "saving",
                    "thread",
                    "ApiV2BlurredPageController.BpSave"
                    );
                return false;
            }
        }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("BlurredPageProcessor is stopping gracefully");
        await base.StopAsync(cancellationToken);
    }
}