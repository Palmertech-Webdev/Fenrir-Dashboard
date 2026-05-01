using System.Net.Http.Headers;
using System.Net.Http.Json;
using Fenrir.Agent.Configuration;
using Fenrir.Contracts;

namespace Fenrir.Agent.Network;

public sealed class FenrirSiemClient(HttpClient httpClient, AgentOptions options)
{
    public async Task<SiemSourceDto?> RegisterSourceAsync(CancellationToken cancellationToken)
    {
        if (!options.RegisterSource)
        {
            return null;
        }

        ApplyAuthorization();

        var request = new SiemSourceRegistrationRequest(
            options.SourceName,
            "agent",
            "Fenrir",
            "Fenrir Agent",
            "http_batch",
            "fenrir_agent_v1",
            $"Endpoint telemetry source for {Environment.MachineName}",
            true);

        using var response = await httpClient.PostAsJsonAsync("api/siem/sources", request, JsonOptions.Default, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SiemSourceDto>(JsonOptions.Default, cancellationToken);
    }

    public async Task<SiemBatchIngestResponse> SendBatchAsync(Guid? sourceId, IReadOnlyList<SiemEventRequest> events, CancellationToken cancellationToken)
    {
        ApplyAuthorization();

        var request = new SiemBatchIngestRequest(
            options.SourceName,
            "agent_telemetry",
            "fenrir_agent_v1",
            sourceId,
            null,
            events);

        using var response = await httpClient.PostAsJsonAsync("api/siem/ingest/batch", request, JsonOptions.Default, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<SiemBatchIngestResponse>(JsonOptions.Default, cancellationToken);
        return result ?? throw new InvalidOperationException("SIEM batch ingest returned an empty response.");
    }

    private void ApplyAuthorization()
    {
        if (string.IsNullOrWhiteSpace(options.BearerToken))
        {
            return;
        }

        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.BearerToken);
    }
}
