using Application.DTOs.Products;
using Domain.Aggregates.Ecommerce;
using Infrastructure.Contracts.Repositories;

namespace BlackInkPaperAPIService.Tests.Products.Fakes;

internal sealed class FakeProductRepository : IProductRepository
{
    // ── Configurable handlers ────────────────────────────────────────────────
    public Func<int, Task<ProductAggregate?>> GetByIdHandler { get; set; }
        = _ => Task.FromResult<ProductAggregate?>(null);
    public Func<string, Task<ProductAggregate?>> GetBySlugHandler { get; set; }
        = _ => Task.FromResult<ProductAggregate?>(null);
    public Func<string?, int?, int?, int?, int?, bool?, bool?, int, int, Task<(IEnumerable<ProductAggregate> Items, int TotalCount)>> SearchHandler { get; set; }
        = (_, _, _, _, _, _, _, _, _) => Task.FromResult<(IEnumerable<ProductAggregate>, int)>(([], 0));
    public Func<ProductAggregate, Task<int>> AddHandler    { get; set; } = _ => Task.FromResult(1);
    public Func<ProductAggregate, Task>      UpdateHandler { get; set; } = _ => Task.CompletedTask;
    public Func<ProductAggregate, Task>      DeleteHandler { get; set; } = _ => Task.CompletedTask;
    public Func<string, int?, Task<bool>> ExistsBySlugHandler       { get; set; } = (_, _) => Task.FromResult(false);
    public Func<string, int?, Task<bool>> ExistsByProductIdHandler  { get; set; } = (_, _) => Task.FromResult(false);
    public Func<string, int?, Task<bool>> ExistsByNameCodeHandler   { get; set; } = (_, _) => Task.FromResult(false);
    public Func<int, Task<bool>> ArtistExistsHandler      { get; set; } = _ => Task.FromResult(true);
    public Func<int, Task<bool>> CategoryExistsHandler    { get; set; } = _ => Task.FromResult(true);
    public Func<int, Task<bool>> SubCategoryExistsHandler { get; set; } = _ => Task.FromResult(true);

    // ── Captured values ──────────────────────────────────────────────────────
    public ProductAggregate? AddedProduct     { get; private set; }
    public ProductAggregate? UpdatedProduct   { get; private set; }
    public ProductAggregate? DeletedProduct   { get; private set; }

    // ── Core product CRUD ────────────────────────────────────────────────────
    public Task<ProductAggregate?> GetById(int id) => GetByIdHandler(id);
    public Task<ProductAggregate?> GetBySlug(string slug) => GetBySlugHandler(slug);
    public Task<IEnumerable<ProductAggregate>> GetAll() => Task.FromResult(Enumerable.Empty<ProductAggregate>());

    public async Task<(IEnumerable<ProductAggregate> Items, int TotalCount)> Search(
        string? query, int? artistId, int? categoryId, int? subCategoryId, int? tagId,
        bool? isAvailable, bool? isFeatured, int page, int pageSize)
        => await SearchHandler(query, artistId, categoryId, subCategoryId, tagId, isAvailable, isFeatured, page, pageSize);

    public async Task<int> Add(ProductAggregate product)  { AddedProduct   = product; return await AddHandler(product); }
    public async Task Update(ProductAggregate product)    { UpdatedProduct = product; await UpdateHandler(product); }
    public async Task Delete(ProductAggregate product)    { DeletedProduct = product; await DeleteHandler(product); }

    public Task UpdateFlags(int id, bool? isAvailable, bool? isFeatured, DateTime updatedAt, string updatedBy)
        => Task.CompletedTask;

    // ── Existence checks ─────────────────────────────────────────────────────
    public Task<bool> ExistsBySlug(string slug, int? excludedProductId = null)      => ExistsBySlugHandler(slug, excludedProductId);
    public Task<bool> ExistsByProductId(string productId, int? excludedProductId = null) => ExistsByProductIdHandler(productId, excludedProductId);
    public Task<bool> ExistsByNameCode(string nameCode, int? excludedProductId = null)   => ExistsByNameCodeHandler(nameCode, excludedProductId);
    public Task<bool> ArtistExists(int artistId)           => ArtistExistsHandler(artistId);
    public Task<bool> CategoryExists(int categoryId)       => CategoryExistsHandler(categoryId);
    public Task<bool> SubCategoryExists(int subCategoryId) => SubCategoryExistsHandler(subCategoryId);

    // ── Reference-data slug / in-use checks (default: no conflicts) ──────────
    public Task<bool> CategorySlugExists(string slug)                           => Task.FromResult(false);
    public Task<bool> CategorySlugExists(string slug, int excludedCategoryId)   => Task.FromResult(false);
    public Task<bool> SubCategorySlugExists(string slug)                        => Task.FromResult(false);
    public Task<bool> SubCategorySlugExists(string slug, int excludedId)        => Task.FromResult(false);
    public Task<bool> TagSlugExists(string slug)                                => Task.FromResult(false);
    public Task<bool> TagSlugExists(string slug, int excludedTagId)             => Task.FromResult(false);
    public Task<bool> TagExists(int tagId)                                      => Task.FromResult(true);
    public Task<bool> IsCategoryInUse(int categoryId)                           => Task.FromResult(false);
    public Task<bool> IsSubCategoryInUse(int subCategoryId)                     => Task.FromResult(false);
    public Task<bool> IsTagInUse(int tagId)                                     => Task.FromResult(false);

    // ── Reference data reads (return empty lists) ────────────────────────────
    public Task<IReadOnlyList<ArtistLookupDto>> GetArtists()
        => Task.FromResult<IReadOnlyList<ArtistLookupDto>>([]);
    public Task<IReadOnlyList<ProductCategoryLookupDto>> GetCategories()
        => Task.FromResult<IReadOnlyList<ProductCategoryLookupDto>>([]);
    public Task<IReadOnlyList<ProductSubCategoryLookupDto>> GetSubCategories(int? categoryId = null)
        => Task.FromResult<IReadOnlyList<ProductSubCategoryLookupDto>>([]);
    public Task<IReadOnlyList<ProductTagDto>> GetTags()
        => Task.FromResult<IReadOnlyList<ProductTagDto>>([]);

    // ── Reference data writes (return minimal stubs) ─────────────────────────
    public Task<ProductCategoryLookupDto> CreateCategory(CreateProductCategoryRequest request)
        => Task.FromResult(new ProductCategoryLookupDto(0, request.NameCode, request.Name, request.PrintName, request.Slug, null, null, true, false));
    public Task<ProductSubCategoryLookupDto> CreateSubCategory(CreateProductSubCategoryRequest request)
        => Task.FromResult(new ProductSubCategoryLookupDto(0, request.CategoryId, request.NameCode, request.Name, request.PrintName, request.Slug, null, null, true, false));
    public Task<ProductTagDto> CreateTag(CreateProductTagRequest request)
        => Task.FromResult(new ProductTagDto(0, request.Name, request.Slug, request.Color));

    public Task<ProductCategoryLookupDto?> UpdateCategory(int id, UpdateProductCategoryRequest request)
        => Task.FromResult<ProductCategoryLookupDto?>(null);
    public Task<ProductSubCategoryLookupDto?> UpdateSubCategory(int id, UpdateProductSubCategoryRequest request)
        => Task.FromResult<ProductSubCategoryLookupDto?>(null);
    public Task<ProductTagDto?> UpdateTag(int id, UpdateProductTagRequest request)
        => Task.FromResult<ProductTagDto?>(null);

    public Task DeleteCategory(int id)    => Task.CompletedTask;
    public Task DeleteSubCategory(int id) => Task.CompletedTask;
    public Task DeleteTag(int id)         => Task.CompletedTask;
}
