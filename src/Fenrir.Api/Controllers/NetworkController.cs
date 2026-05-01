using Fenrir.Application.Abstractions;
using Fenrir.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Fenrir.Api.Controllers;

[ApiController]
[Route("api/network/scans")]
public sealed class NetworkController(INetworkScanningService networkScanning) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<NetworkScanCreatedResponse>> Create(NetworkScanRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await networkScanning.CreateScanAsync(request, cancellationToken);
            return AcceptedAtAction(nameof(Get), new { id = response.ScanId }, response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<NetworkScanDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var response = await networkScanning.GetScanAsync(id, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpGet("{id:guid}/results")]
    public async Task<ActionResult<IReadOnlyList<NetworkScanResultDto>>> GetResults(Guid id, CancellationToken cancellationToken)
    {
        var response = await networkScanning.GetScanAsync(id, cancellationToken);
        return response is null ? NotFound() : Ok(response.Results);
    }
}
