using Application.DTOs.Checkout;
using Infrastructure.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using BlackInkPaperAPIService.Controllers.Extensions;

namespace BlackInkPaperAPIService.Controllers;

[ApiController]
[Authorize]
[Route("api/checkout")]
public class CheckoutController(
    ICheckoutApplicationService checkoutApplicationService) : ControllerBase
{
    [HttpPost("preview")]
    [ProducesResponseType<CheckoutPreviewDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Preview([FromBody] CheckoutPreviewRequest request, CancellationToken cancellationToken)
        => this.ToApiResult(await checkoutApplicationService.PreviewAsync(GetUserId(), request, cancellationToken));

    [HttpPost("payment-session")]
    [ProducesResponseType<PaymentSessionDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreatePaymentSession([FromBody] CreatePaymentSessionRequest request, CancellationToken cancellationToken)
        => this.ToApiResult(await checkoutApplicationService.CreatePaymentSessionAsync(GetUserId(), request, cancellationToken));

    [HttpPost("verify-payment")]
    [ProducesResponseType<OrderDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> VerifyPayment([FromBody] VerifyRazorpayPaymentRequest request, CancellationToken cancellationToken)
        => this.ToApiResult(await checkoutApplicationService.VerifyPaymentAsync(GetUserId(), request, cancellationToken));

    [HttpPost("place-order")]
    [ProducesResponseType<PlaceOrderResponseDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderRequest request, CancellationToken cancellationToken)
        => this.ToApiResult(await checkoutApplicationService.PlaceOrderAsync(GetUserId(), request, cancellationToken));

    [HttpGet("orders")]
    [ProducesResponseType<IReadOnlyList<OrderDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrders(CancellationToken cancellationToken)
        => this.ToApiResult(await checkoutApplicationService.GetOrdersAsync(GetUserId(), cancellationToken));

    [HttpGet("orders/{orderId:int}")]
    [ProducesResponseType<OrderDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrderById(int orderId, CancellationToken cancellationToken)
        => this.ToApiResult(await checkoutApplicationService.GetOrderByIdAsync(GetUserId(), orderId, cancellationToken));

    private string GetUserId()
        => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
}
