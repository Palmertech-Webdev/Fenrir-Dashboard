using Fenrir.Application.Abstractions;
using Fenrir.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Fenrir.Api.Controllers;

[ApiController]
[Route("api/siem/events")]
public sealed class SiemController(ISiemService siemService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<SiemEventIngestResponse>> Ingest(SiemEventRequest request, CancellationToken cancellationToken)
    {
        var response = await siemService.IngestAsync(request, cancellationToken);
        return Accepted(response);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SiemEventDto>>> List([FromQuery] string? source, [FromQuery] string? host, [FromQuery] string? severity, CancellationToken cancellationToken)
    {
        var response = await siemService.ListAsync(source, host, severity, cancellationToken);
        return Ok(response);
    }
}
