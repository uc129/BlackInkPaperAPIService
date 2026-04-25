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
    Task<ServiceResponse<ProductCategoryLookupDto>> UpdateCategoryAsync(int id, UpdateProductCategoryRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResponse<ProductSubCategoryLookupDto>> UpdateSubCategoryAsync(int id, UpdateProductSubCategoryRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResponse<ProductTagDto>> CreateTagAsync(CreateProductTagRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResponse<ProductTagDto>> UpdateTagAsync(int id, UpdateProductTagRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResponse<bool>> DeleteCategoryAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResponse<bool>> DeleteSubCategoryAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResponse<bool>> DeleteTagAsync(int id, CancellationToken cancellationToken = default);
}
