using Fenrir.Application.Abstractions;
using Fenrir.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Fenrir.Api.Controllers;

[ApiController]
[Route("api/dns")]
public sealed class DnsController(IDnsMonitoringService dnsMonitoring) : ControllerBase
{
    [HttpPost("check-domain")]
    public async Task<ActionResult<DnsDomainCheckResponse>> CheckDomain(
        [FromBody] DnsDomainCheckRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Domain))
        {
            return BadRequest(new
            {
                error = "A domain is required.",
                expectedBody = new { domain = "example.com" }
            });
        }

        try
        {
            var response = await dnsMonitoring.CheckDomainAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new
            {
                error = exception.Message,
                expectedBody = new { domain = "example.com" }
            });
        }
    }

    [HttpGet("monitored-domains")]
    public async Task<ActionResult<IReadOnlyList<MonitoredDomainDto>>> ListMonitoredDomains(CancellationToken cancellationToken)
    {
        var response = await dnsMonitoring.ListMonitoredDomainsAsync(cancellationToken);
        return Ok(response);
    }

    [HttpPost("monitored-domains")]
    public async Task<ActionResult<MonitoredDomainDto>> AddMonitoredDomain(
        [FromBody] MonitoredDomainRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Domain))
        {
            return BadRequest(new
            {
                error = "A domain is required.",
                expectedBody = new { domain = "example.com", owner = "Optional owner" }
            });
        }

        try
        {
            var response = await dnsMonitoring.AddMonitoredDomainAsync(request, cancellationToken);
            return CreatedAtAction(nameof(ListMonitoredDomains), response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new
            {
                error = exception.Message,
                expectedBody = new { domain = "example.com", owner = "Optional owner" }
            });
        }
    }
}
