using Application.DTOs.Products;
using Common.YourProject.Models;

namespace Infrastructure.Contracts.Services;

public interface IProductReferenceDataService
{
    Task<ServiceResponse<IReadOnlyList<ArtistLookupDto>>> GetArtistsAsync(CancellationToken cancellationToken = default);
    Task<ServiceResponse<IReadOnlyList<ProductCategoryLookupDto>>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<ServiceResponse<IReadOnlyList<ProductSubCategoryLookupDto>>> GetSubCategoriesAsync(int? categoryId, CancellationToken cancellationToken = default);
    Task<ServiceResponse<IReadOnlyList<ProductTagDto>>> GetTagsAsync(CancellationToken cancellationToken = default);
    Task<ServiceResponse<ProductCategoryLookupDto>> CreateCategoryAsync(CreateProductCategoryRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResponse<ProductSubCategoryLookupDto>> CreateSubCategoryAsync(CreateProductSubCategoryRequest request, CancellationToken cancellationToken = default);
}
