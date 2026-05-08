using Application.DTOs.Products;
using BlackInkPaperAPIService.Controllers.Extensions;
using Infrastructure.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

namespace BlackInkPaperAPIService.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Authorize(Roles = "Artist,Admin")]
[Route("api/admin/products")]
public class AdminProductsController(
    IProductApplicationService productApplicationService,
    ILogger<AdminProductsController> logger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResultDto<ProductSummaryDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Search([FromQuery] ProductSearchRequest request, CancellationToken cancellationToken)
    {
        var response = await productApplicationService.SearchAsync(request, cancellationToken);
        return this.ToApiResult(response);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<ProductResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var response = await productApplicationService.GetByIdAsync(id, cancellationToken);
        if (!response.Success) logger.LogInformation("Admin product lookup failed for id {ProductId}.", id);
        return this.ToApiResult(response);
    }

    [HttpGet("slug/{slug}")]
    [ProducesResponseType<ProductResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken cancellationToken)
    {
        var response = await productApplicationService.GetBySlugAsync(slug, cancellationToken);
        if (!response.Success) logger.LogInformation("Admin product lookup failed for slug {ProductSlug}.", slug);
        return this.ToApiResult(response);
    }

    [HttpPost]
    [ProducesResponseType<ProductResponseDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var response = await productApplicationService.CreateAsync(request, cancellationToken);
        if (!response.Success || response.Data is null) return this.ToApiResult(response);
        return CreatedAtAction(nameof(GetById), new { id = response.Data.Id }, response);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<ProductResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        var response = await productApplicationService.UpdateAsync(id, request, cancellationToken);
        if (!response.Success) logger.LogInformation("Admin product update failed for id {ProductId}.", id);
        return this.ToApiResult(response);
    }

    [HttpPatch("{id:int}/flags")]
    [ProducesResponseType<ProductResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateFlags(
        int id,
        [FromBody] UpdateProductFlagsRequest request,
        CancellationToken cancellationToken)
    {
        var response = await productApplicationService.UpdateFlagsAsync(id, request, cancellationToken);
        if (!response.Success) logger.LogInformation("Admin product flags update failed for id {ProductId}.", id);
        return this.ToApiResult(response);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var response = await productApplicationService.DeleteAsync(id, cancellationToken);
        if (!response.Success) logger.LogInformation("Admin product delete failed for id {ProductId}.", id);
        return this.ToApiResult(response);
    }
}
