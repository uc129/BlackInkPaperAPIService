using Dapper;
using Domain.Aggregates.Ecommerce;
using Domain.Entities.Ecommerce;

namespace Infrastructure.Mappers;

public static class ProductRepositoryMapper
{
    public static DynamicParameters ToProductParameters(ProductAggregate product)
    {
        var parameters = new DynamicParameters();
        parameters.Add("Id", product.Id);
        parameters.Add("ArtistId", product.ArtistId);
        parameters.Add("ProductId", product.ProductId);
        parameters.Add("Name", product.Name);
        parameters.Add("Slug", product.Slug);
        parameters.Add("NameCode", product.NameCode);
        parameters.Add("PrintName", product.PrintName);
        parameters.Add("Description", product.Description);
        parameters.Add("ShortDescription", product.ShortDescription);
        parameters.Add("BasePrice", product.BasePrice);
        parameters.Add("FinalPrice", product.FinalPrice);
        parameters.Add("CurrencyCode", product.CurrencyCode);
        parameters.Add("CategoryId", product.CategoryId);
        parameters.Add("SubCategoryId", product.SubCategoryId);
        parameters.Add("IsFeatured", product.IsFeatured);
        parameters.Add("IsAvailable", product.IsAvailable);
        parameters.Add("CoverImageUrl", product.CoverImageUrl);
        parameters.Add("HeaderImageUrl", product.HeaderImageUrl);
        parameters.Add("AverageRating", product.AverageRating);
        parameters.Add("ReviewCount", product.ReviewCount);
        parameters.Add("StockQuantity", product.StockQuantity);
        parameters.Add("CreatedAt", product.CreatedAt);
        parameters.Add("CreatedBy", product.CreatedBy);
        parameters.Add("UpdatedAt", product.UpdatedAt);
        parameters.Add("UpdatedBy", product.UpdatedBy);
        parameters.Add("IsUsingStandardVariants", product.IsUsingStandardVariants);
        return parameters;
    }

    public static ProductAggregate ToAggregate(
        ProductRow row,
        ArtSpecificationsRow? artSpecifications,
        List<ProductImage> images,
        List<ProductTag> tags,
        List<ProductVariantAggregate> variants)
    {
        var artSpecs = ToArtSpecifications(artSpecifications);

        // Search only carries the IsOriginal flag (via LEFT JOIN), not the full
        // ArtSpecifications; surface it without clobbering a full spec load.
        if (artSpecifications is null && row.IsOriginal is { } isOriginal)
        {
            artSpecs.IsOriginal = isOriginal;
        }

        return new ProductAggregate
        {
            Id = row.Id,
            ArtistId = row.ArtistId,
            ProductId = row.ProductId,
            Name = row.Name,
            Slug = row.Slug,
            NameCode = row.NameCode,
            PrintName = row.PrintName,
            Description = row.Description,
            ShortDescription = row.ShortDescription,
            BasePrice = row.BasePrice,
            FinalPrice = row.FinalPrice,
            CurrencyCode = row.CurrencyCode,
            CategoryId = row.CategoryId,
            SubCategoryId = row.SubCategoryId,
            IsFeatured = row.IsFeatured,
            IsAvailable = row.IsAvailable,
            CoverImageUrl = row.CoverImageUrl,
            HeaderImageUrl = row.HeaderImageUrl,
            AverageRating = row.AverageRating,
            ReviewCount = row.ReviewCount,
            StockQuantity = row.StockQuantity,
            CreatedAt = row.CreatedAt,
            CreatedBy = row.CreatedBy,
            UpdatedAt = row.UpdatedAt,
            UpdatedBy = row.UpdatedBy,
            IsUsingStandardVariants = row.IsUsingStandardVariants,
            ArtSpecs = artSpecs,
            Images = images,
            Tags = tags,
            Variants = variants
        };
    }

    public static DynamicParameters ToArtSpecificationsParameters(int productId, ArtSpecifications artSpecifications)
    {
        var parameters = new DynamicParameters();
        parameters.Add("ProductId", productId);
        parameters.Add("Width", artSpecifications.PhysicalDimensions?.Width);
        parameters.Add("Height", artSpecifications.PhysicalDimensions?.Height);
        parameters.Add("Unit", artSpecifications.PhysicalDimensions?.Unit.ToString());
        parameters.Add("WeightGrams", artSpecifications.WeightGrams);
        parameters.Add("IsFramed", artSpecifications.IsFramed);
        parameters.Add("Material", artSpecifications.Material);
        parameters.Add("FileFormat", artSpecifications.FileFormat);
        parameters.Add("ResolutionDpi", artSpecifications.ResolutionDpi);
        parameters.Add("PixelDimensions", artSpecifications.PixelDimensions);
        parameters.Add("PaperType", artSpecifications.PaperType);
        parameters.Add("PaperWeight", artSpecifications.PaperWeight);
        parameters.Add("InkType", artSpecifications.InkType);
        parameters.Add("IsOriginal", artSpecifications.IsOriginal);
        parameters.Add("IsSigned", artSpecifications.IsSigned);
        parameters.Add("HasCertificate", artSpecifications.HasCertificate);
        parameters.Add("FramingStatus", artSpecifications.FramingStatus);
        return parameters;
    }

    private static ArtSpecifications ToArtSpecifications(ArtSpecificationsRow? row)
    {
        if (row is null)
        {
            return new ArtSpecifications();
        }

        return new ArtSpecifications
        {
            PhysicalDimensions = row.Width.HasValue && row.Height.HasValue
                ? new Dimensions
                {
                    Width = row.Width.Value,
                    Height = row.Height.Value,
                    Unit = ParseDimensionUnit(row.Unit)
                }
                : null,
            WeightGrams = row.WeightGrams,
            IsFramed = row.IsFramed,
            Material = row.Material,
            FileFormat = row.FileFormat,
            ResolutionDpi = row.ResolutionDpi,
            PixelDimensions = row.PixelDimensions,
            PaperType = row.PaperType,
            PaperWeight = row.PaperWeight,
            InkType = row.InkType,
            IsOriginal = row.IsOriginal,
            IsSigned = row.IsSigned,
            HasCertificate = row.HasCertificate,
            FramingStatus = row.FramingStatus
        };
    }

    private static DimensionUnits ParseDimensionUnit(string? unit)
        => Enum.TryParse<DimensionUnits>(unit, true, out var parsedUnit) ? parsedUnit : DimensionUnits.cm;

    public sealed class ProductRow
    {
        public int Id { get; init; }
        public int ArtistId { get; init; }
        public string ProductId { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string Slug { get; init; } = string.Empty;
        public string NameCode { get; init; } = string.Empty;
        public string PrintName { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public string? ShortDescription { get; init; }
        public decimal BasePrice { get; init; }
        public decimal FinalPrice { get; init; }
        public string CurrencyCode { get; init; } = "INR";
        public int CategoryId { get; init; }
        public int SubCategoryId { get; init; }
        public bool IsFeatured { get; init; }
        public bool IsAvailable { get; init; }
        public string CoverImageUrl { get; init; } = string.Empty;
        public string HeaderImageUrl { get; init; } = string.Empty;
        public double AverageRating { get; init; }
        public int ReviewCount { get; init; }
        public int? StockQuantity { get; init; }
        public DateTime CreatedAt { get; init; }
        public string CreatedBy { get; init; } = string.Empty;
        public DateTime UpdatedAt { get; init; }
        public string UpdatedBy { get; init; } = string.Empty;
        public bool IsUsingStandardVariants { get; init; }

        // Populated only by the search query (LEFT JOIN ArtSpecifications); null
        // for reads that load ArtSpecifications separately.
        public bool? IsOriginal { get; init; }
    }

    public sealed class ArtSpecificationsRow
    {
        public int Id { get; init; }
        public double? Width { get; init; }
        public double? Height { get; init; }
        public string? Unit { get; init; }
        public int? WeightGrams { get; init; }
        public bool? IsFramed { get; init; }
        public string? Material { get; init; }
        public string? FileFormat { get; init; }
        public int? ResolutionDpi { get; init; }
        public string? PixelDimensions { get; init; }
        public string? PaperType { get; init; }
        public string? PaperWeight { get; init; }
        public string? InkType { get; init; }
        public bool IsOriginal { get; init; }
        public bool IsSigned { get; init; }
        public bool HasCertificate { get; init; }
        public string? FramingStatus { get; init; }
    }
}
