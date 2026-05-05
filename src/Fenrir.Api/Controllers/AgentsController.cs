using Fenrir.Application.Abstractions;
using Fenrir.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Fenrir.Api.Controllers;

[ApiController]
[Route("api/agents")]
public sealed class AgentsController(IAgentService agentService, IAgentPackageBuilder packageBuilder) : ControllerBase
{
    [HttpPost("build")]
    public async Task<IActionResult> BuildAgentPackage(AgentBuildRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ServerUrl))
        {
            return BadRequest(new { error = "Server API URL or IP address is required." });
        }

        if (string.IsNullOrWhiteSpace(request.CompanyName))
        {
            return BadRequest(new { error = "Company name is required." });
        }

        try
        {
            var result = await packageBuilder.BuildPackageAsync(request, cancellationToken);
            return File(result.Content, "application/zip", result.FileName);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("enrolment-tokens")]
    public async Task<ActionResult<AgentEnrolmentTokenCreatedResponse>> CreateEnrolmentToken(AgentEnrolmentTokenCreateRequest request, CancellationToken cancellationToken)
    {
        var response = await agentService.CreateEnrolmentTokenAsync(request, cancellationToken);
        return Created($"/api/agents/enrolment-tokens/{response.Id}", response);
    }

    [HttpGet("enrolment-tokens")]
    public async Task<ActionResult<IReadOnlyList<AgentEnrolmentTokenDto>>> ListEnrolmentTokens(CancellationToken cancellationToken)
    {
        var response = await agentService.ListEnrolmentTokensAsync(cancellationToken);
        return Ok(response);
    }

    [HttpPost("enrolment-tokens/{id:guid}/revoke")]
    public async Task<ActionResult<AgentEnrolmentTokenDto>> RevokeEnrolmentToken(Guid id, CancellationToken cancellationToken)
    {
        var response = await agentService.RevokeEnrolmentTokenAsync(id, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpPost("enrol")]
    public async Task<ActionResult<AgentEnrolResponse>> Enrol(AgentEnrolRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await agentService.EnrolAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{agentId}/heartbeat")]
    public async Task<ActionResult<AgentHeartbeatResponse>> Heartbeat(string agentId, AgentHeartbeatRequest request, CancellationToken cancellationToken)
    {
        var response = await agentService.HeartbeatAsync(agentId, request, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AgentEndpointDto>>> ListAgents(CancellationToken cancellationToken)
    {
        var response = await agentService.ListAgentsAsync(cancellationToken);
        return Ok(response);
    }

    [HttpGet("{agentId}")]
    public async Task<ActionResult<AgentEndpointDto>> GetAgent(string agentId, CancellationToken cancellationToken)
    {
        var response = await agentService.GetAgentAsync(agentId, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }
}
