using Application.DTOs.Checkout;
using Infrastructure.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using BlackInkPaperAPIService.Controllers.Extensions;

namespace BlackInkPaperAPIService.Controllers;

[ApiController]
[Authorize]
[Route("api/shipping-addresses")]
public class ShippingAddressesController(
    ICheckoutApplicationService checkoutApplicationService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ShippingAddressDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        => this.ToApiResult(await checkoutApplicationService.GetAddressesAsync(GetUserId(), cancellationToken));

    [HttpPost]
    [ProducesResponseType<ShippingAddressDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateShippingAddressRequest request, CancellationToken cancellationToken)
        => this.ToApiResult(await checkoutApplicationService.AddAddressAsync(GetUserId(), request, cancellationToken));

    [HttpPut("{id:int}")]
    [ProducesResponseType<ShippingAddressDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateShippingAddressRequest request, CancellationToken cancellationToken)
        => this.ToApiResult(await checkoutApplicationService.UpdateAddressAsync(GetUserId(), id, request, cancellationToken));

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        => this.ToApiResult(await checkoutApplicationService.DeleteAddressAsync(GetUserId(), id, cancellationToken));

    private string GetUserId()
        => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
}
