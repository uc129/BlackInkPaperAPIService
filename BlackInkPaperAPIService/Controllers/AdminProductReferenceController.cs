using Application.DTOs.Products;
using BlackInkPaperAPIService.Controllers.Extensions;
using Infrastructure.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlackInkPaperAPIService.Controllers;

[ApiController]
[Authorize(Roles = "Artist,Admin")]
[Route("api/admin/product-options")]
public class AdminProductReferenceController(IProductReferenceDataService productReferenceDataService) : ControllerBase
{
    [HttpGet("artists")]
    [ProducesResponseType<IReadOnlyList<ArtistLookupDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetArtists(CancellationToken cancellationToken)
        => this.ToApiResult(await productReferenceDataService.GetArtistsAsync(cancellationToken));

    [HttpGet("categories")]
    [ProducesResponseType<IReadOnlyList<ProductCategoryLookupDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories(CancellationToken cancellationToken)
        => this.ToApiResult(await productReferenceDataService.GetCategoriesAsync(cancellationToken));

    [HttpGet("subcategories")]
    [ProducesResponseType<IReadOnlyList<ProductSubCategoryLookupDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSubCategories([FromQuery] int? categoryId, CancellationToken cancellationToken)
        => this.ToApiResult(await productReferenceDataService.GetSubCategoriesAsync(categoryId, cancellationToken));

    [HttpGet("tags")]
    [ProducesResponseType<IReadOnlyList<ProductTagDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTags(CancellationToken cancellationToken)
        => this.ToApiResult(await productReferenceDataService.GetTagsAsync(cancellationToken));

    [HttpPost("categories")]
    [ProducesResponseType<ProductCategoryLookupDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateCategory([FromBody] CreateProductCategoryRequest request, CancellationToken cancellationToken)
    {
        var response = await productReferenceDataService.CreateCategoryAsync(request, cancellationToken);
        if (!response.Success || response.Data is null)
        {
            return this.ToApiResult(response);
        }

        return StatusCode(response.StatusCode, response);
    }

    [HttpPost("subcategories")]
    [ProducesResponseType<ProductSubCategoryLookupDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateSubCategory([FromBody] CreateProductSubCategoryRequest request, CancellationToken cancellationToken)
    {
        var response = await productReferenceDataService.CreateSubCategoryAsync(request, cancellationToken);
        if (!response.Success || response.Data is null)
        {
            return this.ToApiResult(response);
        }

        return StatusCode(response.StatusCode, response);
    }
}
