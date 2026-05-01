using Fenrir.Application.Abstractions;
using Fenrir.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Fenrir.Api.Controllers;

[ApiController]
[Route("api/email")]
public sealed class EmailController(IEmailVerificationService emailVerification, IEmailHeaderCheckService headerCheck) : ControllerBase
{
    [HttpPost("verify")]
    public async Task<ActionResult<EmailVerificationResponse>> Verify(EmailVerificationRequest request, CancellationToken cancellationToken)
    {
        var response = await emailVerification.VerifyAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("header-check")]
    public async Task<ActionResult<EmailHeaderCheckResponse>> HeaderCheck(EmailHeaderCheckRequest request, CancellationToken cancellationToken)
    {
        var response = await headerCheck.CheckAsync(request, cancellationToken);
        return Ok(response);
    }
}
