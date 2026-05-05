using Fenrir.Application.Abstractions;
using Fenrir.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Fenrir.Api.Controllers;

[ApiController]
[Route("api/darkweb")]
public sealed class DarkWebController(
    IDarkWebService darkWebService,
    IDarkWebExposureImportService exposureImportService) : ControllerBase
{
    [HttpPost("check")]
    public async Task<ActionResult<DarkWebCheckResponse>> Check(DarkWebCheckRequest request, CancellationToken cancellationToken)
    {
        var response = await darkWebService.CheckAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("import")]
    public async Task<ActionResult<DarkWebExposureImportResponse>> Import(
        DarkWebExposureImportRequest request,
        CancellationToken cancellationToken)
    {
        var response = await exposureImportService.ImportAsync(request, cancellationToken);
        return Ok(response);
    }
}
