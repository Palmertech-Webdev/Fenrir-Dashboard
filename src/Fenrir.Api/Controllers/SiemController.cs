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
