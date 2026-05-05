using Fenrir.Contracts;

namespace Fenrir.Application.Abstractions;

public interface IAgentPackageBuilder
{
    Task<AgentPackageBuildResult> BuildPackageAsync(AgentBuildRequest request, CancellationToken cancellationToken);
}

public sealed record AgentPackageBuildResult(string FileName, byte[] Content);
