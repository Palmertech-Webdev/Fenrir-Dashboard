using Fenrir.Application.Abstractions;
using Fenrir.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Fenrir.Api.Controllers;

[ApiController]
[Route("api/findings")]
public sealed class FindingsController(IFindingService findingService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FindingDto>>> List(CancellationToken cancellationToken)
    {
        var response = await findingService.ListAsync(cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<FindingDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var response = await findingService.GetAsync(id, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<FindingDto>> UpdateStatus(Guid id, UpdateFindingStatusRequest request, CancellationToken cancellationToken)
    {
        var response = await findingService.UpdateStatusAsync(id, request.Status, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }
}
