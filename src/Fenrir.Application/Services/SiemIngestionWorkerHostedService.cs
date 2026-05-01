using Fenrir.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Fenrir.Application.Services;

public sealed class SiemIngestionWorkerHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<SiemIngestionWorkerHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan IdleDelay = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan ErrorDelay = TimeSpan.FromSeconds(15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("SIEM ingestion worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var worker = scope.ServiceProvider.GetRequiredService<ISiemIngestionWorker>();
                var processed = await worker.ProcessNextQueuedBatchAsync(stoppingToken);

                if (!processed)
                {
                    await Task.Delay(IdleDelay, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "SIEM ingestion worker failed while processing queued batches.");
                await Task.Delay(ErrorDelay, stoppingToken);
            }
        }

        logger.LogInformation("SIEM ingestion worker stopped.");
    }
}
