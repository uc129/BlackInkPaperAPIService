using Application.DTOs.Products;
using BlackInkPaperAPIService.Controllers.Extensions;
using Infrastructure.Contracts.Services;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

namespace BlackInkPaperAPIService.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/products")]
public class ProductController(
    IProductApplicationService productApplicationService,
    ILogger<ProductController> logger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResultDto<ProductSummaryDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Search([FromQuery] ProductSearchRequest request, CancellationToken cancellationToken)
    {
        var publicRequest = request with { IsAvailable = true };
        var response = await productApplicationService.SearchAsync(publicRequest, cancellationToken);
        return this.ToApiResult(response);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<ProductResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var response = await productApplicationService.GetByIdAsync(id, cancellationToken);
        if (response is { Success: true, Data.Taxonomy.IsAvailable: false })
        {
            logger.LogInformation("Public product lookup blocked for unavailable product id {ProductId}.", id);
            return NotFound(new ProblemDetails { Title = "Product not found.", Status = StatusCodes.Status404NotFound });
        }
        if (!response.Success) logger.LogInformation("Product with id {ProductId} was not found.", id);
        return this.ToApiResult(response);
    }

    [HttpGet("slug/{slug}")]
    [ProducesResponseType<ProductResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken cancellationToken)
    {
        var response = await productApplicationService.GetBySlugAsync(slug, cancellationToken);
        if (response is { Success: true, Data.Taxonomy.IsAvailable: false })
        {
            logger.LogInformation("Public product lookup blocked for unavailable slug {ProductSlug}.", slug);
            return NotFound(new ProblemDetails { Title = "Product not found.", Status = StatusCodes.Status404NotFound });
        }
        if (!response.Success) logger.LogInformation("Product with slug {ProductSlug} was not found.", slug);
        return this.ToApiResult(response);
    }
}
