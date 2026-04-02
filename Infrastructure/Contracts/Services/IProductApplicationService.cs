using Application.DTOs.Products;
using Common.YourProject.Models;

namespace Infrastructure.Contracts.Services;

public interface IProductApplicationService
{
    Task<ServiceResponse<ProductResponseDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResponse<ProductResponseDto>> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<ServiceResponse<PagedResultDto<ProductSummaryDto>>> SearchAsync(ProductSearchRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResponse<ProductResponseDto>> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResponse<ProductResponseDto>> UpdateAsync(int id, UpdateProductRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResponse<ProductResponseDto>> UpdateFlagsAsync(int id, UpdateProductFlagsRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResponse<bool>> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
