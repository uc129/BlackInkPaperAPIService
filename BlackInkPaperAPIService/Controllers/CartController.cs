using Application.DTOs.Cart;
using Infrastructure.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using BlackInkPaperAPIService.Controllers.Extensions;
using Asp.Versioning;

namespace BlackInkPaperAPIService.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/cart")]
public class CartController(
    ICartApplicationService cartApplicationService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<CartResponseDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
        => this.ToApiResult(await cartApplicationService.GetActiveCartAsync(GetUserId(), cancellationToken));

    [HttpPost("items")]
    [ProducesResponseType<CartResponseDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> AddItem([FromBody] AddCartItemRequest request, CancellationToken cancellationToken)
        => this.ToApiResult(await cartApplicationService.AddItemAsync(GetUserId(), request, cancellationToken));

    [HttpPut("items/{cartItemId:int}")]
    [ProducesResponseType<CartResponseDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateQuantity(
        int cartItemId,
        [FromBody] UpdateCartItemQuantityRequest request,
        CancellationToken cancellationToken)
        => this.ToApiResult(await cartApplicationService.UpdateItemQuantityAsync(GetUserId(), cartItemId, request, cancellationToken));

    [HttpDelete("items/{cartItemId:int}")]
    [ProducesResponseType<CartResponseDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> RemoveItem(int cartItemId, CancellationToken cancellationToken)
        => this.ToApiResult(await cartApplicationService.RemoveItemAsync(GetUserId(), cartItemId, cancellationToken));

    [HttpDelete]
    [ProducesResponseType<CartResponseDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Clear(CancellationToken cancellationToken)
        => this.ToApiResult(await cartApplicationService.ClearAsync(GetUserId(), cancellationToken));

    private string GetUserId()
        => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
}
