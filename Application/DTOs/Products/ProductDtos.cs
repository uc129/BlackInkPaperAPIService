namespace Application.DTOs.Products;

public record CreateProductRequest(
    string ProductId,
    string Name,
    string Slug,
    int ArtistId,
    int CategoryId,
    int SubCategoryId,
    int? ArtSpecId,
    ArtSpecificationsDto? ArtSpecs,
    bool IsUsingStandardVariants,
    bool IsFeatured,
    bool IsAvailable,
    ProductTextContentDto Content,
    ProductPricingDto Pricing,
    ProductMediaDto Media,
    int? StockQuantity,
    List<int>? TagIds,
    List<CreateProductImageDto>? Images,
    List<CreateProductVariantDto>? Variants);

public record UpdateProductRequest(
    string ProductId,
    string Name,
    string Slug,
    int CategoryId,
    int SubCategoryId,
    int? ArtSpecId,
    ArtSpecificationsDto? ArtSpecs,
    bool IsUsingStandardVariants,
    bool IsFeatured,
    bool IsAvailable,
    ProductTextContentDto Content,
    ProductPricingDto Pricing,
    ProductMediaDto Media,
    int? StockQuantity,
    List<int>? TagIds,
    List<UpdateProductImageDto>? Images,
    List<UpdateProductVariantDto>? Variants);

public record ProductResponseDto(
    int Id,
    string ProductId,
    string Name,
    string Slug,
    int ArtistId,
    int ArtSpecId,
    bool IsUsingStandardVariants,
    ArtSpecificationsDto? ArtSpecs,
    ProductTextContentDto Content,
    ProductPricingDto Pricing,
    ProductTaxonomyDto Taxonomy,
    ProductMediaDto Media,
    ProductStatsDto Stats,
    ProductAuditDto Audit,
    List<ProductTagDto> Tags,
    List<ProductImageDto> Images,
    List<ProductVariantDto> Variants);

public record ProductSummaryDto(
    int Id,
    string ProductId,
    string Name,
    string Slug,
    int ArtistId,
    ProductPricingDto Pricing,
    ProductTaxonomyDto Taxonomy,
    ProductMediaDto Media,
    ProductStatsDto Stats,
    bool IsUsingStandardVariants);

public record ProductSearchRequest(
    string? Query,
    int? ArtistId,
    int? CategoryId,
    int? SubCategoryId,
    int? TagId,
    bool? IsAvailable,
    bool? IsFeatured,
    int Page = 1,
    int PageSize = 20);

public record UpdateProductFlagsRequest(
    bool? IsAvailable,
    bool? IsFeatured);

public record PagedResultDto<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount);

public record ProductTextContentDto(
    string NameCode,
    string PrintName,
    string Description,
    string? ShortDescription);

public record ProductPricingDto(
    decimal BasePrice,
    decimal FinalPrice,
    string CurrencyCode);

public record ProductTaxonomyDto(
    int CategoryId,
    int SubCategoryId,
    bool IsFeatured,
    bool IsAvailable);

public record ProductMediaDto(
    string CoverImageUrl,
    string HeaderImageUrl);

public record ProductStatsDto(
    double AverageRating,
    int ReviewCount,
    int? StockQuantity);

public record ProductAuditDto(
    DateTime CreatedAt,
    string CreatedBy,
    DateTime UpdatedAt,
    string UpdatedBy);

public record ProductTagDto(
    int Id,
    string Name,
    string Slug,
    string? Color);

public record ProductImageDto(
    int Id,
    string AltText,
    bool IsPrimary,
    int DisplayOrder,
    string PublicId,
    string BaseUrl,
    double AspectRatio,
    int Width,
    int Height,
    string? PlaceholderUrl);

public record CreateProductImageDto(
    string AltText,
    bool IsPrimary,
    int DisplayOrder,
    string PublicId,
    string BaseUrl,
    double AspectRatio,
    int Width,
    int Height,
    string? PlaceholderUrl);

public record UpdateProductImageDto(
    int? Id,
    string AltText,
    bool IsPrimary,
    int DisplayOrder,
    string PublicId,
    string BaseUrl,
    double AspectRatio,
    int Width,
    int Height,
    string? PlaceholderUrl);

public record CreateProductVariantDto(
    string Label,
    List<CreateProductVariantOptionDto> Options);

public record UpdateProductVariantDto(
    int? Id,
    string Label,
    List<UpdateProductVariantOptionDto> Options);

public record ProductVariantDto(
    int Id,
    string Label,
    List<ProductVariantOptionDto> Options);

public record CreateProductVariantOptionDto(
    string Value,
    decimal? PriceModifier,
    decimal? AbsolutePrice,
    int? StockQuantity,
    int FulfillmentType,
    string Sku,
    decimal? WeightGrams);

public record UpdateProductVariantOptionDto(
    int? Id,
    string Value,
    decimal? PriceModifier,
    decimal? AbsolutePrice,
    int? StockQuantity,
    int FulfillmentType,
    string Sku,
    decimal? WeightGrams);

public record ProductVariantOptionDto(
    int Id,
    string Value,
    decimal? PriceModifier,
    decimal? AbsolutePrice,
    int? StockQuantity,
    int FulfillmentType,
    string Sku,
    decimal? WeightGrams);

public record ArtSpecificationsDto(
    DimensionsDto? PhysicalDimensions,
    int? WeightGrams,
    bool? IsFramed,
    string? Material,
    string? FileFormat,
    int? ResolutionDpi,
    string? PixelDimensions);

public record DimensionsDto(
    double Width,
    double Height,
    string Unit);
