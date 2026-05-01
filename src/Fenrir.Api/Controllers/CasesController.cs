using Fenrir.Application.Abstractions;
using Fenrir.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Fenrir.Api.Controllers;

[ApiController]
[Route("api/cases")]
public sealed class CasesController(ICaseService caseService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CaseSummaryDto>>> List(CancellationToken cancellationToken)
    {
        var response = await caseService.ListAsync(cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CaseDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var response = await caseService.GetAsync(id, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<CaseDto>> Create(CaseCreateRequest request, CancellationToken cancellationToken)
    {
        var response = await caseService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = response.Id }, response);
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<CaseDto>> Update(Guid id, CaseUpdateRequest request, CancellationToken cancellationToken)
    {
        var response = await caseService.UpdateAsync(id, request, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpPost("{id:guid}/notes")]
    public async Task<ActionResult<CaseDto>> AddNote(Guid id, CaseNoteCreateRequest request, CancellationToken cancellationToken)
    {
        var response = await caseService.AddNoteAsync(id, request, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpPost("{id:guid}/evidence")]
    public async Task<ActionResult<CaseDto>> AddEvidence(Guid id, CaseEvidenceCreateRequest request, CancellationToken cancellationToken)
    {
        var response = await caseService.AddEvidenceAsync(id, request, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpPost("{id:guid}/events")]
    public async Task<ActionResult<CaseDto>> LinkEvent(Guid id, CaseEventLinkRequest request, CancellationToken cancellationToken)
    {
        var response = await caseService.LinkEventAsync(id, request, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpPost("{id:guid}/indicators")]
    public async Task<ActionResult<CaseDto>> LinkIndicator(Guid id, CaseIndicatorLinkRequest request, CancellationToken cancellationToken)
    {
        var response = await caseService.LinkIndicatorAsync(id, request, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpPost("{id:guid}/assets")]
    public async Task<ActionResult<CaseDto>> LinkAsset(Guid id, CaseAssetLinkRequest request, CancellationToken cancellationToken)
    {
        var response = await caseService.LinkAssetAsync(id, request, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpPost("{id:guid}/users")]
    public async Task<ActionResult<CaseDto>> LinkUser(Guid id, CaseUserLinkRequest request, CancellationToken cancellationToken)
    {
        var response = await caseService.LinkUserAsync(id, request, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpPost("{id:guid}/timeline")]
    public async Task<ActionResult<CaseDto>> AddTimelineItem(Guid id, CaseTimelineItemCreateRequest request, CancellationToken cancellationToken)
    {
        var response = await caseService.AddTimelineItemAsync(id, request, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }
}
