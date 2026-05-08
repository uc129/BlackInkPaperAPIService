using Application.DTOs.Products;
using Domain.Aggregates.Ecommerce;
using Domain.Entities.Ecommerce;

namespace BlackInkPaperAPIService.Tests.Products;

internal static class ProductTestData
{
    public static CreateProductRequest CreateRequest(
        string productId = "ART-001",
        string slug = "sunset-art",
        string nameCode = "SUNSET-001")
        => new(
            ProductId: productId,
            Name: "Sunset Print",
            Slug: slug,
            ArtistId: 10,
            CategoryId: 1,
            SubCategoryId: 2,
            ArtSpecs: new ArtSpecificationsDto(
                PhysicalDimensions: new DimensionsDto(20, 30, "cm"),
                WeightGrams: 200,
                IsFramed: false,
                Material: "Canvas",
                FileFormat: null,
                ResolutionDpi: null,
                PixelDimensions: null),
            IsUsingStandardVariants: false,
            IsFeatured: true,
            IsAvailable: true,
            Content: new ProductTextContentDto(
                NameCode: nameCode,
                PrintName: "Sunset",
                Description: "Orange sky artwork",
                ShortDescription: "Warm sunset"),
            Pricing: new ProductPricingDto(
                BasePrice: 100,
                FinalPrice: 90,
                CurrencyCode: "INR"),
            Media: new ProductMediaDto(
                CoverImageUrl: "https://cdn.example.com/cover.jpg",
                HeaderImageUrl: "https://cdn.example.com/header.jpg"),
            StockQuantity: 5,
            TagIds: [7, 8],
            Images:
            [
                new CreateProductImageDto(
                    AltText: "Sunset image",
                    IsPrimary: true,
                    DisplayOrder: 1,
                    PublicId: "img_1",
                    BaseUrl: "https://cdn.example.com/img_1.jpg",
                    AspectRatio: 1.5,
                    Width: 1200,
                    Height: 800,
                    PlaceholderUrl: null,
                    Format: null,
                    Dpi: null,
                    FileSize: null)
            ],
            Variants:
            [
                new CreateProductVariantDto(
                    Label: "Size",
                    FulfillmentType: (int)ProductFulfillmentType.physical,
                    Sku: "A4-SUNSET",
                    WeightGrams: 120,
                    StockQuantity: null,
                    AbsolutePrice: null,
                    ProductImageId: null,
                    Options:
                    [
                        new CreateProductVariantOptionDto(
                            Value: "A4",
                            PriceModifier: 0,
                            AbsolutePrice: null,
                            StockQuantity: 2)
                    ])
            ]);

    public static UpdateProductRequest UpdateRequest(
        string productId = "ART-001-UPDATED",
        string slug = "sunset-art-updated",
        string nameCode = "SUNSET-UPDATED")
        => new(
            ProductId: productId,
            Name: "Sunset Print Updated",
            Slug: slug,
            CategoryId: 1,
            SubCategoryId: 2,
            ArtSpecs: new ArtSpecificationsDto(
                PhysicalDimensions: new DimensionsDto(24, 36, "inch"),
                WeightGrams: 300,
                IsFramed: true,
                Material: "Paper",
                FileFormat: null,
                ResolutionDpi: null,
                PixelDimensions: null),
            IsUsingStandardVariants: true,
            IsFeatured: false,
            IsAvailable: true,
            Content: new ProductTextContentDto(
                NameCode: nameCode,
                PrintName: "Sunset Updated",
                Description: "Updated description",
                ShortDescription: "Updated short"),
            Pricing: new ProductPricingDto(
                BasePrice: 120,
                FinalPrice: 100,
                CurrencyCode: "INR"),
            Media: new ProductMediaDto(
                CoverImageUrl: "https://cdn.example.com/cover-new.jpg",
                HeaderImageUrl: "https://cdn.example.com/header-new.jpg"),
            StockQuantity: 9,
            TagIds: [7],
            Images:
            [
                new UpdateProductImageDto(
                    Id: 5,
                    AltText: "Updated image",
                    IsPrimary: true,
                    DisplayOrder: 1,
                    PublicId: "img_2",
                    BaseUrl: "https://cdn.example.com/img_2.jpg",
                    AspectRatio: 1.2,
                    Width: 1000,
                    Height: 833,
                    PlaceholderUrl: null,
                    Format: null,
                    Dpi: null,
                    FileSize: null)
            ],
            Variants:
            [
                new UpdateProductVariantDto(
                    Id: 20,
                    Label: "Format",
                    FulfillmentType: (int)ProductFulfillmentType.digital,
                    Sku: "DIGI-SUNSET",
                    WeightGrams: null,
                    StockQuantity: null,
                    AbsolutePrice: null,
                    ProductImageId: null,
                    Options:
                    [
                        new UpdateProductVariantOptionDto(
                            Id: 30,
                            Value: "Digital",
                            PriceModifier: null,
                            AbsolutePrice: 100,
                            StockQuantity: null)
                    ])
            ]);

    public static ProductAggregate Aggregate(
        int id = 1,
        string productId = "ART-001",
        string slug = "sunset-art")
        => new()
        {
            Id = id,
            ArtistId = 10,
            ProductId = productId,
            Name = "Sunset Print",
            Slug = slug,
            NameCode = "SUNSET-001",
            PrintName = "Sunset",
            Description = "Orange sky artwork",
            ShortDescription = "Warm sunset",
            BasePrice = 100,
            FinalPrice = 90,
            CurrencyCode = "INR",
            CategoryId = 1,
            SubCategoryId = 2,
            IsFeatured = true,
            IsAvailable = true,
            IsUsingStandardVariants = false,
            CoverImageUrl = "https://cdn.example.com/cover.jpg",
            HeaderImageUrl = "https://cdn.example.com/header.jpg",
            AverageRating = 4.5,
            ReviewCount = 12,
            StockQuantity = 5,
            CreatedAt = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            CreatedBy = "tester",
            UpdatedAt = new DateTime(2026, 4, 2, 0, 0, 0, DateTimeKind.Utc),
            UpdatedBy = "tester",
            Images =
            [
                new ProductImage
                {
                    Id = 5,
                    ProductId = id,
                    AltText = "Sunset image",
                    IsPrimary = true,
                    DisplayOrder = 1,
                    PublicId = "img_1",
                    BaseUrl = "https://cdn.example.com/img_1.jpg",
                    AspectRatio = 1.5,
                    Width = 1200,
                    Height = 800
                }
            ],
            Tags =
            [
                new ProductTag { Id = 7, Name = "Featured", Slug = "featured", Color = "#fff000" }
            ],
            Variants =
            [
                new ProductVariantAggregate
                {
                    Id = 20,
                    ProductId = id,
                    Label = "Size",
                    FulfillmentType = ProductFulfillmentType.physical,
                    Sku = "A4-SUNSET",
                    WeightGrams = 120,
                    Options =
                    [
                        new ProductVariantOption
                        {
                            Id = 30,
                            ProductVariantId = 20,
                            Value = "A4",
                            StockQuantity = 2,
                        }
                    ]
                }
            ]
        };
}
