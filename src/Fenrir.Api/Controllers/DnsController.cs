using Fenrir.Application.Abstractions;
using Fenrir.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Fenrir.Api.Controllers;

[ApiController]
[Route("api/dns")]
public sealed class DnsController(IDnsMonitoringService dnsMonitoring) : ControllerBase
{
    [HttpPost("check-domain")]
    public async Task<ActionResult<DnsDomainCheckResponse>> CheckDomain(DnsDomainCheckRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await dnsMonitoring.CheckDomainAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    [HttpGet("monitored-domains")]
    public async Task<ActionResult<IReadOnlyList<MonitoredDomainDto>>> ListMonitoredDomains(CancellationToken cancellationToken)
    {
        var response = await dnsMonitoring.ListMonitoredDomainsAsync(cancellationToken);
        return Ok(response);
    }

    [HttpPost("monitored-domains")]
    public async Task<ActionResult<MonitoredDomainDto>> AddMonitoredDomain(MonitoredDomainRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await dnsMonitoring.AddMonitoredDomainAsync(request, cancellationToken);
            return CreatedAtAction(nameof(ListMonitoredDomains), response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }
}
