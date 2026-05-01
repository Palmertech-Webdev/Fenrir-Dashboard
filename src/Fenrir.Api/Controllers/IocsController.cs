using Fenrir.Application.Abstractions;
using Fenrir.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Fenrir.Api.Controllers;

[ApiController]
[Route("api/iocs")]
public sealed class IocsController(IIocService iocService) : ControllerBase
{
    [HttpPost("check")]
    public async Task<ActionResult<IocCheckResponse>> Check(IocCheckRequest request, CancellationToken cancellationToken)
    {
        var response = await iocService.CheckAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("import")]
    public async Task<ActionResult<IReadOnlyList<IocRecordDto>>> Import(IocImportRequest request, CancellationToken cancellationToken)
    {
        var response = await iocService.ImportAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<IocRecordDto>>> List(CancellationToken cancellationToken)
    {
        var response = await iocService.ListAsync(cancellationToken);
        return Ok(response);
    }
}
