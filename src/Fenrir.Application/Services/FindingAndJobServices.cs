using Fenrir.Application.Abstractions;
using Fenrir.Application.Mapping;
using Fenrir.Contracts;

namespace Fenrir.Application.Services;

public sealed class FindingService(IFenrirDataStore dataStore) : IFindingService
{
    public async Task<IReadOnlyList<FindingDto>> ListAsync(CancellationToken cancellationToken)
    {
        var findings = await dataStore.ListFindingsAsync(cancellationToken);
        return findings.Select(finding => finding.ToDto()).ToArray();
    }

    public async Task<FindingDto?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var finding = await dataStore.GetFindingAsync(id, cancellationToken);
        return finding?.ToDto();
    }

    public async Task<FindingDto?> UpdateStatusAsync(Guid id, string status, CancellationToken cancellationToken)
    {
        var finding = await dataStore.GetFindingAsync(id, cancellationToken);
        if (finding is null)
        {
            return null;
        }

        finding.Status = status.Trim();
        await dataStore.UpdateFindingAsync(finding, cancellationToken);
        return finding.ToDto();
    }
}

public sealed class JobService(IFenrirDataStore dataStore) : IJobService
{
    public async Task<IReadOnlyList<JobDto>> ListAsync(CancellationToken cancellationToken)
    {
        var jobs = await dataStore.ListJobsAsync(cancellationToken);
        return jobs.Select(job => job.ToDto()).ToArray();
    }

    public async Task<JobDto?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var job = await dataStore.GetJobAsync(id, cancellationToken);
        return job?.ToDto();
    }
}
