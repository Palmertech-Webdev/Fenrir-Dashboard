using Fenrir.Application.Abstractions;
using Quartz;

namespace Fenrir.Infrastructure.Jobs;

public sealed class QuartzNetworkScanScheduler(ISchedulerFactory schedulerFactory) : IBackgroundJobScheduler
{
    public async Task ScheduleNetworkScanAsync(Guid scanId, Guid jobRecordId, CancellationToken cancellationToken)
    {
        var scheduler = await schedulerFactory.GetScheduler(cancellationToken);
        var quartzJob = JobBuilder.Create<NetworkScanQuartzJob>()
            .WithIdentity($"network-scan-{scanId}")
            .UsingJobData("scanId", scanId.ToString())
            .UsingJobData("jobRecordId", jobRecordId.ToString())
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity($"network-scan-trigger-{scanId}")
            .StartNow()
            .Build();

        await scheduler.ScheduleJob(quartzJob, trigger, cancellationToken);
    }
}

public sealed class NetworkScanQuartzJob(INetworkScanExecutor executor) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var scanId = Guid.Parse(context.MergedJobDataMap.GetString("scanId") ?? throw new InvalidOperationException("Missing scanId."));
        var jobRecordId = Guid.Parse(context.MergedJobDataMap.GetString("jobRecordId") ?? throw new InvalidOperationException("Missing jobRecordId."));
        await executor.ExecuteAsync(scanId, jobRecordId, context.CancellationToken);
    }
}
