using Application.DTOs.Checkout;
using BlackInkPaperAPIService.Controllers.Extensions;
using Infrastructure.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

namespace BlackInkPaperAPIService.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Authorize(Roles = "Admin")]
[Route("api/admin/orders")]
public class AdminOrdersController(
    IAdminOrderService adminOrderService,
    ILogger<AdminOrdersController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] AdminOrderSearchRequest request, CancellationToken cancellationToken)
    {
        var response = await adminOrderService.GetAllOrdersAsync(request, cancellationToken);
        return this.ToApiResult(response);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var response = await adminOrderService.GetOrderByIdAsync(id, cancellationToken);
        if (!response.Success) logger.LogInformation("Admin order lookup failed for id {OrderId}.", id);
        return this.ToApiResult(response);
    }

    [HttpPatch("{id:int}/status")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateStatus(
        int id,
        [FromBody] UpdateOrderStatusRequest request,
        CancellationToken cancellationToken)
    {
        var response = await adminOrderService.UpdateOrderStatusAsync(id, request, cancellationToken);
        return this.ToApiResult(response);
    }
}
