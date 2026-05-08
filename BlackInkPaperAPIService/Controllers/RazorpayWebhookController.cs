using BlackInkPaperAPIService.Controllers.Extensions;
using Infrastructure.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

namespace BlackInkPaperAPIService.Controllers;

[ApiController]
[ApiVersion("1.0")]
[AllowAnonymous]
[Route("api/payments/razorpay")]
public class RazorpayWebhookController(
    ICheckoutApplicationService checkoutApplicationService) : ControllerBase
{
    [HttpPost("webhook")]
    public async Task<IActionResult> HandleWebhook(CancellationToken cancellationToken)
    {
        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body, leaveOpen: true);
        var rawBody = await reader.ReadToEndAsync(cancellationToken);
        Request.Body.Position = 0;

        var signature = Request.Headers["X-Razorpay-Signature"].FirstOrDefault();
        var eventId = Request.Headers["X-Razorpay-Event-Id"].FirstOrDefault();

        return this.ToApiResult(await checkoutApplicationService.HandleRazorpayWebhookAsync(rawBody, signature, eventId, cancellationToken));
    }
}
