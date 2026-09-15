using Application.DTOs.UserAuth;
using Asp.Versioning;
using BlackInkPaperAPIService.Controllers.Extensions;
using Common.YourProject.Models;
using Infrastructure.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace BlackInkPaperAPIService.Controllers;

/// <summary>
/// Passwordless phone login, kept separate from <see cref="AccountsController"/> — that one is
/// email- and password-shaped throughout, and this flow shares none of its steps.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/accounts/phone")]
public class PhoneAuthController(IPhoneAuthService phoneAuthService) : ControllerBase
{
    [HttpPost("start")]
    [AllowAnonymous]
    [EnableRateLimiting("otp")]
    [ProducesResponseType<StartPhoneAuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> Start([FromBody] StartPhoneAuthRequest request, CancellationToken ct)
        => this.ToApiResult(await phoneAuthService.StartAsync(request, ClientIp, ct));

    [HttpPost("verify")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Verify([FromBody] VerifyPhoneAuthRequest request, CancellationToken ct)
        => this.ToApiResult(await phoneAuthService.VerifyAsync(request, ct));

    [HttpPost("link/start")]
    [Authorize]
    [EnableRateLimiting("otp")]
    [ProducesResponseType<StartPhoneAuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> StartLink([FromBody] StartPhoneAuthRequest request, CancellationToken ct)
        => this.ToApiResult(await phoneAuthService.StartPhoneLinkAsync(request, ClientIp, ct));

    [HttpPost("link")]
    [Authorize]
    [EnableRateLimiting("auth")]
    [ProducesResponseType<string>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Link([FromBody] VerifyPhoneAuthRequest request, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return this.ToApiResult(ServiceResponse<string>.Fail(
                "User not found.", statusCode: 404, errorCode: "user_not_found"));

        return this.ToApiResult(await phoneAuthService.LinkPhoneAsync(userId, request, ct));
    }

    private string? ClientIp => HttpContext.Connection.RemoteIpAddress?.ToString();
}
