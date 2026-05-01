using Fenrir.Application.Abstractions;
using Fenrir.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Fenrir.Api.Controllers;

[ApiController]
[Route("api/jobs")]
public sealed class JobsController(IJobService jobService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<JobDto>>> List(CancellationToken cancellationToken)
    {
        var response = await jobService.ListAsync(cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<JobDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var response = await jobService.GetAsync(id, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }
}
