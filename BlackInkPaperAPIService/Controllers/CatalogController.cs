using Application.DTOs.Products;
using BlackInkPaperAPIService.Controllers.Extensions;
using Common.YourProject.Models;
using Infrastructure.Contracts.Services;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

namespace BlackInkPaperAPIService.Controllers;

/// <summary>
/// Public, read-only catalogue taxonomy for the storefront: lists the active
/// top-level categories (e.g. Originals, Prints) and their sub-categories so a
/// client can build navigation and map a slug to the id used by
/// <c>GET /api/products?categoryId=...</c>. Admin management of the same data
/// lives under <c>api/admin/product-options</c>.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/catalog")]
public class CatalogController(IProductReferenceDataService productReferenceDataService) : ControllerBase
{
    [HttpGet("categories")]
    [ProducesResponseType<IReadOnlyList<ProductCategoryLookupDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories(CancellationToken cancellationToken)
    {
        var response = await productReferenceDataService.GetCategoriesAsync(cancellationToken);
        if (response is { Success: true, Data: not null })
        {
            var active = (IReadOnlyList<ProductCategoryLookupDto>)response.Data.Where(c => c.IsActive).ToList();
            return this.ToApiResult(ServiceResponse<IReadOnlyList<ProductCategoryLookupDto>>.Ok(active));
        }

        return this.ToApiResult(response);
    }

    [HttpGet("subcategories")]
    [ProducesResponseType<IReadOnlyList<ProductSubCategoryLookupDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSubCategories([FromQuery] int? categoryId, CancellationToken cancellationToken)
    {
        var response = await productReferenceDataService.GetSubCategoriesAsync(categoryId, cancellationToken);
        if (response is { Success: true, Data: not null })
        {
            var active = (IReadOnlyList<ProductSubCategoryLookupDto>)response.Data.Where(s => s.IsActive).ToList();
            return this.ToApiResult(ServiceResponse<IReadOnlyList<ProductSubCategoryLookupDto>>.Ok(active));
        }

        return this.ToApiResult(response);
    }
}
