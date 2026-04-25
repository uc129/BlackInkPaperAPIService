using Application.DTOs.Products;
using Domain.Aggregates.Ecommerce;
using Domain.Entities.Ecommerce;

namespace Infrastructure.Mappers;

public static class ProductDtoMapper
{
    public static ProductAggregate ToNewAggregate(CreateProductRequest request, DateTime nowUtc, string actor)
    {
        return new ProductAggregate
        {
            ArtistId = request.ArtistId,
            ProductId = request.ProductId.Trim(),
            Name = request.Name.Trim(),
            Slug = request.Slug.Trim(),
            NameCode = request.Content.NameCode.Trim(),
            PrintName = request.Content.PrintName.Trim(),
            Description = request.Content.Description.Trim(),
            ShortDescription = request.Content.ShortDescription?.Trim(),
            BasePrice = request.Pricing.BasePrice,
            FinalPrice = request.Pricing.FinalPrice,
            CurrencyCode = request.Pricing.CurrencyCode.Trim(),
            CategoryId = request.CategoryId,
            SubCategoryId = request.SubCategoryId,
            IsFeatured = request.IsFeatured,
            IsAvailable = request.IsAvailable,
            CoverImageUrl = request.Media.CoverImageUrl.Trim(),
            HeaderImageUrl = request.Media.HeaderImageUrl.Trim(),
            AverageRating = 0,
            ReviewCount = 0,
            StockQuantity = request.StockQuantity,
            CreatedAt = nowUtc,
            CreatedBy = actor,
            UpdatedAt = nowUtc,
            UpdatedBy = actor,
            ArtSpecs = ToArtSpecifications(request.ArtSpecs)?? new ArtSpecifications(),
            IsUsingStandardVariants = request.IsUsingStandardVariants,
            Images = request.Images?.Select(ToImage).ToList() ?? [],
            Tags = request.TagIds?.Distinct().Select(id => new ProductTag { Id = id }).ToList() ?? [],
            Variants = request.Variants?.Select(ToVariant).ToList() ?? []
        };
    }

    public static void ApplyUpdate(ProductAggregate product, UpdateProductRequest request, DateTime nowUtc, string actor)
    {
        product.ProductId = request.ProductId.Trim();
        product.Name = request.Name.Trim();
        product.Slug = request.Slug.Trim();
        product.NameCode = request.Content.NameCode.Trim();
        product.PrintName = request.Content.PrintName.Trim();
        product.Description = request.Content.Description.Trim();
        product.ShortDescription = request.Content.ShortDescription?.Trim();
        product.BasePrice = request.Pricing.BasePrice;
        product.FinalPrice = request.Pricing.FinalPrice;
        product.CurrencyCode = request.Pricing.CurrencyCode.Trim();
        product.CategoryId = request.CategoryId;
        product.SubCategoryId = request.SubCategoryId;
        product.ArtSpecs = ToArtSpecifications(request.ArtSpecs) ?? product.ArtSpecs;
        product.IsUsingStandardVariants = request.IsUsingStandardVariants;
        product.IsFeatured = request.IsFeatured;
        product.IsAvailable = request.IsAvailable;
        product.CoverImageUrl = request.Media.CoverImageUrl.Trim();
        product.HeaderImageUrl = request.Media.HeaderImageUrl.Trim();
        product.StockQuantity = request.StockQuantity;
        product.UpdatedAt = nowUtc;
        product.UpdatedBy = actor;
        product.Images = request.Images?.Select(ToImage).ToList() ?? [];
        product.Tags = request.TagIds?.Distinct().Select(tagId => new ProductTag { Id = tagId }).ToList() ?? [];
        product.Variants = request.Variants?.Select(ToVariant).ToList() ?? [];
    }

    public static ProductResponseDto ToResponse(ProductAggregate product)
    {
        return new ProductResponseDto(
            product.Id,
            product.ProductId,
            product.Name,
            product.Slug,
            product.ArtistId,
            product.IsUsingStandardVariants,
            ToArtSpecificationsDto(product.ArtSpecs),
            new ProductTextContentDto(
                product.NameCode,
                product.PrintName,
                product.Description,
                product.ShortDescription),
            new ProductPricingDto(
                product.BasePrice,
                product.FinalPrice,
                product.CurrencyCode),
            new ProductTaxonomyDto(
                product.CategoryId,
                product.SubCategoryId,
                product.IsFeatured,
                product.IsAvailable),
            new ProductMediaDto(
                product.CoverImageUrl,
                product.HeaderImageUrl),
            new ProductStatsDto(
                product.AverageRating,
                product.ReviewCount,
                product.StockQuantity),
            new ProductAuditDto(
                product.CreatedAt,
                product.CreatedBy,
                product.UpdatedAt,
                product.UpdatedBy),
            product.Tags.Select(tag => new ProductTagDto(tag.Id, tag.Name, tag.Slug, tag.Color)).ToList(),
            product.Images.Select(image => new ProductImageDto(
                image.Id,
                image.AltText,
                image.IsPrimary,
                image.DisplayOrder,
                image.PublicId,
                image.BaseUrl,
                image.AspectRatio,
                image.Width,
                image.Height,
                image.PlaceholderUrl)).ToList(),
            product.Variants.Select(variant => new ProductVariantDto(
                variant.Id,
                variant.Label,
                (int)variant.FulfillmentType,
                variant.Sku,
                variant.WeightGrams,
                variant.StockQuantity,
                variant.AbsolutePrice,
                variant.Options.Select(option => new ProductVariantOptionDto(
                    option.Id,
                    option.Value,
                    option.PriceModifier,
                    option.AbsolutePrice,
                    option.StockQuantity)).ToList())).ToList());
    }

    public static ProductSummaryDto ToSummary(ProductAggregate product)
    {
        return new ProductSummaryDto(
            product.Id,
            product.ProductId,
            product.Name,
            product.Slug,
            product.ArtistId,
            new ProductPricingDto(
                product.BasePrice,
                product.FinalPrice,
                product.CurrencyCode),
            new ProductTaxonomyDto(
                product.CategoryId,
                product.SubCategoryId,
                product.IsFeatured,
                product.IsAvailable),
            new ProductMediaDto(
                product.CoverImageUrl,
                product.HeaderImageUrl),
            new ProductStatsDto(
                product.AverageRating,
                product.ReviewCount,
                product.StockQuantity),
            product.IsUsingStandardVariants);
    }

    public static ProductImage ToImage(CreateProductImageDto image)
    {
        return new ProductImage
        {
            AltText = image.AltText.Trim(),
            IsPrimary = image.IsPrimary,
            DisplayOrder = image.DisplayOrder,
            PublicId = image.PublicId.Trim(),
            BaseUrl = image.BaseUrl.Trim(),
            AspectRatio = image.AspectRatio,
            Width = image.Width,
            Height = image.Height,
            PlaceholderUrl = image.PlaceholderUrl?.Trim()
        };
    }

    public static ProductImage ToImage(UpdateProductImageDto image)
    {
        return new ProductImage
        {
            Id = image.Id ?? 0,
            AltText = image.AltText.Trim(),
            IsPrimary = image.IsPrimary,
            DisplayOrder = image.DisplayOrder,
            PublicId = image.PublicId.Trim(),
            BaseUrl = image.BaseUrl.Trim(),
            AspectRatio = image.AspectRatio,
            Width = image.Width,
            Height = image.Height,
            PlaceholderUrl = image.PlaceholderUrl?.Trim()
        };
    }

    public static ProductVariantAggregate ToVariant(CreateProductVariantDto variant)
    {
        return new ProductVariantAggregate
        {
            Label = variant.Label.Trim(),
            FulfillmentType = (ProductFulfillmentType)variant.FulfillmentType,
            Sku = variant.Sku.Trim(),
            WeightGrams = variant.WeightGrams,
            StockQuantity = variant.StockQuantity,
            AbsolutePrice = variant.AbsolutePrice,
            Options = variant.Options.Select(ToOption).ToList()
        };
    }

    public static ProductVariantAggregate ToVariant(UpdateProductVariantDto variant)
    {
        return new ProductVariantAggregate
        {
            Id = variant.Id ?? 0,
            Label = variant.Label.Trim(),
            FulfillmentType = (ProductFulfillmentType)variant.FulfillmentType,
            Sku = variant.Sku.Trim(),
            WeightGrams = variant.WeightGrams,
            StockQuantity = variant.StockQuantity,
            AbsolutePrice = variant.AbsolutePrice,
            Options = variant.Options.Select(ToOption).ToList()
        };
    }

    public static ProductVariantOption ToOption(CreateProductVariantOptionDto option)
    {
        return new ProductVariantOption
        {
            Value = option.Value.Trim(),
            PriceModifier = option.PriceModifier,
            AbsolutePrice = option.AbsolutePrice,
            StockQuantity = option.StockQuantity
        };
    }

    public static ProductVariantOption ToOption(UpdateProductVariantOptionDto option)
    {
        return new ProductVariantOption
        {
            Id = option.Id ?? 0,
            Value = option.Value.Trim(),
            PriceModifier = option.PriceModifier,
            AbsolutePrice = option.AbsolutePrice,
            StockQuantity = option.StockQuantity
        };
    }

    public static ArtSpecifications? ToArtSpecifications(ArtSpecificationsDto? artSpecs)
    {
        if (artSpecs is null)
        {
            return null;
        }

        return new ArtSpecifications
        {
            PhysicalDimensions = artSpecs.PhysicalDimensions is null
                ? null
                : new Dimensions
                {
                    Width = artSpecs.PhysicalDimensions.Width,
                    Height = artSpecs.PhysicalDimensions.Height,
                    Unit = ParseDimensionUnit(artSpecs.PhysicalDimensions.Unit)
                },
            WeightGrams = artSpecs.WeightGrams,
            IsFramed = artSpecs.IsFramed,
            Material = artSpecs.Material?.Trim(),
            FileFormat = artSpecs.FileFormat?.Trim(),
            ResolutionDpi = artSpecs.ResolutionDpi,
            PixelDimensions = artSpecs.PixelDimensions?.Trim()
        };
    }

    public static ArtSpecificationsDto? ToArtSpecificationsDto(ArtSpecifications? artSpecs)
    {
        if (artSpecs is null)
        {
            return null;
        }

        if (artSpecs.PhysicalDimensions is null
            && artSpecs.WeightGrams is null
            && artSpecs.IsFramed is null
            && artSpecs.Material is null
            && artSpecs.FileFormat is null
            && artSpecs.ResolutionDpi is null
            && artSpecs.PixelDimensions is null)
        {
            return null;
        }

        return new ArtSpecificationsDto(
            artSpecs.PhysicalDimensions is null
                ? null
                : new DimensionsDto(
                    artSpecs.PhysicalDimensions.Width,
                    artSpecs.PhysicalDimensions.Height,
                    artSpecs.PhysicalDimensions.Unit.ToString()),
            artSpecs.WeightGrams,
            artSpecs.IsFramed,
            artSpecs.Material,
            artSpecs.FileFormat,
            artSpecs.ResolutionDpi,
            artSpecs.PixelDimensions);
    }

    private static DimensionUnits ParseDimensionUnit(string? unit)
        => Enum.TryParse<DimensionUnits>(unit, true, out var parsedUnit) ? parsedUnit : DimensionUnits.cm;
}
