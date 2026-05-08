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

    [HttpPut("categories/{id:int}")]
    [ProducesResponseType<ProductCategoryLookupDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateCategory(int id, [FromBody] UpdateProductCategoryRequest request, CancellationToken cancellationToken)
        => this.ToApiResult(await productReferenceDataService.UpdateCategoryAsync(id, request, cancellationToken));

    [HttpDelete("categories/{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteCategory(int id, CancellationToken cancellationToken)
        => this.ToApiResult(await productReferenceDataService.DeleteCategoryAsync(id, cancellationToken));

    [HttpPut("subcategories/{id:int}")]
    [ProducesResponseType<ProductSubCategoryLookupDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateSubCategory(int id, [FromBody] UpdateProductSubCategoryRequest request, CancellationToken cancellationToken)
        => this.ToApiResult(await productReferenceDataService.UpdateSubCategoryAsync(id, request, cancellationToken));

    [HttpDelete("subcategories/{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteSubCategory(int id, CancellationToken cancellationToken)
        => this.ToApiResult(await productReferenceDataService.DeleteSubCategoryAsync(id, cancellationToken));

    [HttpPost("tags")]
    [ProducesResponseType<ProductTagDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateTag([FromBody] CreateProductTagRequest request, CancellationToken cancellationToken)
    {
        var response = await productReferenceDataService.CreateTagAsync(request, cancellationToken);
        if (!response.Success || response.Data is null)
        {
            return this.ToApiResult(response);
        }

        return StatusCode(response.StatusCode, response);
    }

    [HttpPut("tags/{id:int}")]
    [ProducesResponseType<ProductTagDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateTag(int id, [FromBody] UpdateProductTagRequest request, CancellationToken cancellationToken)
        => this.ToApiResult(await productReferenceDataService.UpdateTagAsync(id, request, cancellationToken));

    [HttpDelete("tags/{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteTag(int id, CancellationToken cancellationToken)
        => this.ToApiResult(await productReferenceDataService.DeleteTagAsync(id, cancellationToken));
}
