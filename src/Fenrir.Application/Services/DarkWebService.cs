using Fenrir.Application.Abstractions;
using Fenrir.Application.Mapping;
using Fenrir.Contracts;
using Fenrir.Domain.Entities;
using Fenrir.Domain.Enums;

namespace Fenrir.Application.Services;

public sealed class DarkWebService(IDarkWebProvider provider, IFenrirDataStore dataStore) : IDarkWebService
{
    public async Task<DarkWebCheckResponse> CheckAsync(DarkWebCheckRequest request, CancellationToken cancellationToken)
    {
        var query = request.Query.Trim();
        var queryType = request.QueryType.Trim();
        var providerResult = queryType.ToLowerInvariant() switch
        {
            "email" => await provider.CheckEmailAsync(query, cancellationToken),
            "domain" => await provider.CheckDomainAsync(query, cancellationToken),
            "username" => await provider.CheckUsernameAsync(query, cancellationToken),
            _ => new DarkWebProviderResult(false, 0, [])
        };

        var check = new DarkWebCheck
        {
            Query = query,
            QueryType = queryType,
            Exposed = providerResult.Exposed,
            BreachCount = providerResult.BreachCount,
            Sources = providerResult.Sources.ToList()
        };

        await dataStore.AddDarkWebCheckAsync(check, cancellationToken);

        var findings = new List<Finding>();
        if (providerResult.Exposed)
        {
            var finding = new Finding
            {
                Module = "DarkWeb",
                Type = "DarkWebFinding",
                Title = "Exposure found",
                Severity = providerResult.BreachCount >= 3 ? FindingSeverity.High : FindingSeverity.Medium,
                RiskScore = providerResult.BreachCount >= 3 ? 75 : 50,
                Summary = $"{queryType} appeared in {providerResult.BreachCount} breach or exposure source(s).",
                Evidence = string.Join(", ", providerResult.Sources),
                Recommendation = "Notify the owner, rotate affected credentials, and verify whether the exposed identity is still active.",
                RelatedEntityId = check.Id,
                RelatedEntityType = nameof(DarkWebCheck)
            };
            findings.Add(finding);
            await dataStore.AddFindingAsync(finding, cancellationToken);
        }

        return new DarkWebCheckResponse(
            check.Query,
            check.QueryType,
            check.Exposed,
            check.BreachCount,
            check.Sources,
            check.CheckedAtUtc,
            findings.Select(finding => finding.ToDto()).ToArray());
    }
}

public sealed class NoopDarkWebProvider : IDarkWebProvider
{
    public Task<DarkWebProviderResult> CheckEmailAsync(string email, CancellationToken cancellationToken) =>
        Task.FromResult(new DarkWebProviderResult(false, 0, ["No dark-web provider configured"]));

    public Task<DarkWebProviderResult> CheckDomainAsync(string domain, CancellationToken cancellationToken) =>
        Task.FromResult(new DarkWebProviderResult(false, 0, ["No dark-web provider configured"]));

    public Task<DarkWebProviderResult> CheckUsernameAsync(string username, CancellationToken cancellationToken) =>
        Task.FromResult(new DarkWebProviderResult(false, 0, ["No dark-web provider configured"]));
}
