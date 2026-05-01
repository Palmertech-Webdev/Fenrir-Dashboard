using Fenrir.Application.Abstractions;
using Fenrir.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Fenrir.Api.Controllers;

[ApiController]
[Route("api/siem")]
public sealed class SiemController(ISiemService siemService) : ControllerBase
{
    [HttpPost("events")]
    public async Task<ActionResult<SiemEventIngestResponse>> Ingest(SiemEventRequest request, CancellationToken cancellationToken)
    {
        var response = await siemService.IngestAsync(request, cancellationToken);
        return Accepted(response);
    }

    [HttpPost("ingest/batch")]
    public async Task<ActionResult<SiemBatchIngestResponse>> IngestBatch(SiemBatchIngestRequest request, CancellationToken cancellationToken)
    {
        var response = await siemService.IngestBatchAsync(request, cancellationToken);
        return Accepted(response);
    }

    [HttpGet("events")]
    public async Task<ActionResult<IReadOnlyList<SiemEventDto>>> List([FromQuery] string? source, [FromQuery] string? host, [FromQuery] string? severity, CancellationToken cancellationToken)
    {
        var response = await siemService.ListAsync(source, host, severity, cancellationToken);
        return Ok(response);
    }

    [HttpPost("events/search")]
    public async Task<ActionResult<IReadOnlyList<SiemEventDto>>> Search(SiemEventSearchRequest request, CancellationToken cancellationToken)
    {
        var response = await siemService.SearchAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("sources")]
    public async Task<ActionResult<SiemSourceDto>> RegisterSource(SiemSourceRegistrationRequest request, CancellationToken cancellationToken)
    {
        var response = await siemService.RegisterSourceAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpGet("sources")]
    public async Task<ActionResult<IReadOnlyList<SiemSourceDto>>> ListSources(CancellationToken cancellationToken)
    {
        var response = await siemService.ListSourcesAsync(cancellationToken);
        return Ok(response);
    }

    [HttpGet("sources/{id:guid}")]
    public async Task<ActionResult<SiemSourceDto>> GetSource(Guid id, CancellationToken cancellationToken)
    {
        var response = await siemService.GetSourceAsync(id, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpPatch("sources/{id:guid}")]
    public async Task<ActionResult<SiemSourceDto>> UpdateSource(Guid id, SiemSourceUpdateRequest request, CancellationToken cancellationToken)
    {
        var response = await siemService.UpdateSourceAsync(id, request, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpPut("sources/{id:guid}/config")]
    public async Task<ActionResult<SiemSourceDto>> UpdateSourceConfig(Guid id, SiemSourceConfigRequest request, CancellationToken cancellationToken)
    {
        var response = await siemService.UpdateSourceConfigAsync(id, request, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpPut("sources/{id:guid}/secret-refs")]
    public async Task<ActionResult<SiemSourceDto>> AddOrUpdateSecretRef(Guid id, SiemSourceSecretRefRequest request, CancellationToken cancellationToken)
    {
        var response = await siemService.AddOrUpdateSecretRefAsync(id, request, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpDelete("sources/{id:guid}/secret-refs/{secretPurpose}")]
    public async Task<ActionResult<SiemSourceDto>> RemoveSecretRef(Guid id, string secretPurpose, CancellationToken cancellationToken)
    {
        var response = await siemService.RemoveSecretRefAsync(id, secretPurpose, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpPut("sources/{id:guid}/state")]
    public async Task<ActionResult<SiemSourceDto>> UpdateSourceState(Guid id, SiemSourceStateRequest request, CancellationToken cancellationToken)
    {
        var response = await siemService.UpdateSourceStateAsync(id, request, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpPost("sources/{id:guid}/health")]
    public async Task<ActionResult<SiemSourceDto>> AddHealthSnapshot(Guid id, SiemSourceHealthSnapshotRequest request, CancellationToken cancellationToken)
    {
        var response = await siemService.AddHealthSnapshotAsync(id, request, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpGet("ingestion-jobs")]
    public async Task<ActionResult<IReadOnlyList<SiemIngestionJobDto>>> ListIngestionJobs(CancellationToken cancellationToken)
    {
        var response = await siemService.ListIngestionJobsAsync(cancellationToken);
        return Ok(response);
    }

    [HttpGet("ingestion-jobs/{id:guid}")]
    public async Task<ActionResult<SiemIngestionJobDto>> GetIngestionJob(Guid id, CancellationToken cancellationToken)
    {
        var response = await siemService.GetIngestionJobAsync(id, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }
}
