using Fenrir.Contracts;
using Fenrir.Infrastructure.Correlation;
using Fenrir.Infrastructure.Database;
using Microsoft.AspNetCore.Mvc;

namespace Fenrir.Api.Controllers;

[ApiController]
[Route("api/correlation")]
public sealed class CorrelationController(FenrirDbContext dbContext) : ControllerBase
{
    [HttpGet("rules")]
    public async Task<ActionResult<IReadOnlyList<CorrelationRuleDto>>> ListRules(CancellationToken cancellationToken)
    {
        var service = new EfCorrelationService(dbContext);
        return Ok(await service.ListRulesAsync(cancellationToken));
    }

    [HttpPost("rules")]
    public async Task<ActionResult<CorrelationRuleDto>> CreateRule(CorrelationRuleCreateRequest request, CancellationToken cancellationToken)
    {
        var service = new EfCorrelationService(dbContext);
        var created = await service.CreateRuleAsync(request, cancellationToken);
        return Created($"/api/correlation/rules/{created.Id}", created);
    }

    [HttpPatch("rules/{id:guid}")]
    public async Task<ActionResult<CorrelationRuleDto>> UpdateRule(Guid id, CorrelationRuleUpdateRequest request, CancellationToken cancellationToken)
    {
        var service = new EfCorrelationService(dbContext);
        var updated = await service.UpdateRuleAsync(id, request, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpGet("alerts")]
    public async Task<ActionResult<IReadOnlyList<CorrelationAlertDto>>> ListAlerts(CancellationToken cancellationToken)
    {
        var service = new EfCorrelationService(dbContext);
        return Ok(await service.ListAlertsAsync(cancellationToken));
    }

    [HttpPost("run")]
    public async Task<ActionResult<CorrelationRunResponse>> Run(CorrelationRunRequest request, CancellationToken cancellationToken)
    {
        var service = new EfCorrelationService(dbContext);
        return Ok(await service.RunAsync(request, cancellationToken));
    }

    [HttpGet("graph")]
    public async Task<ActionResult<EntityGraphResponse>> Graph([FromQuery] Guid? alertId, [FromQuery] int lookbackMinutes = 1440, CancellationToken cancellationToken = default)
    {
        var service = new EfCorrelationService(dbContext);
        return Ok(await service.BuildEntityGraphAsync(alertId, lookbackMinutes, cancellationToken));
    }
}
