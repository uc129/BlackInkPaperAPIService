using Application.DTOs.Products;
using Domain.Aggregates.Ecommerce;

namespace Infrastructure.Contracts.Repositories;

public interface IProductRepository
{
    Task<ProductAggregate?> GetById(int id);
    Task<ProductAggregate?> GetBySlug(string slug);
    Task<IEnumerable<ProductAggregate>> GetAll();
    Task<(IEnumerable<ProductAggregate> Items, int TotalCount)> Search(
        string? query,
        int? artistId,
        int? categoryId,
        int? subCategoryId,
        int? tagId,
        bool? isAvailable,
        bool? isFeatured,
        int page,
        int pageSize);
    Task<int> Add(ProductAggregate product);
    Task Update(ProductAggregate product);
    Task UpdateFlags(int id, bool? isAvailable, bool? isFeatured, DateTime updatedAt, string updatedBy);
    Task Delete(ProductAggregate product);
    Task<bool> ExistsBySlug(string slug, int? excludedProductId = null);
    Task<bool> ExistsByProductId(string productId, int? excludedProductId = null);
    Task<bool> ExistsByNameCode(string nameCode, int? excludedProductId = null);
    Task<bool> ArtistExists(int artistId);
    Task<bool> CategoryExists(int categoryId);
    Task<bool> SubCategoryExists(int subCategoryId);
    Task<bool> ArtSpecificationExists(int artSpecId);
    Task<IReadOnlyList<ArtistLookupDto>> GetArtists();
    Task<IReadOnlyList<ProductCategoryLookupDto>> GetCategories();
    Task<IReadOnlyList<ProductSubCategoryLookupDto>> GetSubCategories(int? categoryId = null);
    Task<IReadOnlyList<ProductTagDto>> GetTags();
    Task<ProductCategoryLookupDto> CreateCategory(CreateProductCategoryRequest request);
    Task<ProductSubCategoryLookupDto> CreateSubCategory(CreateProductSubCategoryRequest request);
    Task<bool> CategorySlugExists(string slug);
    Task<bool> SubCategorySlugExists(string slug);
}
