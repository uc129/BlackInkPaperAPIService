namespace Application.DTOs.Products;

public record ArtistLookupDto(
    int Id,
    string DisplayName,
    string? ProfileImageUrl,
    bool IsVerified);

public record ProductCategoryLookupDto(
    int Id,
    string NameCode,
    string Name,
    string PrintName,
    string Slug,
    string? Description,
    string? CoverImageUrl,
    bool IsActive,
    bool IsFeatured);

public record ProductSubCategoryLookupDto(
    int Id,
    int CategoryId,
    string NameCode,
    string Name,
    string PrintName,
    string Slug,
    string? Description,
    string? CoverImageUrl,
    bool IsActive,
    bool IsFeatured);

public record CreateProductCategoryRequest(
    string NameCode,
    string Name,
    string PrintName,
    string Slug,
    string? Description,
    string? CoverImageUrl,
    bool IsActive = true,
    bool IsFeatured = false);

public record CreateProductSubCategoryRequest(
    int CategoryId,
    string NameCode,
    string Name,
    string PrintName,
    string Slug,
    string? Description,
    string? CoverImageUrl,
    bool IsActive = true,
    bool IsFeatured = false);
