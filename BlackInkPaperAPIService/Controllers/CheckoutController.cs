using Application.DTOs.Checkout;
using Application.DTOs.Products;
using BlackInkPaperAPIService.Controllers.Extensions;
using Common.YourProject.Models;
using Infrastructure.Contracts.Services;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Asp.Versioning;

namespace BlackInkPaperAPIService.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/checkout")]
public class CheckoutController(
    ICheckoutApplicationService checkoutApplicationService,
    UserManager<AppIdentityUser> userManager) : ControllerBase
{
    [HttpPost("preview")]
    [ProducesResponseType<CheckoutPreviewDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Preview([FromBody] CheckoutPreviewRequest request, CancellationToken cancellationToken)
        => this.ToApiResult(await checkoutApplicationService.PreviewAsync(GetUserId(), request, cancellationToken));

    [HttpPost("payment-session")]
    [ProducesResponseType<PaymentSessionDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreatePaymentSession([FromBody] CreatePaymentSessionRequest request, CancellationToken cancellationToken)
    {
        var guard = await RequireEmailConfirmedAsync<PaymentSessionDto>(cancellationToken);
        if (guard is not null) return guard;
        return this.ToApiResult(await checkoutApplicationService.CreatePaymentSessionAsync(GetUserId(), request, cancellationToken));
    }

    [HttpPost("verify-payment")]
    [ProducesResponseType<OrderDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> VerifyPayment([FromBody] VerifyRazorpayPaymentRequest request, CancellationToken cancellationToken)
        => this.ToApiResult(await checkoutApplicationService.VerifyPaymentAsync(GetUserId(), request, cancellationToken));

    [HttpPost("place-order")]
    [ProducesResponseType<PlaceOrderResponseDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderRequest request, CancellationToken cancellationToken)
    {
        var guard = await RequireEmailConfirmedAsync<PlaceOrderResponseDto>(cancellationToken);
        if (guard is not null) return guard;
        return this.ToApiResult(await checkoutApplicationService.PlaceOrderAsync(GetUserId(), request, cancellationToken));
    }

    [HttpGet("orders")]
    [ProducesResponseType<PagedResultDto<OrderDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
        => this.ToApiResult(await checkoutApplicationService.GetOrdersAsync(GetUserId(), page, pageSize, cancellationToken));

    [HttpGet("orders/{orderId:int}")]
    [ProducesResponseType<OrderDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrderById(int orderId, CancellationToken cancellationToken)
        => this.ToApiResult(await checkoutApplicationService.GetOrderByIdAsync(GetUserId(), orderId, cancellationToken));

    [HttpPost("orders/{orderId:int}/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelOrder(int orderId, CancellationToken cancellationToken)
        => this.ToApiResult(await checkoutApplicationService.CancelOrderAsync(GetUserId(), orderId, cancellationToken));

    private string GetUserId()
        => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    private async Task<IActionResult?> RequireEmailConfirmedAsync<T>(CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(GetUserId());
        if (user is not null && !user.EmailConfirmed)
            return this.ToApiResult(ServiceResponse<T>.Fail(
                "Please confirm your email address before placing an order.",
                statusCode: 403, errorCode: "email_not_confirmed"));
        return null;
    }
}
