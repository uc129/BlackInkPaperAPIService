using Application.DTOs.Contact;
using Asp.Versioning;
using BlackInkPaperAPIService.Controllers.Extensions;
using Infrastructure.Contracts.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BlackInkPaperAPIService.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/contact")]
public class ContactController(IContactApplicationService contactApplicationService) : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Submit([FromBody] SubmitContactRequest request, CancellationToken cancellationToken)
        => this.ToApiResult(await contactApplicationService.SubmitAsync(request, cancellationToken));
}
